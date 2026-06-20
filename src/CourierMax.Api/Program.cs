using CourierMax.Api.Api;
using CourierMax.Api.Infrastructure;
using CourierMax.Api.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IReferenceData, ReferenceData>();
builder.Services.AddSingleton<IShipmentStore, InMemoryShipmentStore>();
builder.Services.AddSingleton<IPricingService, PricingService>();
builder.Services.AddSingleton<IBusinessCalendar, ColombianBusinessCalendar>();
builder.Services.AddSingleton<IShipmentService, ShipmentService>();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapShipmentEndpoints();

app.Run();

public partial class Program;
