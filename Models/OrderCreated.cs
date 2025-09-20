namespace OrderProvision.Models
{
    public class OrderCreated
    {
        public string message_id { get; set; }
        public DateTime occurred_at { get; set; }
        public Order order { get; set; }
    }

    public class Order
    {
        public string id { get; set; }
        public string product_type { get; set; }
        public decimal amount { get; set; }
        public string customer_id { get; set; }
    }
}