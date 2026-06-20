using CourierMax.Api.Api;
using CourierMax.Api.Domain;
using CourierMax.Api.Infrastructure;
using CourierMax.Api.Services;
using Microsoft.AspNetCore.Http;

namespace CourierMax.Tests;

public sealed class ShipmentServiceTests
{
    [Fact]
    public void Create_CalculatesPriceUsingReferenceExample()
    {
        var service = CreateService(new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero));

        var shipment = service.Create(ValidRequest(
            weightKg: 5m,
            packageType: PackageType.Fragile,
            serviceType: ServiceType.Express,
            origin: "Bogota",
            destination: "Medellin"));

        Assert.Equal(40_950m, shipment.Price);
        Assert.StartsWith("CM-", shipment.TrackingCode);
        Assert.Equal(11, shipment.TrackingCode.Length);
        Assert.Equal(ShipmentStatus.Created, shipment.Status);
    }

    [Fact]
    public void Create_RejectsInvalidColombianPhone()
    {
        var service = CreateService();
        var request = ValidRequest();
        request = request with { Sender = request.Sender with { Phone = "1234567890" } };

        var exception = Assert.Throws<AppException>(() => service.Create(request));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Contains("telefono", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Assign_RejectsVehicleWhenWeightCapacityIsExceeded()
    {
        var service = CreateService();

        for (var i = 0; i < 3; i++)
        {
            var shipment = service.Create(ValidRequest(weightKg: 100m));
            service.Assign(shipment.Id, new AssignShipmentRequest(2, "dispatcher"));
        }

        var extraShipment = service.Create(ValidRequest(weightKg: 1m));

        var exception = Assert.Throws<AppException>(() =>
            service.Assign(extraShipment.Id, new AssignShipmentRequest(2, "dispatcher")));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.Contains("peso", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Assign_WithoutDriver_SelectsLeastLoadedVehicleWithCapacity()
    {
        var service = CreateService();

        var shipment = service.Create(ValidRequest(weightKg: 50m));
        var assigned = service.Assign(shipment.Id, new AssignShipmentRequest(null, "dispatcher"));

        Assert.Equal(2, assigned.DriverId);
        Assert.Equal(2, assigned.VehicleId);
    }

    [Fact]
    public void ChangeStatus_RequiresValidFlowAndCancellationReason()
    {
        var service = CreateService();
        var shipment = service.Create(ValidRequest());

        var invalidFlow = Assert.Throws<AppException>(() =>
            service.ChangeStatus(shipment.Id, new ChangeStatusRequest(ShipmentStatus.Delivered, "driver-1", null)));
        Assert.Equal(StatusCodes.Status409Conflict, invalidFlow.StatusCode);

        var invalidCancel = Assert.Throws<AppException>(() =>
            service.ChangeStatus(shipment.Id, new ChangeStatusRequest(ShipmentStatus.Cancelled, "driver-1", "bad")));
        Assert.Equal(StatusCodes.Status400BadRequest, invalidCancel.StatusCode);

        var cancelled = service.ChangeStatus(
            shipment.Id,
            new ChangeStatusRequest(ShipmentStatus.Cancelled, "driver-1", "Cliente cancela solicitud"));

        Assert.Equal(ShipmentStatus.Cancelled, cancelled.Status);
        Assert.Single(cancelled.History);
    }

    [Fact]
    public void LateShipments_UsesBusinessDaysAndColombianHolidays()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 6, 12, 9, 0, 0, TimeSpan.Zero));
        var service = CreateService(clock);
        var shipment = service.Create(ValidRequest(serviceType: ServiceType.Express));

        clock.SetUtcNow(new DateTimeOffset(2026, 6, 17, 9, 0, 0, TimeSpan.Zero));

        var late = service.GetLateShipments(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30));

        Assert.Contains(late, x => x.Id == shipment.Id);
    }

    [Fact]
    public void Metrics_ReturnsDriverEfficiency()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 9, 0, 0, TimeSpan.Zero));
        var service = CreateService(clock);
        var deliveredShipment = service.Create(ValidRequest(weightKg: 10m));

        service.Assign(deliveredShipment.Id, new AssignShipmentRequest(1, "dispatcher"));
        service.ChangeStatus(deliveredShipment.Id, new ChangeStatusRequest(ShipmentStatus.InTransit, "driver-1", null));
        clock.SetUtcNow(new DateTimeOffset(2026, 6, 16, 9, 0, 0, TimeSpan.Zero));
        service.ChangeStatus(deliveredShipment.Id, new ChangeStatusRequest(ShipmentStatus.Delivered, "driver-1", null));

        var cancelledShipment = service.Create(ValidRequest(weightKg: 2m));
        service.Assign(cancelledShipment.Id, new AssignShipmentRequest(1, "dispatcher"));
        service.ChangeStatus(cancelledShipment.Id, new ChangeStatusRequest(ShipmentStatus.Cancelled, "dispatcher", "Cliente ausente"));

        var metrics = service.GetDriverMetrics(1);

        Assert.Equal(2, metrics.TotalAssigned);
        Assert.Equal(1, metrics.Delivered);
        Assert.Equal(1, metrics.Cancelled);
        Assert.Equal(12m, metrics.TotalWeightKg);
        Assert.Equal(100m, metrics.OnTimeDeliveryPercentage);
    }

    private static IShipmentService CreateService(DateTimeOffset? now = null)
    {
        return CreateService(new FakeTimeProvider(now ?? new DateTimeOffset(2026, 6, 15, 9, 0, 0, TimeSpan.Zero)));
    }

    private static IShipmentService CreateService(FakeTimeProvider clock)
    {
        var referenceData = new ReferenceData();
        return new ShipmentService(
            new InMemoryShipmentStore(),
            referenceData,
            new PricingService(referenceData),
            new ColombianBusinessCalendar(),
            clock);
    }

    private static CreateShipmentRequest ValidRequest(
        decimal weightKg = 1m,
        PackageType packageType = PackageType.Package,
        ServiceType serviceType = ServiceType.Standard,
        string origin = "Bogota",
        string destination = "Cali")
    {
        return new CreateShipmentRequest(
            new ContactRequest("Laura Gomez", "3001234567", "Calle 1 # 2-3"),
            new ContactRequest("Andres Ruiz", "6012345678", "Carrera 4 # 5-6"),
            new PackageRequest(weightKg, new DimensionsRequest(10m, 10m, 10m), packageType),
            serviceType,
            origin,
            destination);
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public void SetUtcNow(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }
    }
}
