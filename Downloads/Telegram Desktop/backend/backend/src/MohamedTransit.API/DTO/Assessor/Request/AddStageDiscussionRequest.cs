namespace MohamedTransit.API.DTO.Assessor.Request;

public class AddStageDiscussionRequest
{
    public long ServiceId { get; set; }
    public long StageId { get; set; }
    public string Comment { get; set; } = string.Empty;
}
