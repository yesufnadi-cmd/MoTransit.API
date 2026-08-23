using MohamedTransit.Domain.Common;

namespace MohamedTransit.Domain.Entities;

public class Shipment : BaseEntity
{
    // Public Properties ከ Private Set ጋር (Encapsulation)
    public string TrackingNumber { get; private set; } = string.Empty;
    public long ImporterId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public TransportMode Mode { get; private set; }
    public HubLocation AssignedHub { get; private set; }
    public ShipmentStatus Status { get; private set; } = ShipmentStatus.Registered;

    // Value Object Properties (Route)
    public string Origin { get; private set; } = string.Empty;
    public string Destination { get; private set; } = string.Empty;

    // A constructor that EF Core requires to create an object when materializing data retrieved from the database.
    private Shipment() { }

    // Static Factory Method (to create new shipment)
    public static Shipment Create(
        string trackingNumber,
        long importerId,
        string description,
        TransportMode mode,
        HubLocation assignedHub,
        string origin,
        string destination)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            throw new ArgumentException("Tracking number is required.", nameof(trackingNumber));

        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException("Origin and Destination cannot be empty.");

        return new Shipment
        {
            TrackingNumber = trackingNumber,
            ImporterId = importerId,
            Description = description,
            Mode = mode,
            AssignedHub = assignedHub,
            Origin = origin,
            Destination = destination,
            Status = ShipmentStatus.Registered
        };
    }
    public void UpdateStatus(ShipmentStatus newStatus)
    {
        Status = newStatus;
    }

}

