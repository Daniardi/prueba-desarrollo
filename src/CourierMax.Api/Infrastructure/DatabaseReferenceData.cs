using CourierMax.Api.Domain;
using CourierMax.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CourierMax.Api.Infrastructure;

public sealed class DatabaseReferenceData(CourierMaxDbContext database) : IReferenceData
{
    public IReadOnlyCollection<string> Cities =>
        database.Cities.AsNoTracking().OrderBy(x => x.Id).Select(x => x.Name).ToArray();

    public IReadOnlyCollection<Vehicle> Vehicles =>
        database.Vehicles.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new Vehicle(x.Id, x.Plate, x.MaxWeightKg, x.MaxVolumeM3)).ToArray();

    public IReadOnlyCollection<Driver> Drivers =>
        database.Drivers.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new Driver(x.Id, x.Name, x.VehicleId, x.IsActive)).ToArray();

    public bool IsValidCity(string city)
    {
        var normalized = ReferenceData.Normalize(city);
        return Cities.Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public DistanceRate GetDistanceRate(string originCity, string destinationCity)
    {
        var origin = ReferenceData.Normalize(originCity);
        var destination = ReferenceData.Normalize(destinationCity);
        var rate = database.DistanceRates.AsNoTracking().AsEnumerable().FirstOrDefault(x =>
            (EqualsCity(x.OriginCity, origin) && EqualsCity(x.DestinationCity, destination)) ||
            (EqualsCity(x.OriginCity, destination) && EqualsCity(x.DestinationCity, origin)));

        return rate is null
            ? throw new AppException(StatusCodes.Status400BadRequest, "No existe tarifa de distancia para el par de ciudades indicado.")
            : new DistanceRate(rate.OriginCity, rate.DestinationCity, rate.DistanceKm, rate.Price);
    }

    private static bool EqualsCity(string left, string right) =>
        string.Equals(ReferenceData.Normalize(left), ReferenceData.Normalize(right), StringComparison.OrdinalIgnoreCase);
}
