using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleService.Entities;
using VehicleServiceApi.Data;

namespace VehicleServiceApi.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = "Admin")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<CustomersController> _log;

    public CustomersController(AppDbContext db, ILogger<CustomersController> log)
    {
        _db = db;
        _log = log;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? q)
    {
        var query = _db.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(c => c.Name.Contains(q) || c.Phone.Contains(q) || c.Email.Contains(q));
        return Ok(await query.OrderBy(c => c.Name).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return c == null ? NotFound() : Ok(c);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Customer c)
    {
        c.Id = 0;
        _db.Customers.Add(c);
        await _db.SaveChangesAsync();
        _log.LogInformation("Customer {Id} created", c.Id);
        return CreatedAtAction(nameof(GetById), new { id = c.Id }, c);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Customer c)
    {
        var e = await _db.Customers.FindAsync(id);
        if (e == null) return NotFound();
        e.Name = c.Name; e.Phone = c.Phone; e.Email = c.Email; e.Address = c.Address;
        await _db.SaveChangesAsync();
        return Ok(e);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.Customers.FindAsync(id);
        if (e == null) return NotFound();
        if (await _db.Vehicles.AnyAsync(v => v.CustomerId == id))
            return Conflict(new { message = "This customer has vehicles. Delete the vehicles first." });
        _db.Customers.Remove(e);
        await _db.SaveChangesAsync();
        _log.LogInformation("Customer {Id} deleted", id);
        return NoContent();
    }
}