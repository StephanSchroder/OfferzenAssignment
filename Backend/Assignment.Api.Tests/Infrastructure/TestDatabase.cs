using Assignment.Api.Data;
using Assignment.Api.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Api.Tests.Infrastructure;

/// <summary>
/// A fresh SQLite in-memory database per test, so unique indexes, foreign keys and the
/// money / date converters behave exactly like the real database file.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public TestDatabase()
    {
        // The in-memory database lives as long as this connection stays open.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>Each call returns a new context, like a new HTTP request would get.</summary>
    public AppDbContext CreateContext() => new(_options);

    public Customer AddCustomer(string name = "Acme", string email = "acme@example.com")
    {
        using var context = CreateContext();
        var customer = new Customer { Name = name, Email = email };
        context.Customers.Add(customer);
        context.SaveChanges();
        return customer;
    }

    public Product AddProduct(string name = "Desk", decimal price = 100m)
    {
        using var context = CreateContext();
        var product = new Product { Name = name, Price = price };
        context.Products.Add(product);
        context.SaveChanges();
        return product;
    }

    public void Dispose() => _connection.Dispose();
}
