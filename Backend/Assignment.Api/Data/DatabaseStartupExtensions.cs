using Assignment.Api.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Assignment.Api.Data;

/// <summary>
/// Startup helpers that make sure the SQLite database exists and is migrated to the latest
/// version before the application starts serving requests.
/// </summary>
public static class DatabaseStartupExtensions
{
    public static async Task<WebApplication> InitialiseDatabaseAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseStartupExtensions));
        var dbContext = services.GetRequiredService<AppDbContext>();

        try
        {
            EnsureDatabaseDirectoryExists(dbContext.Database.GetConnectionString(), logger);

            // 1. Ensure the database exists.
            var databaseCreator = dbContext.GetService<IRelationalDatabaseCreator>();
            if (!await databaseCreator.ExistsAsync(cancellationToken))
            {
                logger.LogInformation("Database does not exist yet, creating it.");
                await databaseCreator.CreateAsync(cancellationToken);
            }

            // 2. Apply any pending migrations.
            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pendingMigrations.Count > 0)
            {
                logger.LogInformation(
                    "Applying {Count} pending migration(s): {Migrations}",
                    pendingMigrations.Count,
                    string.Join(", ", pendingMigrations));
            }

            await dbContext.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database is up to date.");

            // 3. Optional sample data (enabled in appsettings.Development.json).
            if (app.Configuration.GetValue<bool>("Database:SeedSampleData"))
            {
                await SeedSampleDataAsync(dbContext, logger, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "Database initialisation failed.");
            throw;
        }

        return app;
    }

    /// <summary>
    /// SQLite creates the database file itself, but not the folder it lives in
    /// (e.g. for "Data Source=App_Data/assignment.db").
    /// </summary>
    private static void EnsureDatabaseDirectoryExists(string? connectionString, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        }

        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || dataSource == ":memory:")
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            logger.LogInformation("Creating database directory {Directory}", directory);
            Directory.CreateDirectory(directory);
        }
    }

    private static async Task SeedSampleDataAsync(AppDbContext dbContext, ILogger logger, CancellationToken cancellationToken)
    {
        if (await dbContext.Customers.AnyAsync(cancellationToken) || await dbContext.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        logger.LogInformation("Seeding sample data.");

        var customers = new[]
        {
            new Customer { Name = "Acme Corporation", Email = "purchasing@acme.example" },
            new Customer { Name = "Globex B.V.", Email = "orders@globex.example" },
            new Customer { Name = "Initech", Email = "procurement@initech.example" },
        };

        var products = new[]
        {
            new Product { Name = "Standing desk", Description = "Electric height-adjustable desk", Price = 449.00m },
            new Product { Name = "Office chair", Description = "Ergonomic mesh chair", Price = 289.50m },
            new Product { Name = "Monitor 27\"", Description = "27 inch QHD monitor", Price = 239.99m },
            new Product { Name = "USB-C dock", Description = "Dual display docking station", Price = 129.00m },
            new Product { Name = "Keyboard", Description = "Mechanical keyboard", Price = 89.95m },
        };

        dbContext.Customers.AddRange(customers);
        dbContext.Products.AddRange(products);
        await dbContext.SaveChangesAsync(cancellationToken);

        var orders = new[]
        {
            CreateOrder(customers[0], "ACME-2026-0001", PurchaseOrderStatus.Confirmed, (products[0], 4u), (products[1], 4u)),
            CreateOrder(customers[1], "GLX-77812", PurchaseOrderStatus.Pending, (products[2], 10u), (products[3], 10u)),
            CreateOrder(customers[2], "INI-TPS-42", PurchaseOrderStatus.Fulfilled, (products[4], 25u)),
        };

        dbContext.PurchaseOrders.AddRange(orders);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static PurchaseOrder CreateOrder(
        Customer customer,
        string clientReference,
        PurchaseOrderStatus status,
        params (Product Product, uint Quantity)[] lines)
    {
        var order = new PurchaseOrder
        {
            Customer = customer,
            ClientReference = clientReference,
            Status = status,
            Timestamp = DateTimeOffset.UtcNow,
        };

        foreach (var (product, quantity) in lines)
        {
            order.Items.Add(new PurchaseOrderItem { Product = product, Quantity = quantity, UnitPrice = product.Price });
        }

        order.RecalculateTotal();
        return order;
    }
}
