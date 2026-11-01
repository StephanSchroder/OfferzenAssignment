using Assignment.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Assignment.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite has no native DateTimeOffset type, so EF can't sort or compare it in SQL.
        // Storing it as a sortable 64-bit integer keeps OData $filter / $orderby on dates
        // working. All values are UTC, so the encoded values sort chronologically.
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToBinaryConverter>();

        // SQLite has no decimal type either: money is stored as exact integer cents so it can be
        // sorted and compared in SQL, see MoneyToCentsConverter.
        configurationBuilder.Properties<decimal>()
            .HaveConversion<MoneyToCentsConverter>();

        configurationBuilder.Properties<PurchaseOrderStatus>()
            .HaveConversion<string>()
            .HaveMaxLength(20);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Email).IsRequired().HasMaxLength(320);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.Property(p => p.Price).IsRequired();
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.ToTable("PurchaseOrders");
            entity.HasKey(po => po.Id);

            entity.Property(po => po.ClientReference).IsRequired().HasMaxLength(100);
            entity.HasIndex(po => po.ClientReference).IsUnique();

            entity.Property(po => po.Timestamp).IsRequired();
            entity.Property(po => po.Status).IsRequired();

            // 1 customer -> many purchase orders.
            entity.HasOne(po => po.Customer)
                .WithMany(c => c.PurchaseOrders)
                .HasForeignKey(po => po.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.ToTable("PurchaseOrderItems");
            entity.HasKey(i => i.Id);

            entity.HasOne(i => i.PurchaseOrder)
                .WithMany(po => po.Items)
                .HasForeignKey(i => i.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(i => i.Quantity).IsRequired();
            entity.Property(i => i.UnitPrice).IsRequired();
        });
    }
}
