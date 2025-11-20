using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OrderProvision.Models;
using OrderProvision.Services.Interfaces;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection _connection;
    private IModel _channel;

    public Worker(
        ILogger<Worker> logger, 
        IConfiguration config, 
        IServiceScopeFactory scopeFactory
        )
    {
        System.Console.WriteLine("Worker running");
        
        _logger = logger;
        _config = config;
        _scopeFactory = scopeFactory;

        InitRabbitMq();
    }

    private void InitRabbitMq()
    {
        try
        {
            Console.WriteLine("Initializing RabbitMQ connection...");
        
            var host = _config["Rabbit:Host"];
            var port = _config["Rabbit:Port"];
            var user = _config["Rabbit:User"];
            var pass = _config["Rabbit:Pass"];
        
            if(string.IsNullOrEmpty(host) || string.IsNullOrEmpty(port))
            {
                throw new InvalidOperationException("RabbitMQ configuration is missing");
            }
        
            Console.WriteLine($"Connecting to RabbitMQ at {host}:{port}");
        
            var factory = new ConnectionFactory()
            {
                HostName = host,
                Port = int.Parse(port),
                UserName = user,
                Password = pass,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            var exchange = _config["Rabbit:Exchange"];
            var queue = _config["Rabbit:Queue"];

            Console.WriteLine($"Setting up exchange: {exchange}, queue: {queue}");

            _channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true);
            _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue, exchange, "order.created");
        
            Console.WriteLine("RabbitMQ initialized successfully");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Failed to initialize RabbitMQ: {ex.Message}");
            _logger?.LogError(ex, "Failed to initialize RabbitMQ");
            throw;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("ExecuteAsync started");
        _logger.LogInformation("Setting up RabbitMQ consumer");
    
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (sender, ea) =>
        {
            Console.WriteLine("Message received!");
            _logger.LogInformation("Message received in consumer");
            
            try
            {
                Console.WriteLine("Creating service scope...");
                using var scope = _scopeFactory.CreateScope();
                
                Console.WriteLine("Getting handler factory...");
                var handlerFactory = scope.ServiceProvider.GetRequiredService<IProductHandlerFactory>();
                Console.WriteLine("Handler factory retrieved successfully");

                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                
                Console.WriteLine($"Received message: {json}");
                _logger.LogInformation("Received message: {json}", json);

                Console.WriteLine("Deserializing order...");
                var orderCreated = JsonSerializer.Deserialize<OrderCreated>(json);
                Console.WriteLine($"Order deserialized: ID={orderCreated?.order?.id}");
                
                var productType = ProductTypeExtensions.ParseProductType(orderCreated.order.product_type);
                Console.WriteLine($"Product type parsed: {productType}");

                _logger.LogInformation("Processing order {OrderId} for product type: {ProductType}", orderCreated.order.id, productType);

                bool success = false;
                
                if(productType != ProductType.Unknown)
                {
                    Console.WriteLine("Getting handler for product type...");
                    var handler = handlerFactory.GetHandler(productType);
                    Console.WriteLine("Handler retrieved, processing order...");
                    
                    success = await handler.ProcessOrderAsync(orderCreated);
                    Console.WriteLine($"Order processing completed: success={success}");
                }
                else
                {
                    _logger.LogWarning("Unknown product type: {ProductType}", orderCreated.order.product_type);
                }

                Console.WriteLine("Publishing result event...");
                await PublishResultEvent(orderCreated, success);
                Console.WriteLine("Result event published");
                
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                _logger.LogInformation("Successfully processed order {OrderId}", orderCreated.order.id);
                Console.WriteLine("Message processing completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in message processing: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                _logger.LogError(ex, "Error processing message");
                _channel.BasicNack(ea.DeliveryTag, false, false);
            }
        };

        var queueName = _config["Rabbit:Queue"];
        Console.WriteLine($"\nStarting to consume from queue: {queueName}");
    
        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
    
        _logger.LogInformation("Consumer started, waiting for messages...");
        Console.WriteLine("\nConsumer started, waiting for messages...");
    
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
    
    private async Task PublishResultEvent(OrderCreated originalOrder, bool success)
    {
        /*
         * build the params we send back
         */
        var messageId = Guid.NewGuid().ToString();
        var resultEvent = new
        {
            message_id = messageId,
            correlation_id = originalOrder.message_id,
            type = success ? "sourceguru.fulfilled" : "sourceguru.failed",
            occurred_at = DateTime.UtcNow,
        
            order = new
            {
                id = originalOrder.order.id,
                product_type = originalOrder.order.product_type,
                status = success ? "fulfilled" : "failed"
            }
        };

        var outJson = JsonSerializer.Serialize(resultEvent);
        var outBody = Encoding.UTF8.GetBytes(outJson);

        var props = _channel.CreateBasicProperties();
        props.ContentType = "application/json";
        props.DeliveryMode = 2;
        props.MessageId = messageId;
        props.CorrelationId = originalOrder.message_id;
        props.Timestamp = new AmqpTimestamp(((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds());
        
        // headers Laravel expects in the consumer script
        props.Headers = new Dictionary<string, object>
        {
            ["type"] = resultEvent.type,
            ["source"] = "sourceguru.processor"
        };

        // TODO ensure response queue / exchange exists
        var responseQueue = "sourceguru.results"; 
        
        try
        {
            _channel.QueueDeclare(responseQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(responseQueue, _config["Rabbit:Exchange"], resultEvent.type);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not declare/bind response queue {Queue}", responseQueue);
        }

        _channel.BasicPublish(
            exchange: _config["Rabbit:Exchange"],
            routingKey: resultEvent.type,
            basicProperties: props,
            body: outBody
        );

        _logger.LogInformation("Published {EventType} for order {OrderId} with message ID {MessageId}", 
            resultEvent.type, originalOrder.order.id, messageId);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}