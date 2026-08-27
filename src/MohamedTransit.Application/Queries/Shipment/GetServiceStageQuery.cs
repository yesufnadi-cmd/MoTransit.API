using MediatR;

using MohamedTransit.Application.Helper;
using MohamedTransit.Domain.Entities;



namespace Transit.Application;

public class GetServiceStagesQuery : IRequest<OperationResult<List<ServiceStageExecution>>>
{
    public long ServiceId { get; set; }
}

