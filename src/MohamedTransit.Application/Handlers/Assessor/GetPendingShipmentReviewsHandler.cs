using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.EntityFrameworkCore;

using MohamedTransit.Application.Helper;
using MohamedTransit.Domain.Common;
using MohamedTransit.Domain.Data;
using MohamedTransit.Domain.Entities;

using Transit.Application.Queries;


namespace Transit.Application.Handlers;

internal class GetPendingShipmentReviewsHandler : IRequestHandler<GetPendingShipmentReviewsQuery, OperationResult<List<Shipment>>>
{
    private readonly ApplicationDbContext _context;

    public GetPendingShipmentReviewsHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<List<Shipment>>> Handle(GetPendingShipmentReviewsQuery request, CancellationToken cancellationToken)
    {
        var result = new OperationResult<List<Shipment>>();

        // ለገባው Assessor የተመደቡ እና ገና ያልተገመገሙ (Pending/UnderReview) ጭነቶችን በሙሉ ማምጣት
        var pendingShipments = await _context.Shipments
            .Include(s => s.Importer)
            .Include(s => s.AssignedCaseExecutor)
            .Include(s => s.AssignedAssessor)
            .Include(s => s.Stages)
                .ThenInclude(stage => stage.StageComments)
            .Include(s => s.Stages)
                .ThenInclude(stage => stage.Documents)
            .Include(s => s.Documents)
            .Include(s => s.Messages)
            .Where(s => s.AssignedAssessorId == request.UserId &&
                       (s.Status == ShipmentStatus.Draft || s.Status == ShipmentStatus.UnderReview))
            .ToListAsync(cancellationToken);

        if (pendingShipments == null || !pendingShipments.Any())
        {
            result.Message = "No unreviewed shipment assigned to the importer was found.";
            result.Payload = new List<Shipment>();
            return result;
        }

        result.Payload = pendingShipments;
        result.Message = "Unreviewed shipments were successfully retrieved";
        return result;
    }
}
