using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Assignment.Api.Data;

/// <summary>
/// Stores <see cref="decimal"/> money values as whole cents (INTEGER) in SQLite.
/// SQLite has no decimal type and EF Core can't ORDER BY decimals stored as TEXT, so storing
/// exact integer cents keeps OData $orderby / $filter on prices and totals running in SQL
/// while the C# model keeps using decimal. Values are rounded to 2 decimal places.
/// </summary>
public sealed class MoneyToCentsConverter() : ValueConverter<decimal, long>(
    value => (long)Math.Round(value * 100m, MidpointRounding.AwayFromZero),
    cents => cents / 100m);
