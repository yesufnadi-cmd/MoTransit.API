namespace MohamedTransit.API.DTO.Assessor.Request;

public class NotifyImporterRequest
{
    public long ServiceId { get; set; }
    public string Subject { get; set; } = "Service update";
    public string Message { get; set; } = string.Empty;
    public bool IsUrgent { get; set; }
}
