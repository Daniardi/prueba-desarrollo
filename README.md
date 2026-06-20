# CourierMax API

API REST para gestionar el ciclo de vida de envios de CourierMax: creacion, asignacion a conductor/vehiculo, seguimiento de estados, calculo de tarifas, alertas de SLA y metricas por conductor.

## Tecnologias

- .NET 8
- ASP.NET Core Minimal APIs
- Swagger/OpenAPI con Swashbuckle
- Almacenamiento en memoria
- xUnit para pruebas automatizadas

## Arquitectura

La solucion usa una arquitectura por capas simple:

- `Domain`: entidades, enums y objetos de valor del negocio.
- `Services`: reglas de negocio, validaciones, calculo de tarifas, SLA, transiciones de estado y metricas.
- `Infrastructure`: almacenamiento en memoria.
- `Api`: contratos HTTP, endpoints y manejo centralizado de errores.

Se eligio almacenamiento en memoria porque la prueba evalua principalmente diseno, reglas de negocio y calidad de API. La separacion por interfaces permite reemplazar `InMemoryShipmentStore` por EF Core o Dapper sin cambiar los endpoints ni la logica principal.

## Ejecutar localmente

Requisitos:

- .NET SDK 8 o superior.

```powershell
dotnet restore CourierMax.slnx
dotnet build CourierMax.slnx
dotnet test CourierMax.slnx
dotnet run --project src\CourierMax.Api\CourierMax.Api.csproj
```

Swagger queda disponible en:

```text
http://localhost:5000/swagger
https://localhost:5001/swagger
```

El puerto exacto puede variar segun `launchSettings.json`; la consola de `dotnet run` muestra la URL activa.

## Datos de referencia

Ciudades soportadas:

- Bogota
- Medellin
- Cali
- Barranquilla

Conductores y vehiculos:

- Juan Perez, vehiculo `ABC-123`, 500 kg, 10 m3
- Maria Lopez, vehiculo `DEF-456`, 300 kg, 6 m3
- Carlos Ruiz, vehiculo `GHI-789`, 800 kg, 15 m3

## Endpoints principales

### Crear envio

```bash
curl -X POST http://localhost:5000/api/shipments \
  -H "Content-Type: application/json" \
  -d '{
    "sender": {
      "name": "Laura Gomez",
      "phone": "3001234567",
      "address": "Calle 1 # 2-3"
    },
    "recipient": {
      "name": "Andres Ruiz",
      "phone": "6012345678",
      "address": "Carrera 4 # 5-6"
    },
    "package": {
      "weightKg": 5,
      "dimensions": { "lengthCm": 30, "widthCm": 20, "heightCm": 15 },
      "type": "Fragile"
    },
    "serviceType": "Express",
    "originCity": "Bogota",
    "destinationCity": "Medellin"
  }'
```

Respuesta esperada: `201 Created`, codigo `CM-XXXXXXXX`, estado `Created` y tarifa calculada.

### Asignar envio

Si `driverId` es `null`, el sistema selecciona el conductor activo cuyo vehiculo tenga menor carga actual y capacidad disponible.

```bash
curl -X POST http://localhost:5000/api/shipments/{shipmentId}/assign \
  -H "Content-Type: application/json" \
  -d '{ "driverId": null, "changedBy": "dispatcher-1" }'
```

### Cambiar estado

```bash
curl -X PATCH http://localhost:5000/api/shipments/{shipmentId}/status \
  -H "Content-Type: application/json" \
  -d '{ "newStatus": "InTransit", "changedBy": "driver-1" }'
```

Cancelar requiere motivo de minimo 5 caracteres:

```bash
curl -X PATCH http://localhost:5000/api/shipments/{shipmentId}/status \
  -H "Content-Type: application/json" \
  -d '{
    "newStatus": "Cancelled",
    "changedBy": "dispatcher-1",
    "reason": "Cliente cancela solicitud"
  }'
```

### Consultar envios atrasados

```bash
curl "http://localhost:5000/api/shipments/late?from=2026-06-01&to=2026-06-30"
```

### Metricas por conductor

```bash
curl http://localhost:5000/api/drivers/1/metrics
```

### Carga de vehiculos

```bash
curl http://localhost:5000/api/vehicles/load
```

## Reglas implementadas

- Telefonos colombianos de 10 digitos que inician en 3 o 6.
- Peso entre 0.1 kg y 100 kg.
- Dimensiones entre 1 cm y 200 cm por lado.
- Ciudades restringidas a la lista de referencia.
- Codigo de rastreo unico con formato `CM-XXXXXXXX`.
- Flujo de estados: `Created -> Assigned -> InTransit -> Delivered`.
- Cancelacion desde cualquier estado excepto `Delivered`.
- Historial de cambios con estado anterior/nuevo, fecha, actor y motivo.
- Capacidad por peso y volumen calculada en tiempo real.
- Liberacion de capacidad al cancelar.
- Tarifa base, recargo por peso, distancia y tipo de paquete.
- SLA con dias habiles, excluyendo sabados, domingos y festivos colombianos 2026.
- Reporte de eficiencia por conductor.

## Pruebas

La suite valida:

- Calculo de tarifa con el ejemplo del enunciado.
- Validacion de telefono.
- Rechazo por exceso de capacidad.
- Balanceo automatico al asignar.
- Flujo de estados y motivo obligatorio de cancelacion.
- Deteccion de envios atrasados por dias habiles.
- Metricas de eficiencia por conductor.
