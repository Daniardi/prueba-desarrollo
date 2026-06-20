using System.Text.Json;
using System.Text.Json.Serialization;
using CourierMax.Api.Domain;

namespace CourierMax.Api.Api;

public sealed class SpanishEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"El valor de {typeof(TEnum).Name} debe ser texto.");
        }

        var value = reader.GetString();
        if (SpanishEnumNames.TryParse(value, out TEnum result))
        {
            return result;
        }

        throw new JsonException($"El valor '{value}' no es valido para {typeof(TEnum).Name}.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(SpanishEnumNames.GetName(value));
    }
}

public static class SpanishEnumNames
{
    public static IReadOnlyCollection<string> GetNames(Type enumType)
    {
        if (enumType == typeof(ShipmentStatus))
        {
            return ["Creado", "Asignado", "EnTransito", "Entregado", "Cancelado"];
        }

        if (enumType == typeof(ServiceType))
        {
            return ["Estandar", "Express", "MismoDia"];
        }

        if (enumType == typeof(PackageType))
        {
            return ["Documento", "Paquete", "Fragil", "Perecedero"];
        }

        return Enum.GetNames(enumType);
    }

    public static string GetName<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        return value switch
        {
            ShipmentStatus.Created => "Creado",
            ShipmentStatus.Assigned => "Asignado",
            ShipmentStatus.InTransit => "EnTransito",
            ShipmentStatus.Delivered => "Entregado",
            ShipmentStatus.Cancelled => "Cancelado",
            ServiceType.Standard => "Estandar",
            ServiceType.Express => "Express",
            ServiceType.SameDay => "MismoDia",
            PackageType.Document => "Documento",
            PackageType.Package => "Paquete",
            PackageType.Fragile => "Fragil",
            PackageType.Perishable => "Perecedero",
            _ => value.ToString()
        };
    }

    public static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct, Enum
    {
        var normalized = Normalize(value);
        foreach (var enumValue in Enum.GetValues<TEnum>())
        {
            if (Normalize(GetName(enumValue)) == normalized || Normalize(enumValue.ToString()) == normalized)
            {
                result = enumValue;
                return true;
            }
        }

        result = default;
        return false;
    }

    private static string Normalize(string? value)
    {
        return (value ?? "").Trim()
            .Replace(" ", "", StringComparison.Ordinal)
            .Replace("á", "a", StringComparison.OrdinalIgnoreCase)
            .Replace("é", "e", StringComparison.OrdinalIgnoreCase)
            .Replace("í", "i", StringComparison.OrdinalIgnoreCase)
            .Replace("ó", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("ú", "u", StringComparison.OrdinalIgnoreCase)
            .ToUpperInvariant();
    }
}
