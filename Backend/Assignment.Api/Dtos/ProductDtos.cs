using System.ComponentModel.DataAnnotations;

namespace Assignment.Api.Dtos;

public class ProductDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }
}

public class ProductRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Unit price. Negative values are stored as their absolute value.
    /// </summary>
    public decimal Price { get; set; }
}
