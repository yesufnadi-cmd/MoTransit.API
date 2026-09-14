using MediatR;

using Microsoft.EntityFrameworkCore;

using MohamedTransit.Application.Commands.Shipment;
using MohamedTransit.Application.DTO;
using MohamedTransit.Domain.Common;
using MohamedTransit.Domain.Data;
using MohamedTransit.Domain.Entities;

namespace MohamedTransit.Application.Handlers.Shipment;

public class CreateShipmentCommandHandler
    : IRequestHandler<CreateServiceCommand, ShipmentDto>
{
    private readonly ApplicationDbContext _context;

    public CreateShipmentCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ShipmentDto> Handle(
        CreateServiceCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Importer በዳታቤዝ ውስጥ መኖሩን ማረጋገጥ (AsNoTracking በመጠቀም Lock እንዳይፈጥር ማድረግ)
        var importerExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.ImporterId, cancellationToken);

        if (!importerExists)
        {
            throw new KeyNotFoundException($"Importer with ID {request.ImporterId} does not exist.");
        }

        // 2. Unique Tracking Number ማፍለቅ
        long generatedLongId = DateTime.UtcNow.Ticks;
        var shipment = MohamedTransit.Domain.Entities.Shipment.Create(
                    $"MT-{generatedLongId}",
                    request.Description,
                    request.ImporterId,
                    request.Origin,
                    request.Mode,
                    request.Mode == TransportMode.MultiModalSeaRail
                        ? HubLocation.Mojo
                        : HubLocation.Adama,
                    request.Destination,
                    "Ethiopia",             // 8. country (እንደ ቋሚ ጽሁፍ)
                    request.ImporterId      // 9. createdById (ከ request.ImporterId ሊወሰድ ይችላል ወይም ሌላ ID)
                );

        _context.Shipments.Add(shipment);

        await _context.SaveChangesAsync(cancellationToken);

        return new ShipmentDto(
            shipment.Id,
            shipment.TrackingNumber,
            shipment.ImporterId,
            shipment.Description,
            shipment.Mode.ToString(),
            shipment.AssignedHub.ToString(),
            shipment.Status.ToString(),
            shipment.Origin,
            shipment.Destination,
            shipment.CreateAt,
            shipment.UpdatedAt
        );
    }
}
