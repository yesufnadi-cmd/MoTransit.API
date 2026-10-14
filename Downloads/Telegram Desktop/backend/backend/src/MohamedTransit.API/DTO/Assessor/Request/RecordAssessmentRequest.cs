namespace MohamedTransit.API.DTO.Assessor.Request;

public class RecordAssessmentRequest
{
    public long ServiceId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public decimal TaxAmount { get; set; }
    public decimal DutyAmount { get; set; }
    public string? Notes { get; set; }
}
