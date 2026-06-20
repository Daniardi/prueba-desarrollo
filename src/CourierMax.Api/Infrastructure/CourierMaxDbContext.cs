using CourierMax.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CourierMax.Api.Infrastructure;

public sealed class CourierMaxDbContext(DbContextOptions<CourierMaxDbContext> options) : DbContext(options)
{
    public DbSet<ShipmentEntity> Shipments => Set<ShipmentEntity>();
    public DbSet<StatusChangeEntity> StatusChanges => Set<StatusChangeEntity>();
    public DbSet<VehicleEntity> Vehicles => Set<VehicleEntity>();
    public DbSet<DriverEntity> Drivers => Set<DriverEntity>();
    public DbSet<CityEntity> Cities => Set<CityEntity>();
    public DbSet<DistanceRateEntity> DistanceRates => Set<DistanceRateEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShipmentEntity>(entity =>
        {
            entity.ToTable("Shipments");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.TrackingCode).IsUnique();
            entity.Property(x => x.TrackingCode).HasMaxLength(11);
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.Property(x => x.WeightKg).HasPrecision(10, 2);
            entity.Property(x => x.LengthCm).HasPrecision(10, 2);
            entity.Property(x => x.WidthCm).HasPrecision(10, 2);
            entity.Property(x => x.HeightCm).HasPrecision(10, 2);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.PackageType).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.ServiceType).HasConversion<string>().HasMaxLength(20);
            entity.HasMany(x => x.History).WithOne().HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StatusChangeEntity>(entity =>
        {
            entity.ToTable("ShipmentStatusHistory");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<VehicleEntity>(entity =>
        {
            entity.ToTable("Vehicles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.HasIndex(x => x.Plate).IsUnique();
            entity.Property(x => x.MaxWeightKg).HasPrecision(10, 2);
            entity.Property(x => x.MaxVolumeM3).HasPrecision(10, 3);
        });

        modelBuilder.Entity<DriverEntity>(entity =>
        {
            entity.ToTable("Drivers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.HasOne<VehicleEntity>().WithMany().HasForeignKey(x => x.VehicleId);
        });

        modelBuilder.Entity<CityEntity>(entity =>
        {
            entity.ToTable("Cities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<DistanceRateEntity>(entity =>
        {
            entity.ToTable("DistanceRates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Price).HasPrecision(18, 2);
        });
    }
}

public sealed class ShipmentEntity
{
    public Guid Id { get; set; }
    public string TrackingCode { get; set; } = "";
    public string SenderName { get; set; } = "";
    public string SenderPhone { get; set; } = "";
    public string SenderAddress { get; set; } = "";
    public string RecipientName { get; set; } = "";
    public string RecipientPhone { get; set; } = "";
    public string RecipientAddress { get; set; } = "";
    public decimal WeightKg { get; set; }
    public decimal LengthCm { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }
    public PackageType PackageType { get; set; }
    public ServiceType ServiceType { get; set; }
    public string OriginCity { get; set; } = "";
    public string DestinationCity { get; set; } = "";
    public decimal Price { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ShipmentStatus Status { get; set; }
    public int? DriverId { get; set; }
    public int? VehicleId { get; set; }
    public int? LastAssignedDriverId { get; set; }
    public DateTimeOffset? AssignedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public List<StatusChangeEntity> History { get; set; } = [];
}

public sealed class StatusChangeEntity
{
    public long Id { get; set; }
    public Guid ShipmentId { get; set; }
    public ShipmentStatus PreviousStatus { get; set; }
    public ShipmentStatus NewStatus { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public string ChangedBy { get; set; } = "";
    public string? Reason { get; set; }
}

public sealed class VehicleEntity
{
    public int Id { get; set; }
    public string Plate { get; set; } = "";
    public decimal MaxWeightKg { get; set; }
    public decimal MaxVolumeM3 { get; set; }
}

public sealed class DriverEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int VehicleId { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CityEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class DistanceRateEntity
{
    public int Id { get; set; }
    public string OriginCity { get; set; } = "";
    public string DestinationCity { get; set; } = "";
    public int DistanceKm { get; set; }
    public decimal Price { get; set; }
}
