using CourierMax.Api.Domain;
using CourierMax.Api.Services;

namespace CourierMax.Api.Api;

public static class ShipmentEndpoints
{
    public static RouteGroupBuilder MapShipmentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api").WithTags("CourierMax");

        group.MapGet("/shipments", (IShipmentService service) =>
            Results.Ok(service.GetAll().Select(ToResponse)));

        group.MapGet("/shipments/{id:guid}", (Guid id, IShipmentService service) =>
            Results.Ok(ToResponse(service.Get(id))));

        group.MapPost("/shipments", (CreateShipmentRequest request, IShipmentService service) =>
        {
            var shipment = service.Create(request);
            return Results.Created($"/api/shipments/{shipment.Id}", ToResponse(shipment));
        });

        group.MapPost("/shipments/{id:guid}/assign", (Guid id, AssignShipmentRequest request, IShipmentService service) =>
            Results.Ok(ToResponse(service.Assign(id, request))));

        group.MapPatch("/shipments/{id:guid}/status", (Guid id, ChangeStatusRequest request, IShipmentService service) =>
            Results.Ok(ToResponse(service.ChangeStatus(id, request))));

        group.MapGet("/shipments/late", (DateOnly from, DateOnly to, IShipmentService service) =>
            Results.Ok(service.GetLateShipments(from, to).Select(ToResponse)));

        group.MapGet("/drivers/{driverId:int}/metrics", (int driverId, IShipmentService service) =>
            Results.Ok(service.GetDriverMetrics(driverId)));

        group.MapGet("/vehicles/load", (IShipmentService service) =>
            Results.Ok(service.GetVehicleLoads()));

        group.MapGet("/reference-data", (IReferenceData referenceData) => Results.Ok(new
        {
            referenceData.Cities,
            referenceData.Drivers,
            referenceData.Vehicles
        }));

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
