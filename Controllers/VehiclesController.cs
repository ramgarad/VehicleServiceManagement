using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleService.Entities;
using VehicleServiceApi.Data;

namespace VehicleServiceApi.Controllers;

[ApiController]
[Route("api/vehicles")]
[Authorize(Roles = "Admin")]
public class VehiclesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<VehiclesController> _log;

    public VehiclesController(AppDbContext db, ILogger<VehiclesController> log)
    {
        _db = db;
        _log = log;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? q)
    {
        var query = _db.Vehicles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(v => v.RegistrationNumber.Contains(q) || v.Make.Contains(q)
                                  || v.Model.Contains(q) || v.Customer!.Name.Contains(q));
        var list = await query.OrderBy(v => v.RegistrationNumber)
            .Select(v => new
            {
                v.Id,
                v.RegistrationNumber,
                v.Make,
                v.Model,
                v.Year,
                v.CustomerId,
                CustomerName = v.Customer!.Name,
                v.LastServiceDate,
                v.NextServiceDueDate
            }).ToListAsync();
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Vehicle v)
    {
        v.Id = 0;
        v.RegistrationNumber = v.RegistrationNumber.Trim().ToUpper();
        if (!await _db.Customers.AnyAsync(c => c.Id == v.CustomerId))
            return BadRequest(new { message = "Customer not found" });
        if (await _db.Vehicles.AnyAsync(x => x.RegistrationNumber == v.RegistrationNumber))
            return Conflict(new { message = "A vehicle with this registration number already exists" });

        _db.Vehicles.Add(v);
        await _db.SaveChangesAsync();
        _log.LogInformation("Vehicle {Reg} created", v.RegistrationNumber);
        return Ok(new { v.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Vehicle v)
    {
        var e = await _db.Vehicles.FindAsync(id);
        if (e == null) return NotFound();
        var reg = v.RegistrationNumber.Trim().ToUpper();
        if (await _db.Vehicles.AnyAsync(x => x.RegistrationNumber == reg && x.Id != id))
            return Conflict(new { message = "A vehicle with this registration number already exists" });
        if (!await _db.Customers.AnyAsync(c => c.Id == v.CustomerId))
            return BadRequest(new { message = "Customer not found" });

        e.RegistrationNumber = reg; e.Make = v.Make; e.Model = v.Model; e.Year = v.Year;
        e.CustomerId = v.CustomerId; e.NextServiceDueDate = v.NextServiceDueDate;
        await _db.SaveChangesAsync();
        return Ok(new { e.Id });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.Vehicles.FindAsync(id);
        if (e == null) return NotFound();
        if (await _db.ServiceRecords.AnyAsync(s => s.VehicleId == id))
            return Conflict(new { message = "This vehicle has service records and cannot be deleted." });
        _db.Vehicles.Remove(e);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
