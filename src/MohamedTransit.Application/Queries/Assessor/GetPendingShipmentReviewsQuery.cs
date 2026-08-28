using MediatR;
using MohamedTransit.Application.Helper;
using MohamedTransit.Domain.Entities;
namespace Transit.Application.Queries;
public class GetPendingShipmentReviewsQuery : IRequest<OperationResult<List<Shipment>>>
{
    public long UserId { get; set; }
}
