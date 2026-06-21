# CourierMax API

API REST para gestionar el ciclo de vida de envios de CourierMax: creacion, asignacion a conductor/vehiculo, seguimiento de estados, calculo de tarifas, alertas de SLA y metricas por conductor.

## Tecnologias

- .NET 8
- ASP.NET Core Minimal APIs
- Swagger/OpenAPI con Swashbuckle
- SQL Server LocalDB y Entity Framework Core
- xUnit para pruebas automatizadas

## Arquitectura

La solucion usa una arquitectura por capas simple:

- `Domain`: entidades, enums y objetos de valor del negocio.
- `Services`: reglas de negocio, validaciones, calculo de tarifas, SLA, transiciones de estado y metricas.
- `Infrastructure`: persistencia en SQL Server mediante Entity Framework Core.
- `Api`: contratos HTTP, endpoints y manejo centralizado de errores.

### Justificacion de las decisiones

- **Minimal API:** se eligio porque el proyecto tiene una cantidad manejable de endpoints y permite definirlos de forma clara sin agregar estructura innecesaria.
- **Separacion por capas:** mantiene independientes la comunicacion HTTP, las reglas de negocio y el acceso a datos. Esto facilita entender y modificar cada parte.
- **Interfaces:** permiten cambiar una implementacion sin afectar directamente la logica de negocio. Tambien facilitan las pruebas al poder usar almacenamiento en memoria.
- **Entity Framework Core:** simplifica la conexion y las operaciones con SQL Server, evitando escribir manualmente la mayoria de las consultas.
- **SQL Server LocalDB:** permite ejecutar el proyecto localmente con una base de datos real, sin necesitar configurar un servidor externo.

La base `CourierMaxDb`, sus tablas y los datos iniciales se crean automaticamente al iniciar la API.

## Ejecutar localmente

Requisitos:

- .NET SDK 8 o superior.
- SQL Server LocalDB en `(localdb)\MSSQLLocalDB`.

```powershell
dotnet restore CourierMax.slnx
dotnet build CourierMax.slnx
dotnet test CourierMax.slnx
dotnet run --project src\CourierMax.Api\CourierMax.Api.csproj
```

Swagger queda disponible en:

```text
http://localhost:5088/swagger
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
curl -X POST http://localhost:5088/api/envios \
  -H "Content-Type: application/json" \
  -d '{
    "remitente": {
      "nombre": "Laura Gomez",
      "telefono": "3001234567",
      "direccion": "Calle 1 # 2-3"
    },
    "destinatario": {
      "nombre": "Andres Ruiz",
      "telefono": "6012345678",
      "direccion": "Carrera 4 # 5-6"
    },
    "paquete": {
      "pesoKg": 5,
      "dimensiones": { "largoCm": 30, "anchoCm": 20, "altoCm": 15 },
      "tipo": "Fragil"
    },
    "tipoServicio": "Express",
    "ciudadOrigen": "Bogota",
    "ciudadDestino": "Medellin"
  }'
```

Respuesta esperada: `201 Created`, codigo `CM-XXXXXXXX`, estado `Created` y tarifa calculada.

### Asignar envio

Si `driverId` es `null`, el sistema selecciona el conductor activo cuyo vehiculo tenga menor carga actual y capacidad disponible.

```bash
curl -X POST http://localhost:5088/api/envios/{envioId}/asignar \
  -H "Content-Type: application/json" \
  -d '{ "conductorId": null, "modificadoPor": "dispatcher-1" }'
```

### Cambiar estado

```bash
curl -X PATCH http://localhost:5088/api/envios/{envioId}/estado \
  -H "Content-Type: application/json" \
  -d '{ "nuevoEstado": "EnTransito", "modificadoPor": "driver-1" }'
```

Cancelar requiere motivo de minimo 5 caracteres:

```bash
curl -X PATCH http://localhost:5088/api/envios/{envioId}/estado \
  -H "Content-Type: application/json" \
  -d '{
    "nuevoEstado": "Cancelado",
    "modificadoPor": "dispatcher-1",
    "motivo": "Cliente cancela solicitud"
  }'
```

### Consultar envios atrasados

```bash
curl "http://localhost:5088/api/envios/atrasados?fechaDesde=2026-06-01&fechaHasta=2026-06-30"
```

### Metricas por conductor

```bash
curl http://localhost:5088/api/conductores/1/metricas
```

### Carga de vehiculos

```bash
curl http://localhost:5088/api/vehiculos/carga
```

## Reglas implementadas

- Telefono colombianos de 10 digitos que inician en 3 o 6.
- Peso entre 0.1 kg y 100 kg.
- Dimensiones entre 1 cm y 200 cm por lado.
- Ciudades restringidas a la lista de referencia.
- Codigo de rastreo unico con formato `CM-XXXXXXXX`.
- Flujo de estados: `Created -> Assigned -> InTransit -> Delivered`.
- Cancelacion desde cualquier estado excepto `Delivered`.
- Historial de cambios con estaod anterior/nuevo, fecha, actor y motivo.
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
