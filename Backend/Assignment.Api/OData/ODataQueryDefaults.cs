namespace Assignment.Api.OData;

public static class ODataQueryDefaults
{
    /// <summary>Server-side page size when the client doesn't send $top.</summary>
    public const int PageSize = 100;

    /// <summary>Largest $top a client may request.</summary>
    public const int MaxTop = 500;
}
