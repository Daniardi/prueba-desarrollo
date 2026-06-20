using CourierMax.Api.Domain;

namespace CourierMax.Api.Infrastructure;

public static class DatabaseSeeder
{
    public static void Seed(CourierMaxDbContext database)
    {
        if (!database.Vehicles.Any())
        {
            database.Vehicles.AddRange(
                new VehicleEntity { Id = 1, Plate = "ABC-123", MaxWeightKg = 500m, MaxVolumeM3 = 10m },
                new VehicleEntity { Id = 2, Plate = "DEF-456", MaxWeightKg = 300m, MaxVolumeM3 = 6m },
                new VehicleEntity { Id = 3, Plate = "GHI-789", MaxWeightKg = 800m, MaxVolumeM3 = 15m });
            database.Drivers.AddRange(
                new DriverEntity { Id = 1, Name = "Juan Perez", VehicleId = 1, IsActive = true },
                new DriverEntity { Id = 2, Name = "Maria Lopez", VehicleId = 2, IsActive = true },
                new DriverEntity { Id = 3, Name = "Carlos Ruiz", VehicleId = 3, IsActive = true });
        }

        if (!database.Cities.Any())
        {
            database.Cities.AddRange(
                new CityEntity { Id = 1, Name = "Bogota" },
                new CityEntity { Id = 2, Name = "Medellin" },
                new CityEntity { Id = 3, Name = "Cali" },
                new CityEntity { Id = 4, Name = "Barranquilla" });
            database.DistanceRates.AddRange(
                Rate(1, "Bogota", "Medellin", 480, 12_000m),
                Rate(2, "Bogota", "Cali", 360, 9_000m),
                Rate(3, "Bogota", "Barranquilla", 950, 20_000m),
                Rate(4, "Medellin", "Cali", 310, 8_000m),
                Rate(5, "Medellin", "Barranquilla", 650, 15_000m),
                Rate(6, "Cali", "Barranquilla", 900, 18_000m));
        }

        database.SaveChanges();

        if (database.Shipments.Any())
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var created = Sample(new Guid("11111111-1111-1111-1111-111111111111"), "CM-10000001", now.AddDays(-1), ServiceType.Express, 4.5m);
        var assigned = Sample(new Guid("22222222-2222-2222-2222-222222222222"), "CM-10000002", now.AddDays(-2), ServiceType.Standard, 12m);
        assigned.Assign(2, 2, now.AddDays(-1), "seed");
        var transit = Sample(new Guid("33333333-3333-3333-3333-333333333333"), "CM-10000003", now.AddDays(-4), ServiceType.Standard, 20m);
        transit.Assign(1, 1, now.AddDays(-3), "seed");
        transit.ChangeStatus(ShipmentStatus.InTransit, now.AddDays(-2), "seed", null);
        var delivered = Sample(new Guid("44444444-4444-4444-4444-444444444444"), "CM-10000004", now.AddDays(-6), ServiceType.Standard, 8m);
        delivered.Assign(3, 3, now.AddDays(-5), "seed");
        delivered.ChangeStatus(ShipmentStatus.InTransit, now.AddDays(-4), "seed", null);
        delivered.ChangeStatus(ShipmentStatus.Delivered, now.AddDays(-2), "seed", null);

        database.Shipments.AddRange(
            SqlShipmentStore.ToEntity(created),
            SqlShipmentStore.ToEntity(assigned),
            SqlShipmentStore.ToEntity(transit),
            SqlShipmentStore.ToEntity(delivered));
        database.SaveChanges();
    }

    private static DistanceRateEntity Rate(int id, string origin, string destination, int km, decimal price) =>
        new() { Id = id, OriginCity = origin, DestinationCity = destination, DistanceKm = km, Price = price };

    private static Shipment Sample(Guid id, string trackingCode, DateTimeOffset createdAt, ServiceType serviceType, decimal weight)
    {
        return new Shipment(
            id,
            trackingCode,
            new Contact("Laura Gomez", "3001234567", "Calle 10 # 20-30"),
            new Contact("Andres Ruiz", "6012345678", "Carrera 7 # 40-50"),
            new PackageDetails(weight, new Dimensions(30m, 20m, 15m), PackageType.Package),
            serviceType,
            new ShipmentRoute("Bogota", "Medellin"),
            serviceType == ServiceType.Express ? 31_000m : 24_000m,
            createdAt);
    }
}
