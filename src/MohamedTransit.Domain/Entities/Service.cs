namespace MohamedTransit.Domain.Entities // (ወይም የፕሮጀክትዎ ትክክለኛ የ Domain namespace)
{
    public class Service
    {
        public int Id { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
