namespace MohamedTransit.Application.DTO
{
    public class ServiceDto
    {
        public int Id { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
