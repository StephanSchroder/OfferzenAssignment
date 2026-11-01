using Assignment.Api.Dtos;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace Assignment.Api.OData;

/// <summary>
/// The OData model served under /odata. Entity sets expose read-only DTO projections so
/// soft-deleted / internal fields never leak, and clients can use $filter, $orderby, $top,
/// $skip, $count and $select.
/// </summary>
public static class EdmModelBuilder
{
    public const string RoutePrefix = "odata";

    public static IEdmModel Build()
    {
        var builder = new ODataConventionModelBuilder();

        // camelCase property names, matching the REST endpoints' JSON.
        builder.EnableLowerCamelCase();

        builder.EntitySet<CustomerDto>("Customers");
        builder.EntitySet<ProductDto>("Products");
        builder.EntitySet<PurchaseOrderSummaryDto>("PurchaseOrders");

        return builder.GetEdmModel();
    }
}
