using Assignment.Api.Data;
using Assignment.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Assignment.Api.OData;

namespace Assignment.Api.Controllers.OData;

/// <summary>
/// GET /odata/Products — supports $filter, $orderby, $top, $skip, $count and $select.
/// Example: /odata/Products?$filter=price lt 100&amp;$orderby=price desc
/// </summary>
public class ProductsODataController(AppDbContext db) : ODataController
{
    [HttpGet("odata/Products")]
    [EnableQuery(PageSize = ODataQueryDefaults.PageSize, MaxTop = ODataQueryDefaults.MaxTop)]
    public IQueryable<ProductDto> Get() =>
        db.Products
            .Where(p => !p.IsDeleted)
            .Select(Mappings.ProductToDto);
}
