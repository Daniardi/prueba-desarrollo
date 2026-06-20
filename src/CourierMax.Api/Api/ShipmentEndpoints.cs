using CourierMax.Api.Domain;
using CourierMax.Api.Services;

namespace CourierMax.Api.Api;

public static class ShipmentEndpoints
{
    public static RouteGroupBuilder MapShipmentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api").WithTags("Envios CourierMax");

        group.MapGet("/envios", (IShipmentService service) =>
            Results.Ok(service.GetAll().Select(ToResponse)))
            .WithName("ConsultarEnvios")
            .WithSummary("Consultar todos los envios")
            .WithDescription("Obtiene el listado completo de envios registrados.")
            .Produces<IEnumerable<ShipmentResponse>>();

        group.MapGet("/envios/{id:guid}", (Guid id, IShipmentService service) =>
            Results.Ok(ToResponse(service.Get(id))))
            .WithName("ConsultarEnvioPorId")
            .WithSummary("Consultar un envio por identificador")
            .WithDescription("Obtiene el detalle de un envio y su historial de estados.")
            .Produces<ShipmentResponse>();

        group.MapPost("/envios", (CreateShipmentRequest request, IShipmentService service) =>
        {
            var shipment = service.Create(request);
            return Results.Created($"/api/envios/{shipment.Id}", ToResponse(shipment));
        })
            .WithName("CrearEnvio")
            .WithSummary("Crear un envio")
            .WithDescription("Registra un nuevo envio, calcula su tarifa y genera el codigo de rastreo.")
            .Produces<ShipmentResponse>(StatusCodes.Status201Created);

        group.MapPost("/envios/{id:guid}/asignar", (Guid id, AssignShipmentRequest request, IShipmentService service) =>
            Results.Ok(ToResponse(service.Assign(id, request))))
            .WithName("AsignarEnvio")
            .WithSummary("Asignar un envio")
            .WithDescription("Asigna el envio a un conductor y su vehiculo, validando la capacidad disponible.")
            .Produces<ShipmentResponse>();

        group.MapPatch("/envios/{id:guid}/estado", (Guid id, ChangeStatusRequest request, IShipmentService service) =>
            Results.Ok(ToResponse(service.ChangeStatus(id, request))))
            .WithName("CambiarEstadoEnvio")
            .WithSummary("Cambiar el estado de un envio")
            .WithDescription("Actualiza el estado del envio respetando el flujo permitido y registra el cambio.")
            .Produces<ShipmentResponse>();

        group.MapGet("/envios/atrasados", (DateOnly fechaDesde, DateOnly fechaHasta, IShipmentService service) =>
            Results.Ok(service.GetLateShipments(fechaDesde, fechaHasta).Select(ToResponse)))
            .WithName("ConsultarEnviosAtrasados")
            .WithSummary("Consultar envios atrasados")
            .WithDescription("Obtiene los envios que superaron el SLA dentro del rango de fechas indicado.")
            .Produces<IEnumerable<ShipmentResponse>>();

        group.MapGet("/conductores/{conductorId:int}/metricas", (int conductorId, IShipmentService service) =>
            Results.Ok(service.GetDriverMetrics(conductorId)))
            .WithName("ConsultarMetricasConductor")
            .WithSummary("Consultar metricas de un conductor")
            .WithDescription("Calcula indicadores de asignacion, entrega, cancelacion y cumplimiento del conductor.")
            .Produces<DriverMetricsResponse>();

        group.MapGet("/vehiculos/carga", (IShipmentService service) =>
            Results.Ok(service.GetVehicleLoads()))
            .WithName("ConsultarCargaVehiculos")
            .WithSummary("Consultar la carga de los vehiculos")
            .WithDescription("Obtiene el peso y volumen utilizados y disponibles de cada vehiculo.")
            .Produces<IEnumerable<VehicleLoadResponse>>();

        group.MapGet("/datos-referencia", (IReferenceData referenceData) => Results.Ok(new ReferenceDataResponse(
            referenceData.Cities,
            referenceData.Drivers.Select(x => new DriverResponse(x.Id, x.Name, x.VehicleId, x.IsActive)).ToArray(),
            referenceData.Vehicles.Select(x => new VehicleResponse(x.Id, x.Plate, x.MaxWeightKg, x.MaxVolumeM3)).ToArray())))
            .WithName("ConsultarDatosReferencia")
            .WithSummary("Consultar datos de referencia")
            .WithDescription("Obtiene las ciudades, conductores y vehiculos disponibles en el sistema.")
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
