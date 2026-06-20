using System.Collections.Concurrent;
using CourierMax.Api.Domain;
using CourierMax.Api.Services;

namespace CourierMax.Api.Infrastructure;

public interface IShipmentStore
{
    IReadOnlyCollection<Shipment> GetAll();
    Shipment? GetById(Guid id);
    Shipment? GetByTrackingCode(string trackingCode);
    void Add(Shipment shipment);
    void Update(Shipment shipment);
}

public sealed class InMemoryShipmentStore : IShipmentStore
{
    private readonly ConcurrentDictionary<Guid, Shipment> _shipments = new();

    public IReadOnlyCollection<Shipment> GetAll()
    {
        return _shipments.Values.ToArray();
    }

    public Shipment? GetById(Guid id)
    {
        return _shipments.GetValueOrDefault(id);
    }

    public Shipment? GetByTrackingCode(string trackingCode)
    {
        return _shipments.Values.FirstOrDefault(x =>
            string.Equals(x.TrackingCode, trackingCode, StringComparison.OrdinalIgnoreCase));
    }

    public void Add(Shipment shipment)
    {
        if (!_shipments.TryAdd(shipment.Id, shipment))
        {
            throw new AppException(StatusCodes.Status409Conflict, "Ya existe un envio con el mismo identificador.");
        }
    }

    public void Update(Shipment shipment)
    {
        _shipments[shipment.Id] = shipment;
    }
}
