using CourierMax.Api.Domain;

namespace CourierMax.Api.Api;

public sealed record ContactRequest(string Name, string Phone, string Address);

public sealed record DimensionsRequest(decimal LengthCm, decimal WidthCm, decimal HeightCm);

public sealed record PackageRequest(decimal WeightKg, DimensionsRequest Dimensions, PackageType Type);

public sealed record CreateShipmentRequest(
    ContactRequest Sender,
    ContactRequest Recipient,
    PackageRequest Package,
    ServiceType ServiceType,
    string OriginCity,
    string DestinationCity);

public sealed record AssignShipmentRequest(int? DriverId, string ChangedBy);

public sealed record ChangeStatusRequest(ShipmentStatus NewStatus, string ChangedBy, string? Reason);

public sealed record ShipmentResponse(
    Guid Id,
    string TrackingCode,
    ShipmentStatus Status,
    ServiceType ServiceType,
    PackageType PackageType,
    decimal WeightKg,
    decimal VolumeM3,
    string OriginCity,
    string DestinationCity,
    decimal Price,
    DateTimeOffset CreatedAt,
    int? DriverId,
    int? VehicleId,
    IReadOnlyCollection<StatusChange> History);

public sealed record VehicleLoadResponse(
    int VehicleId,
    string Plate,
    int DriverId,
    string DriverName,
    decimal UsedWeightKg,
    decimal MaxWeightKg,
    decimal UsedVolumeM3,
    decimal MaxVolumeM3);

public sealed record DriverMetricsResponse(
    int DriverId,
    string DriverName,
    int TotalAssigned,
    int Delivered,
    int Cancelled,
    int InTransit,
    decimal AverageDeliveryDays,
    decimal OnTimeDeliveryPercentage,
    decimal TotalWeightKg);

public sealed record ProblemResponse(string Error, string? Detail = null);
