using Assignment.Api.Data;
using Assignment.Api.Dtos;
using Assignment.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Api.Controllers;

/// <summary>
/// CRUD for customers. Deleting a customer is a soft delete. For filtering / sorting use
/// the OData endpoint at /odata/Customers.
/// </summary>
[ApiController]
[Route("api/customers")]
[Produces("application/json")]
public class CustomerController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll(CancellationToken cancellationToken)
    {
        var customers = await db.Customers
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(Mappings.CustomerToDto)
            .ToListAsync(cancellationToken);

        return Ok(customers);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var customer = await db.Customers
            .AsNoTracking()
            .Where(c => c.Id == id && !c.IsDeleted)
            .Select(Mappings.CustomerToDto)
            .FirstOrDefaultAsync(cancellationToken);

        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerDto>> Create(CustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = new Customer
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer.ToDto());
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> Update(int id, CustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        customer.Name = request.Name.Trim();
        customer.Email = request.Email.Trim();
        await db.SaveChangesAsync(cancellationToken);

        return Ok(customer.ToDto());
    }

    /// <summary>
    /// Soft deletes the customer. The record (and its purchase order history) is kept.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        customer.SoftDelete();
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
