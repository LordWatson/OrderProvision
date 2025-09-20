namespace OrderProvision.Models
{
    public enum ProductType
    {
        Router,
        BroadbandLine,
        Handset,
        Unknown
    }

    public static class ProductTypeExtensions
    {
        public static ProductType ParseProductType(string productType)
        {
            return productType?.ToLowerInvariant() switch
            {
                "router" => ProductType.Router,
                "broadband_line" => ProductType.BroadbandLine,
                "handset" => ProductType.Handset,
                _ => ProductType.Unknown
            };
        }
    }
}