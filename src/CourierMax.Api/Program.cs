using CourierMax.Api.Api;
using CourierMax.Api.Infrastructure;
using CourierMax.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SchemaFilter<SpanishEnumSchemaFilter>());
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.AddSingleton(TimeProvider.System);
var connectionString = builder.Configuration.GetConnectionString("CourierMax")
    ?? throw new InvalidOperationException("No se configuro la conexion CourierMax.");
builder.Services.AddDbContext<CourierMaxDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<IReferenceData, DatabaseReferenceData>();
builder.Services.AddScoped<IShipmentStore, SqlShipmentStore>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddSingleton<IBusinessCalendar, ColombianBusinessCalendar>();
builder.Services.AddScoped<IShipmentService, ShipmentService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<CourierMaxDbContext>();
    database.Database.EnsureCreated();
    DatabaseSeeder.Seed(database);
}

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapShipmentEndpoints();

app.Run();

public partial class Program;
