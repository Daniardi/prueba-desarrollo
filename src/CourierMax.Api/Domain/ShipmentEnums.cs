namespace CourierMax.Api.Domain;

public enum PackageType
{
    Document,
    Package,
    Fragile,
    Perishable
}

public enum ServiceType
{
    Standard,
    Express,
    SameDay
}

public enum ShipmentStatus
{
    Created,
    Assigned,
    InTransit,
    Delivered,
    Cancelled
}
