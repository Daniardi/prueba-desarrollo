using System.Text.RegularExpressions;
using CourierMax.Api.Api;
using CourierMax.Api.Domain;
using CourierMax.Api.Infrastructure;

namespace CourierMax.Api.Services;

public interface IShipmentService
{
    Shipment Create(CreateShipmentRequest request);
    Shipment Get(Guid id);
    IReadOnlyCollection<Shipment> GetAll();
    Shipment Assign(Guid shipmentId, AssignShipmentRequest request);
    Shipment ChangeStatus(Guid shipmentId, ChangeStatusRequest request);
    IReadOnlyCollection<Shipment> GetLateShipments(DateOnly from, DateOnly to);
    DriverMetricsResponse GetDriverMetrics(int driverId);
    IReadOnlyCollection<VehicleLoadResponse> GetVehicleLoads();
}

public sealed partial class ShipmentService(
    IShipmentStore store,
    IReferenceData referenceData,
    IPricingService pricingService,
    IBusinessCalendar businessCalendar,
    TimeProvider timeProvider) : IShipmentService
{
    private static readonly HashSet<ShipmentStatus> CapacityConsumingStatuses =
    [
        ShipmentStatus.Assigned,
        ShipmentStatus.InTransit
    ];

    public Shipment Create(CreateShipmentRequest request)
    {
        ValidateCreateRequest(request);

        var sender = ToContact(request.Sender);
        var recipient = ToContact(request.Recipient);
        var package = new PackageDetails(
            request.Package.WeightKg,
            new Dimensions(
                request.Package.Dimensions.LengthCm,
                request.Package.Dimensions.WidthCm,
                request.Package.Dimensions.HeightCm),
            request.Package.Type);
        var route = new ShipmentRoute(
            ReferenceData.Normalize(request.OriginCity),
            ReferenceData.Normalize(request.DestinationCity));
        var price = pricingService.Calculate(package, request.ServiceType, route);

        var shipment = new Shipment(
            Guid.NewGuid(),
            GenerateTrackingCode(),
            sender,
            recipient,
            package,
            request.ServiceType,
            route,
            price,
            timeProvider.GetUtcNow());

        store.Add(shipment);
        return shipment;
    }

    public Shipment Get(Guid id)
    {
        return store.GetById(id)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Envio no encontrado.");
    }

    public IReadOnlyCollection<Shipment> GetAll()
    {
        return store.GetAll();
    }

    public Shipment Assign(Guid shipmentId, AssignShipmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ChangedBy))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "El usuario que realiza el cambio es obligatorio.");
        }

        var shipment = Get(shipmentId);
        EnsureTransition(shipment.Status, ShipmentStatus.Assigned, null);

        var driver = request.DriverId is null
            ? SelectLeastLoadedDriver(shipment)
            : FindDriver(request.DriverId.Value);

        if (!driver.IsActive)
        {
            throw new AppException(StatusCodes.Status409Conflict, "El conductor seleccionado no esta activo.");
        }

        var vehicle = FindVehicle(driver.VehicleId);
        EnsureVehicleCapacity(vehicle, shipment);

        shipment.Assign(driver.Id, vehicle.Id, timeProvider.GetUtcNow(), request.ChangedBy);
        store.Update(shipment);
        return shipment;
    }

    public Shipment ChangeStatus(Guid shipmentId, ChangeStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ChangedBy))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "El usuario que realiza el cambio es obligatorio.");
        }

        var shipment = Get(shipmentId);
        EnsureTransition(shipment.Status, request.NewStatus, request.Reason);
        shipment.ChangeStatus(request.NewStatus, timeProvider.GetUtcNow(), request.ChangedBy, request.Reason);
        store.Update(shipment);
        return shipment;
    }

    public IReadOnlyCollection<Shipment> GetLateShipments(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "La fecha inicial no puede ser mayor que la final.");
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        return store.GetAll()
            .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= from)
            .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= to)
            .Where(x => x.Status != ShipmentStatus.Delivered)
            .Where(x => IsLate(x, today))
            .OrderBy(x => x.CreatedAt)
            .ToArray();
    }

    public DriverMetricsResponse GetDriverMetrics(int driverId)
    {
        var driver = FindDriver(driverId);
        var shipments = store.GetAll()
            .Where(x => x.LastAssignedDriverId == driverId)
            .ToArray();

        var delivered = shipments.Where(x => x.Status == ShipmentStatus.Delivered).ToArray();
        var totalAssigned = shipments.Count(x => x.History.Any(h => h.NewStatus == ShipmentStatus.Assigned));
        var onTimeDelivered = delivered.Count(IsDeliveredOnTime);
        var averageDeliveryDays = delivered.Length == 0
            ? 0m
            : delivered.Average(x => (decimal)(x.DeliveredAt!.Value - x.AssignedAt!.Value).TotalDays);

        return new DriverMetricsResponse(
            driver.Id,
            driver.Name,
            totalAssigned,
            delivered.Length,
            shipments.Count(x => x.Status == ShipmentStatus.Cancelled),
            shipments.Count(x => x.Status == ShipmentStatus.InTransit),
            decimal.Round(averageDeliveryDays, 2),
            delivered.Length == 0 ? 0m : decimal.Round(onTimeDelivered * 100m / delivered.Length, 2),
            shipments.Sum(x => x.Package.WeightKg));
    }

    public IReadOnlyCollection<VehicleLoadResponse> GetVehicleLoads()
    {
        return referenceData.Drivers
            .Select(driver =>
            {
                var vehicle = FindVehicle(driver.VehicleId);
                var activeShipments = GetCapacityShipments(vehicle.Id).ToArray();

                return new VehicleLoadResponse(
                    vehicle.Id,
                    vehicle.Plate,
                    driver.Id,
                    driver.Name,
                    activeShipments.Sum(x => x.Package.WeightKg),
                    vehicle.MaxWeightKg,
                    activeShipments.Sum(x => x.Package.Dimensions.VolumeM3),
                    vehicle.MaxVolumeM3);
            })
            .OrderBy(x => x.VehicleId)
            .ToArray();
    }

    private static Contact ToContact(ContactRequest request)
    {
        return new Contact(request.Name.Trim(), request.Phone.Trim(), request.Address.Trim());
    }

    private void ValidateCreateRequest(CreateShipmentRequest request)
    {
        ValidateContact(request.Sender, "remitente");
        ValidateContact(request.Recipient, "destinatario");
        ValidatePackage(request.Package);

        if (!referenceData.IsValidCity(request.OriginCity) || !referenceData.IsValidCity(request.DestinationCity))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Origen y destino deben ser ciudades validas del sistema.");
        }

        if (string.Equals(
            ReferenceData.Normalize(request.OriginCity),
            ReferenceData.Normalize(request.DestinationCity),
            StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Origen y destino deben ser diferentes.");
        }

        referenceData.GetDistanceRate(request.OriginCity, request.DestinationCity);
    }

    private static void ValidateContact(ContactRequest contact, string label)
    {
        if (string.IsNullOrWhiteSpace(contact.Name))
        {
            throw new AppException(StatusCodes.Status400BadRequest, $"El nombre del {label} es obligatorio.");
        }

        if (!ColombianPhoneRegex().IsMatch(contact.Phone ?? string.Empty))
        {
            throw new AppException(StatusCodes.Status400BadRequest, $"El telefono del {label} debe tener 10 digitos e iniciar con 3 o 6.");
        }

        if (string.IsNullOrWhiteSpace(contact.Address))
        {
            throw new AppException(StatusCodes.Status400BadRequest, $"La direccion del {label} es obligatoria.");
        }
    }

    private static void ValidatePackage(PackageRequest package)
    {
        if (package.WeightKg is < 0.1m or > 100m)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "El peso debe estar entre 0.1 kg y 100 kg.");
        }

        ValidateDimension(package.Dimensions.LengthCm, "largo");
        ValidateDimension(package.Dimensions.WidthCm, "ancho");
        ValidateDimension(package.Dimensions.HeightCm, "alto");
    }

    private static void ValidateDimension(decimal value, string label)
    {
        if (value is < 1m or > 200m)
        {
            throw new AppException(StatusCodes.Status400BadRequest, $"La dimension {label} debe estar entre 1 cm y 200 cm.");
        }
    }

    private string GenerateTrackingCode()
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = $"CM-{Random.Shared.Next(0, 100_000_000):D8}";
            if (store.GetByTrackingCode(code) is null)
            {
                return code;
            }
        }

        throw new AppException(StatusCodes.Status500InternalServerError, "No fue posible generar un codigo de rastreo unico.");
    }

    private Driver SelectLeastLoadedDriver(Shipment shipment)
    {
        var candidates = referenceData.Drivers
            .Where(x => x.IsActive)
            .Select(driver =>
            {
                var vehicle = FindVehicle(driver.VehicleId);
                var activeShipments = GetCapacityShipments(vehicle.Id).ToArray();
                var usedWeight = activeShipments.Sum(x => x.Package.WeightKg);
                var usedVolume = activeShipments.Sum(x => x.Package.Dimensions.VolumeM3);

                return new
                {
                    Driver = driver,
                    Vehicle = vehicle,
                    UsedWeight = usedWeight,
                    UsedVolume = usedVolume,
                    LoadRatio = Math.Max(
                        vehicle.MaxWeightKg == 0 ? 1 : usedWeight / vehicle.MaxWeightKg,
                        vehicle.MaxVolumeM3 == 0 ? 1 : usedVolume / vehicle.MaxVolumeM3)
                };
            })
            .Where(x => x.UsedWeight + shipment.Package.WeightKg <= x.Vehicle.MaxWeightKg)
            .Where(x => x.UsedVolume + shipment.Package.Dimensions.VolumeM3 <= x.Vehicle.MaxVolumeM3)
            .OrderBy(x => x.LoadRatio)
            .ThenBy(x => x.Vehicle.MaxWeightKg)
            .ToArray();

        return candidates.FirstOrDefault()?.Driver
            ?? throw new AppException(StatusCodes.Status409Conflict, "No hay vehiculos activos con capacidad disponible para este envio.");
    }

    private Driver FindDriver(int driverId)
    {
        return referenceData.Drivers.FirstOrDefault(x => x.Id == driverId)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Conductor no encontrado.");
    }

    private Vehicle FindVehicle(int vehicleId)
    {
        return referenceData.Vehicles.FirstOrDefault(x => x.Id == vehicleId)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Vehiculo no encontrado.");
    }

    private void EnsureVehicleCapacity(Vehicle vehicle, Shipment shipment)
    {
        var activeShipments = GetCapacityShipments(vehicle.Id).ToArray();
        var usedWeight = activeShipments.Sum(x => x.Package.WeightKg);
        var usedVolume = activeShipments.Sum(x => x.Package.Dimensions.VolumeM3);

        if (usedWeight + shipment.Package.WeightKg > vehicle.MaxWeightKg)
        {
            throw new AppException(StatusCodes.Status409Conflict, "La asignacion excede la capacidad maxima de peso del vehiculo.");
        }

        if (usedVolume + shipment.Package.Dimensions.VolumeM3 > vehicle.MaxVolumeM3)
        {
            throw new AppException(StatusCodes.Status409Conflict, "La asignacion excede la capacidad maxima de volumen del vehiculo.");
        }
    }

    private IEnumerable<Shipment> GetCapacityShipments(int vehicleId)
    {
        return store.GetAll()
            .Where(x => x.VehicleId == vehicleId)
            .Where(x => CapacityConsumingStatuses.Contains(x.Status));
    }

    private static void EnsureTransition(ShipmentStatus current, ShipmentStatus target, string? reason)
    {
        if (current == ShipmentStatus.Delivered && target == ShipmentStatus.Cancelled)
        {
            throw new AppException(StatusCodes.Status409Conflict, "Un envio entregado no puede cancelarse.");
        }

        if (target == ShipmentStatus.Cancelled)
        {
            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
            {
                throw new AppException(StatusCodes.Status400BadRequest, "La cancelacion requiere un motivo de minimo 5 caracteres.");
            }

            return;
        }

        var allowed = current switch
        {
            ShipmentStatus.Created => target == ShipmentStatus.Assigned,
            ShipmentStatus.Assigned => target == ShipmentStatus.InTransit,
            ShipmentStatus.InTransit => target == ShipmentStatus.Delivered,
            _ => false
        };

        if (!allowed)
        {
            throw new AppException(StatusCodes.Status409Conflict, $"Transicion de estado invalida: {current} -> {target}.");
        }
    }

    private bool IsLate(Shipment shipment, DateOnly referenceDate)
    {
        var createdDate = DateOnly.FromDateTime(shipment.CreatedAt.UtcDateTime);
        return businessCalendar.CountBusinessDaysAfterStart(createdDate, referenceDate) > GetSlaDays(shipment.ServiceType);
    }

    private bool IsDeliveredOnTime(Shipment shipment)
    {
        if (shipment.DeliveredAt is null)
        {
            return false;
        }

        var createdDate = DateOnly.FromDateTime(shipment.CreatedAt.UtcDateTime);
        var deliveredDate = DateOnly.FromDateTime(shipment.DeliveredAt.Value.UtcDateTime);
        return businessCalendar.CountBusinessDaysAfterStart(createdDate, deliveredDate) <= GetSlaDays(shipment.ServiceType);
    }

    private static int GetSlaDays(ServiceType serviceType)
    {
        return serviceType switch
        {
            ServiceType.Standard => 5,
            ServiceType.Express => 2,
            ServiceType.SameDay => 0,
            _ => throw new AppException(StatusCodes.Status400BadRequest, "Tipo de servicio invalido.")
        };
    }

    [GeneratedRegex("^[36][0-9]{9}$")]
    private static partial Regex ColombianPhoneRegex();
}
