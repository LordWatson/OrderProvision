namespace OrderProvision.Models
{
    public class OrderCreated
    {
        public string message_id { get; set; }
        public int task_id { get; set; }
        public int order_id { get; set; }
        public string step_key { get; set; }
        public string product_type { get; set; }
        
        // For backward compatibility, create a synthetic order property
        public Order order => new Order 
        { 
            id = order_id.ToString(), 
            product_type = this.product_type 
        };
    }


    public class Order
    {
        public string id { get; set; }
        public string product_type { get; set; }
    }
}