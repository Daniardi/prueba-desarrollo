using CourierMax.Api.Domain;
using CourierMax.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CourierMax.Api.Infrastructure;

public sealed class SqlShipmentStore(CourierMaxDbContext database) : IShipmentStore
{
    public IReadOnlyCollection<Shipment> GetAll()
    {
        return database.Shipments
            .AsNoTracking()
            .Include(x => x.History)
            .OrderByDescending(x => x.CreatedAt)
            .AsEnumerable()
            .Select(ToDomain)
            .ToArray();
    }

    public Shipment? GetById(Guid id)
    {
        var entity = database.Shipments
            .AsNoTracking()
            .Include(x => x.History)
            .SingleOrDefault(x => x.Id == id);
        return entity is null ? null : ToDomain(entity);
    }

    public Shipment? GetByTrackingCode(string trackingCode)
    {
        var entity = database.Shipments
            .AsNoTracking()
            .Include(x => x.History)
            .SingleOrDefault(x => x.TrackingCode == trackingCode);
        return entity is null ? null : ToDomain(entity);
    }

    public void Add(Shipment shipment)
    {
        try
        {
            database.Shipments.Add(ToEntity(shipment));
            database.SaveChanges();
        }
        catch (DbUpdateException)
        {
            throw new AppException(StatusCodes.Status409Conflict, "Ya existe un envio con el mismo identificador o codigo.");
        }
    }

    public void Update(Shipment shipment)
    {
        var entity = database.Shipments
            .Include(x => x.History)
            .SingleOrDefault(x => x.Id == shipment.Id)
            ?? throw new AppException(StatusCodes.Status404NotFound, "Envio no encontrado.");

        entity.Status = shipment.Status;
        entity.DriverId = shipment.DriverId;
        entity.VehicleId = shipment.VehicleId;
        entity.LastAssignedDriverId = shipment.LastAssignedDriverId;
        entity.AssignedAt = shipment.AssignedAt;
        entity.DeliveredAt = shipment.DeliveredAt;
        database.StatusChanges.RemoveRange(entity.History);
        entity.History = shipment.History.Select(x => new StatusChangeEntity
        {
            ShipmentId = shipment.Id,
            PreviousStatus = x.PreviousStatus,
            NewStatus = x.NewStatus,
            ChangedAt = x.ChangedAt,
            ChangedBy = x.ChangedBy,
            Reason = x.Reason
        }).ToList();
        database.SaveChanges();
    }

    private static Shipment ToDomain(ShipmentEntity entity)
    {
        var shipment = new Shipment(
            entity.Id,
            entity.TrackingCode,
            new Contact(entity.SenderName, entity.SenderPhone, entity.SenderAddress),
            new Contact(entity.RecipientName, entity.RecipientPhone, entity.RecipientAddress),
            new PackageDetails(entity.WeightKg, new Dimensions(entity.LengthCm, entity.WidthCm, entity.HeightCm), entity.PackageType),
            entity.ServiceType,
            new ShipmentRoute(entity.OriginCity, entity.DestinationCity),
            entity.Price,
            entity.CreatedAt);

        shipment.RestoreState(
            entity.Status,
            entity.DriverId,
            entity.VehicleId,
            entity.LastAssignedDriverId,
            entity.AssignedAt,
            entity.DeliveredAt,
            entity.History.OrderBy(x => x.ChangedAt).Select(x =>
                new StatusChange(x.PreviousStatus, x.NewStatus, x.ChangedAt, x.ChangedBy, x.Reason)));
        return shipment;
    }

    internal static ShipmentEntity ToEntity(Shipment shipment)
    {
        return new ShipmentEntity
        {
            Id = shipment.Id,
            TrackingCode = shipment.TrackingCode,
            SenderName = shipment.Sender.Name,
            SenderPhone = shipment.Sender.Phone,
            SenderAddress = shipment.Sender.Address,
            RecipientName = shipment.Recipient.Name,
            RecipientPhone = shipment.Recipient.Phone,
            RecipientAddress = shipment.Recipient.Address,
            WeightKg = shipment.Package.WeightKg,
            LengthCm = shipment.Package.Dimensions.LengthCm,
            WidthCm = shipment.Package.Dimensions.WidthCm,
            HeightCm = shipment.Package.Dimensions.HeightCm,
            PackageType = shipment.Package.Type,
            ServiceType = shipment.ServiceType,
            OriginCity = shipment.Route.OriginCity,
            DestinationCity = shipment.Route.DestinationCity,
            Price = shipment.Price,
            CreatedAt = shipment.CreatedAt,
            Status = shipment.Status,
            DriverId = shipment.DriverId,
            VehicleId = shipment.VehicleId,
            LastAssignedDriverId = shipment.LastAssignedDriverId,
            AssignedAt = shipment.AssignedAt,
            DeliveredAt = shipment.DeliveredAt,
            History = shipment.History.Select(x => new StatusChangeEntity
            {
                ShipmentId = shipment.Id,
                PreviousStatus = x.PreviousStatus,
                NewStatus = x.NewStatus,
                ChangedAt = x.ChangedAt,
                ChangedBy = x.ChangedBy,
                Reason = x.Reason
            }).ToList()
        };
    }
}
