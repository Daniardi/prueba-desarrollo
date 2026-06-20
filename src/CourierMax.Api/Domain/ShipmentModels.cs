using System.Text.Json.Serialization;

namespace CourierMax.Api.Domain;

public sealed record Contact(string Name, string Phone, string Address);

public sealed record Dimensions(decimal LengthCm, decimal WidthCm, decimal HeightCm)
{
    public decimal VolumeM3 => LengthCm * WidthCm * HeightCm / 1_000_000m;
}

public sealed record PackageDetails(decimal WeightKg, Dimensions Dimensions, PackageType Type);

public sealed record ShipmentRoute(string OriginCity, string DestinationCity);

public sealed class Driver
{
    public Driver(int id, string name, int vehicleId, bool isActive = true)
    {
        Id = id;
        Name = name;
        VehicleId = vehicleId;
        IsActive = isActive;
    }

    public int Id { get; }
    public string Name { get; }
    public int VehicleId { get; }
    public bool IsActive { get; set; }
}

public sealed class Vehicle
{
    public Vehicle(int id, string plate, decimal maxWeightKg, decimal maxVolumeM3)
    {
        Id = id;
        Plate = plate;
        MaxWeightKg = maxWeightKg;
        MaxVolumeM3 = maxVolumeM3;
    }

    public int Id { get; }
    public string Plate { get; }
    public decimal MaxWeightKg { get; }
    public decimal MaxVolumeM3 { get; }
}

public sealed record StatusChange(
    [property: JsonPropertyName("estadoAnterior")] ShipmentStatus PreviousStatus,
    [property: JsonPropertyName("estadoNuevo")] ShipmentStatus NewStatus,
    [property: JsonPropertyName("fechaCambio")] DateTimeOffset ChangedAt,
    [property: JsonPropertyName("modificadoPor")] string ChangedBy,
    [property: JsonPropertyName("motivo")] string? Reason);

public sealed class Shipment
{
    public Shipment(
        Guid id,
        string trackingCode,
        Contact sender,
        Contact recipient,
        PackageDetails package,
        ServiceType serviceType,
        ShipmentRoute route,
        decimal price,
        DateTimeOffset createdAt)
    {
        Id = id;
        TrackingCode = trackingCode;
        Sender = sender;
        Recipient = recipient;
        Package = package;
        ServiceType = serviceType;
        Route = route;
        Price = price;
        CreatedAt = createdAt;
        Status = ShipmentStatus.Created;
    }

    public Guid Id { get; }
    public string TrackingCode { get; }
    public Contact Sender { get; }
    public Contact Recipient { get; }
    public PackageDetails Package { get; }
    public ServiceType ServiceType { get; }
    public ShipmentRoute Route { get; }
    public decimal Price { get; }
    public DateTimeOffset CreatedAt { get; }
    public ShipmentStatus Status { get; private set; }
    public int? DriverId { get; private set; }
    public int? VehicleId { get; private set; }
    public int? LastAssignedDriverId { get; private set; }
    public DateTimeOffset? AssignedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public List<StatusChange> History { get; } = [];

    public void Assign(int driverId, int vehicleId, DateTimeOffset changedAt, string changedBy)
    {
        DriverId = driverId;
        VehicleId = vehicleId;
        LastAssignedDriverId = driverId;
        AssignedAt = changedAt;
        ChangeStatus(ShipmentStatus.Assigned, changedAt, changedBy, null);
    }

    public void ChangeStatus(ShipmentStatus newStatus, DateTimeOffset changedAt, string changedBy, string? reason)
    {
        var previous = Status;
        Status = newStatus;

        if (newStatus == ShipmentStatus.Delivered)
        {
            DeliveredAt = changedAt;
        }

        if (newStatus == ShipmentStatus.Cancelled)
        {
            DriverId = null;
            VehicleId = null;
        }

        History.Add(new StatusChange(previous, newStatus, changedAt, changedBy, reason));
    }

    public void RestoreState(
        ShipmentStatus status,
        int? driverId,
        int? vehicleId,
        int? lastAssignedDriverId,
        DateTimeOffset? assignedAt,
        DateTimeOffset? deliveredAt,
        IEnumerable<StatusChange> history)
    {
        Status = status;
        DriverId = driverId;
        VehicleId = vehicleId;
        LastAssignedDriverId = lastAssignedDriverId;
        AssignedAt = assignedAt;
        DeliveredAt = deliveredAt;
        History.Clear();
        History.AddRange(history);
    }
}
