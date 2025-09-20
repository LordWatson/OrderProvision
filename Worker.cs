using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OrderProvision.Models;
using OrderProvision.Services.Interfaces;
using Microsoft.Extensions.Configuration;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly IProductHandlerFactory _handlerFactory;
    private IConnection _connection;
    private IModel _channel;

    public Worker(
        ILogger<Worker> logger, 
        IConfiguration config, 
        IProductHandlerFactory handlerFactory)
    {
        _logger = logger;
        _config = config;
        _handlerFactory = handlerFactory;
        InitRabbitMq();
    }

    private void InitRabbitMq()
    {
        var factory = new ConnectionFactory()
        {
            HostName = _config["Rabbit:Host"],
            Port = int.Parse(_config["Rabbit:Port"]),
            UserName = _config["Rabbit:User"],
            Password = _config["Rabbit:Pass"],
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        var exchange = _config["Rabbit:Exchange"];
        var queue = _config["Rabbit:Queue"];

        _channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue, exchange, "order.created");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (sender, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            _logger.LogInformation("Received message: {json}", json);

            try
            {
                /*
                 * get the components from the order
                 */
                var orderCreated = JsonSerializer.Deserialize<OrderCreated>(json);
                var productType = ProductTypeExtensions.ParseProductType(orderCreated.order.product_type);

                _logger.LogInformation("Processing order {OrderId} for product type: {ProductType}", orderCreated.order.id, productType);

                bool success = false;
                
                /*
                 * are we working with a product type we've configured
                 */
                if(productType != ProductType.Unknown)
                {
                    try
                    {
                        /*
                         * get the handler for this product type
                         */
                        var handler = _handlerFactory.GetHandler(productType);
                        
                        /*
                         * trigger the handler
                         */
                        success = await handler.ProcessOrderAsync(orderCreated);
                    }
                    catch (NotSupportedException ex)
                    {
                        /*
                         * we don't have a handler setup yet
                         * for example maybe the product type is 'potato'
                         */
                        _logger.LogWarning(ex, "Unsupported product type: {ProductType}", productType);
                        success = false;
                    }
                }else
                {
                    /*
                     * we're not setup for this product type yet
                     * @see ProductTypes.cs
                     */
                    _logger.LogWarning("Unknown product type: {ProductType}", orderCreated.order.product_type);
                }

                /*
                 * send result back to RabbitMQ
                 */
                await PublishResultEvent(orderCreated, success);
                
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                _logger.LogInformation("Successfully processed order {OrderId}", orderCreated.order.id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                _channel.BasicNack(ea.DeliveryTag, false, false);
            }
        };

        _channel.BasicConsume(queue: _config["Rabbit:Queue"], autoAck: false, consumer: consumer);
        
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task PublishResultEvent(OrderCreated originalOrder, bool success)
    {
        /*
         * build the params we send back
         */
        var resultEvent = new
        {
            message_id = Guid.NewGuid().ToString(),
            correlation_id = originalOrder.message_id,
            type = success ? "order.fulfilled" : "order.failed",
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

        _channel.BasicPublish(
            exchange: _config["Rabbit:Exchange"],
            routingKey: resultEvent.type,
            basicProperties: props,
            body: outBody
        );

        _logger.LogInformation("Published {EventType} for order {OrderId}", resultEvent.type, originalOrder.order.id);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}