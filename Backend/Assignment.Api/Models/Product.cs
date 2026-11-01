namespace Assignment.Api.Models;

/// <summary>
/// A product that can be ordered. Products are soft deleted because purchase order items keep
/// referencing them (and the price they had at the time of ordering).
/// </summary>
public class Product
{
    private decimal _price;

    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Unit price. Always stored as a positive value.
    /// </summary>
    public decimal Price
    {
        get => _price;
        set => _price = Math.Abs(value);
    }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public void SoftDelete()
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
    }
}
