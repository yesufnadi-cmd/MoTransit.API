namespace MohamedTransit.API.DTO.Assessor.Request;

public class EscalateServiceRequest
{
    public long ServiceId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
