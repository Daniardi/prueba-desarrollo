using CourierMax.Api.Domain;

namespace CourierMax.Api.Services;

public interface IPricingService
{
    decimal Calculate(PackageDetails package, ServiceType serviceType, ShipmentRoute route);
}

public sealed class PricingService(IReferenceData referenceData) : IPricingService
{
    public decimal Calculate(PackageDetails package, ServiceType serviceType, ShipmentRoute route)
    {
        var subtotal = GetBasePrice(serviceType)
            + GetExtraWeightPrice(package.WeightKg)
            + referenceData.GetDistanceRate(route.OriginCity, route.DestinationCity).Price;

        return decimal.Round(subtotal * GetPackageMultiplier(package.Type), 2);
    }

    private static decimal GetBasePrice(ServiceType serviceType)
    {
        return serviceType switch
        {
            ServiceType.Standard => 8_000m,
            ServiceType.Express => 15_000m,
            ServiceType.SameDay => 25_000m,
            _ => throw new AppException(StatusCodes.Status400BadRequest, "Tipo de servicio invalido.")
        };
    }

    private static decimal GetExtraWeightPrice(decimal weightKg)
    {
        return Math.Max(0m, weightKg - 2m) * 1_500m;
    }

    private static decimal GetPackageMultiplier(PackageType packageType)
    {
        return packageType switch
        {
            PackageType.Fragile => 1.30m,
            PackageType.Perishable => 1.25m,
            PackageType.Document or PackageType.Package => 1m,
            _ => throw new AppException(StatusCodes.Status400BadRequest, "Tipo de paquete invalido.")
        };
    }
}
