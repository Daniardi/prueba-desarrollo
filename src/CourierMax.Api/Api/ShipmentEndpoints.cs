using CourierMax.Api.Domain;
using CourierMax.Api.Services;

namespace CourierMax.Api.Api;

public static class ShipmentEndpoints
{
    public static RouteGroupBuilder MapShipmentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api").WithTags("Envios CourierMax");

        group.MapGet("/shipments", (IShipmentService service) =>
            Results.Ok(service.GetAll().Select(ToResponse)))
            .Produces<IEnumerable<ShipmentResponse>>();

        group.MapGet("/shipments/{id:guid}", (Guid id, IShipmentService service) =>
            Results.Ok(ToResponse(service.Get(id))))
            .Produces<ShipmentResponse>();

        group.MapPost("/shipments", (CreateShipmentRequest request, IShipmentService service) =>
        {
            var shipment = service.Create(request);
            return Results.Created($"/api/shipments/{shipment.Id}", ToResponse(shipment));
        }).Produces<ShipmentResponse>(StatusCodes.Status201Created);

        group.MapPost("/shipments/{id:guid}/assign", (Guid id, AssignShipmentRequest request, IShipmentService service) =>
            Results.Ok(ToResponse(service.Assign(id, request))))
            .Produces<ShipmentResponse>();

        group.MapPatch("/shipments/{id:guid}/status", (Guid id, ChangeStatusRequest request, IShipmentService service) =>
            Results.Ok(ToResponse(service.ChangeStatus(id, request))))
            .Produces<ShipmentResponse>();

        group.MapGet("/shipments/late", (DateOnly fechaDesde, DateOnly fechaHasta, IShipmentService service) =>
            Results.Ok(service.GetLateShipments(fechaDesde, fechaHasta).Select(ToResponse)))
            .Produces<IEnumerable<ShipmentResponse>>();

        group.MapGet("/drivers/{conductorId:int}/metrics", (int conductorId, IShipmentService service) =>
            Results.Ok(service.GetDriverMetrics(conductorId)))
            .Produces<DriverMetricsResponse>();

        group.MapGet("/vehicles/load", (IShipmentService service) =>
            Results.Ok(service.GetVehicleLoads()))
            .Produces<IEnumerable<VehicleLoadResponse>>();

        group.MapGet("/reference-data", (IReferenceData referenceData) => Results.Ok(new ReferenceDataResponse(
            referenceData.Cities,
            referenceData.Drivers.Select(x => new DriverResponse(x.Id, x.Name, x.VehicleId, x.IsActive)).ToArray(),
            referenceData.Vehicles.Select(x => new VehicleResponse(x.Id, x.Plate, x.MaxWeightKg, x.MaxVolumeM3)).ToArray())))
            .Produces<ReferenceDataResponse>();

        return group;
    }

    private static ShipmentResponse ToResponse(Shipment shipment)
    {
        return new ShipmentResponse(
            shipment.Id,
            shipment.TrackingCode,
            shipment.Status,
            shipment.ServiceType,
            shipment.Package.Type,
            shipment.Package.WeightKg,
            decimal.Round(shipment.Package.Dimensions.VolumeM3, 6),
            shipment.Route.OriginCity,
            shipment.Route.DestinationCity,
            shipment.Price,
            shipment.CreatedAt,
            shipment.DriverId,
            shipment.VehicleId,
            shipment.History);
    }
}
