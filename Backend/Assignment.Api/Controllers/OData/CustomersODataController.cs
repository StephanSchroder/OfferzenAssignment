using Assignment.Api.Data;
using Assignment.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Assignment.Api.OData;

namespace Assignment.Api.Controllers.OData;

/// <summary>
/// GET /odata/Customers — supports $filter, $orderby, $top, $skip, $count and $select.
/// Example: /odata/Customers?$filter=contains(tolower(name),'acme')&amp;$orderby=name&amp;$count=true
/// </summary>
public class CustomersODataController(AppDbContext db) : ODataController
{
    [HttpGet("odata/Customers")]
    [EnableQuery(PageSize = ODataQueryDefaults.PageSize, MaxTop = ODataQueryDefaults.MaxTop)]
    public IQueryable<CustomerDto> Get() =>
        db.Customers
            .Where(c => !c.IsDeleted)
            .Select(Mappings.CustomerToDto);
}
