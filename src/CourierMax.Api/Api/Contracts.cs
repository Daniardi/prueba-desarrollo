using CourierMax.Api.Domain;
using System.Text.Json.Serialization;

namespace CourierMax.Api.Api;

public sealed record ContactRequest(
    [property: JsonPropertyName("nombre")] string Name,
    [property: JsonPropertyName("telefono")] string Phone,
    [property: JsonPropertyName("direccion")] string Address);

public sealed record DimensionsRequest(
    [property: JsonPropertyName("largoCm")] decimal LengthCm,
    [property: JsonPropertyName("anchoCm")] decimal WidthCm,
    [property: JsonPropertyName("altoCm")] decimal HeightCm);

public sealed record PackageRequest(
    [property: JsonPropertyName("pesoKg")] decimal WeightKg,
    [property: JsonPropertyName("dimensiones")] DimensionsRequest Dimensions,
    [property: JsonPropertyName("tipo")] PackageType Type);

public sealed record CreateShipmentRequest(
    [property: JsonPropertyName("remitente")] ContactRequest Sender,
    [property: JsonPropertyName("destinatario")] ContactRequest Recipient,
    [property: JsonPropertyName("paquete")] PackageRequest Package,
    [property: JsonPropertyName("tipoServicio")] ServiceType ServiceType,
    [property: JsonPropertyName("ciudadOrigen")] string OriginCity,
    [property: JsonPropertyName("ciudadDestino")] string DestinationCity);

public sealed record AssignShipmentRequest(
    [property: JsonPropertyName("conductorId")] int? DriverId,
    [property: JsonPropertyName("modificadoPor")] string ChangedBy);

public sealed record ChangeStatusRequest(
    [property: JsonPropertyName("nuevoEstado")] ShipmentStatus NewStatus,
    [property: JsonPropertyName("modificadoPor")] string ChangedBy,
    [property: JsonPropertyName("motivo")] string? Reason);

public sealed record ShipmentResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("codigoRastreo")] string TrackingCode,
    [property: JsonPropertyName("estado")] ShipmentStatus Status,
    [property: JsonPropertyName("tipoServicio")] ServiceType ServiceType,
    [property: JsonPropertyName("tipoPaquete")] PackageType PackageType,
    [property: JsonPropertyName("pesoKg")] decimal WeightKg,
    [property: JsonPropertyName("volumenM3")] decimal VolumeM3,
    [property: JsonPropertyName("ciudadOrigen")] string OriginCity,
    [property: JsonPropertyName("ciudadDestino")] string DestinationCity,
    [property: JsonPropertyName("precio")] decimal Price,
    [property: JsonPropertyName("fechaCreacion")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("conductorId")] int? DriverId,
    [property: JsonPropertyName("vehiculoId")] int? VehicleId,
    [property: JsonPropertyName("historial")] IReadOnlyCollection<StatusChange> History);

public sealed record VehicleLoadResponse(
    [property: JsonPropertyName("vehiculoId")] int VehicleId,
    [property: JsonPropertyName("placa")] string Plate,
    [property: JsonPropertyName("conductorId")] int DriverId,
    [property: JsonPropertyName("nombreConductor")] string DriverName,
    [property: JsonPropertyName("pesoUtilizadoKg")] decimal UsedWeightKg,
    [property: JsonPropertyName("pesoMaximoKg")] decimal MaxWeightKg,
    [property: JsonPropertyName("volumenUtilizadoM3")] decimal UsedVolumeM3,
    [property: JsonPropertyName("volumenMaximoM3")] decimal MaxVolumeM3);

public sealed record DriverMetricsResponse(
    [property: JsonPropertyName("conductorId")] int DriverId,
    [property: JsonPropertyName("nombreConductor")] string DriverName,
    [property: JsonPropertyName("totalAsignados")] int TotalAssigned,
    [property: JsonPropertyName("entregados")] int Delivered,
    [property: JsonPropertyName("cancelados")] int Cancelled,
    [property: JsonPropertyName("enTransito")] int InTransit,
    [property: JsonPropertyName("promedioDiasEntrega")] decimal AverageDeliveryDays,
    [property: JsonPropertyName("porcentajeEntregasATiempo")] decimal OnTimeDeliveryPercentage,
    [property: JsonPropertyName("pesoTotalKg")] decimal TotalWeightKg);

public sealed record ProblemResponse(
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("detalle")] string? Detail = null);

public sealed record ReferenceDataResponse(
    [property: JsonPropertyName("ciudades")] IReadOnlyCollection<string> Cities,
    [property: JsonPropertyName("conductores")] IReadOnlyCollection<DriverResponse> Drivers,
    [property: JsonPropertyName("vehiculos")] IReadOnlyCollection<VehicleResponse> Vehicles);

public sealed record DriverResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("nombre")] string Name,
    [property: JsonPropertyName("vehiculoId")] int VehicleId,
    [property: JsonPropertyName("activo")] bool IsActive);

public sealed record VehicleResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("placa")] string Plate,
    [property: JsonPropertyName("pesoMaximoKg")] decimal MaxWeightKg,
    [property: JsonPropertyName("volumenMaximoM3")] decimal MaxVolumeM3);
