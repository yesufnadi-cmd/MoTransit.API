using MediatR;
using MohamedTransit.Domain.Entities;
namespace MohamedTransit.API.DTO.MOT.Response;
public class CaseExecutorDashboardResponse
{
    public int AssignedServices { get; set; }
    public int PendingServices { get; set; }
    public int CompletedServices { get; set; }
    public int BlockedStages { get; set; }
    public List<ServiceStageExecution> TodaysTasks { get; set; } = new();
    //public List<Notification> UrgentNotifications { get; set; } = new();
}


