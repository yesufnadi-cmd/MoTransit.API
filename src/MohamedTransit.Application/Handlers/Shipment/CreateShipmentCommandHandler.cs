using MediatR;

using Microsoft.EntityFrameworkCore;

using MohamedTransit.Application.Commands.Shipment;
using MohamedTransit.Application.DTO;
using MohamedTransit.Domain.Common;
using MohamedTransit.Domain.Data;
using MohamedTransit.Domain.Entities;

namespace MohamedTransit.Application.Handlers.Shipment;

public class CreateShipmentCommandHandler
    : IRequestHandler<CreateShipmentCommand, ShipmentDto>
{
    private readonly ApplicationDbContext _context;

    public CreateShipmentCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ShipmentDto> Handle(
        CreateShipmentCommand request,
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
            request.ImporterId,
            request.Description,
            request.Mode,
            request.Mode == TransportMode.MultiModalSeaRail
                ? HubLocation.Mojo
                : HubLocation.Adama,
            request.Origin,
            request.Destination
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
