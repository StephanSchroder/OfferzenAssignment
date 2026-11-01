using Assignment.Api.Data;

namespace Assignment.Api.Tests.Domain;

public class MoneyToCentsConverterTests
{
    private readonly MoneyToCentsConverter _converter = new();

    [Theory]
    [InlineData(239.99, 23999L)]
    [InlineData(0.01, 1L)]
    [InlineData(10, 1000L)]
    [InlineData(9.995, 1000L)] // rounded to whole cents
    [InlineData(9.994, 999L)]
    public void Stores_money_as_whole_cents(double amount, long expectedCents)
    {
        var cents = _converter.ConvertToProvider((decimal)amount);

        Assert.Equal(expectedCents, cents);
    }

    [Fact]
    public void Reads_cents_back_as_the_same_decimal()
    {
        var amount = _converter.ConvertFromProvider(23999L);

        Assert.Equal(239.99m, amount);
    }
}
