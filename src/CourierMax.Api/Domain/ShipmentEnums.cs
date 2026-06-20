using System.Text.Json.Serialization;
using CourierMax.Api.Api;

namespace CourierMax.Api.Domain;

[JsonConverter(typeof(SpanishEnumConverter<PackageType>))]
public enum PackageType
{
    Document,
    Package,
    Fragile,
    Perishable
}

[JsonConverter(typeof(SpanishEnumConverter<ServiceType>))]
public enum ServiceType
{
    Standard,
    Express,
    SameDay
}

[JsonConverter(typeof(SpanishEnumConverter<ShipmentStatus>))]
public enum ShipmentStatus
{
    Created,
    Assigned,
    InTransit,
    Delivered,
    Cancelled
}
