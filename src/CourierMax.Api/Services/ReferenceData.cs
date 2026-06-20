using CourierMax.Api.Domain;

namespace CourierMax.Api.Services;

public interface IReferenceData
{
    IReadOnlyCollection<string> Cities { get; }
    IReadOnlyCollection<Vehicle> Vehicles { get; }
    IReadOnlyCollection<Driver> Drivers { get; }
    DistanceRate GetDistanceRate(string originCity, string destinationCity);
    bool IsValidCity(string city);
}

public sealed record DistanceRate(string OriginCity, string DestinationCity, int DistanceKm, decimal Price);

public sealed class ReferenceData : IReferenceData
{
    private readonly List<DistanceRate> _distanceRates =
    [
        new("Bogota", "Medellin", 480, 12_000m),
        new("Bogota", "Cali", 360, 9_000m),
        new("Bogota", "Barranquilla", 950, 20_000m),
        new("Medellin", "Cali", 310, 8_000m),
        new("Medellin", "Barranquilla", 650, 15_000m),
        new("Cali", "Barranquilla", 900, 18_000m)
    ];

    public IReadOnlyCollection<string> Cities { get; } =
    [
        "Bogota",
        "Medellin",
        "Cali",
        "Barranquilla"
    ];

    public IReadOnlyCollection<Vehicle> Vehicles { get; } =
    [
        new(1, "ABC-123", 500m, 10m),
        new(2, "DEF-456", 300m, 6m),
        new(3, "GHI-789", 800m, 15m)
    ];

    public IReadOnlyCollection<Driver> Drivers { get; } =
    [
        new(1, "Juan Perez", 1),
        new(2, "Maria Lopez", 2),
        new(3, "Carlos Ruiz", 3)
    ];

    public bool IsValidCity(string city)
    {
        return Cities.Any(x => string.Equals(x, Normalize(city), StringComparison.OrdinalIgnoreCase));
    }

    public DistanceRate GetDistanceRate(string originCity, string destinationCity)
    {
        var origin = Normalize(originCity);
        var destination = Normalize(destinationCity);

        var rate = _distanceRates.FirstOrDefault(x =>
            (EqualsCity(x.OriginCity, origin) && EqualsCity(x.DestinationCity, destination)) ||
            (EqualsCity(x.OriginCity, destination) && EqualsCity(x.DestinationCity, origin)));

        return rate ?? throw new AppException(
            StatusCodes.Status400BadRequest,
            "No existe tarifa de distancia para el par de ciudades indicado.");
    }

    private static bool EqualsCity(string left, string right)
    {
        return string.Equals(left, Normalize(right), StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string value)
    {
        return value.Trim()
            .Replace("á", "a", StringComparison.OrdinalIgnoreCase)
            .Replace("é", "e", StringComparison.OrdinalIgnoreCase)
            .Replace("í", "i", StringComparison.OrdinalIgnoreCase)
            .Replace("ó", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("ú", "u", StringComparison.OrdinalIgnoreCase);
    }
}
