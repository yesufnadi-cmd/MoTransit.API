namespace MohamedTransit.Domain.Common;



public enum HubLocation
{
    Djibouti = 1,
    Mojo = 2,
    Adama = 3
}
public enum ShipmentStatus
{
    Registered = 1,
    InTransitDjibouti = 2,
    CustomsProcessing = 3,
    Released = 4,
    InTransitDestination = 5,
    ArrivedAtHub = 6,
    Delivered = 7,
    Cancelled = 8
}
public enum TransportMode
{
    MultiModalSeaRail = 1,
    UniModalRoad = 2
}
public enum RecordStatus
{
    InActive = 1,
    Active = 2,
    Delete = 3
}

public enum AccountStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Locked = 4
}



