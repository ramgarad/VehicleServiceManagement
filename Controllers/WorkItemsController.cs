using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleService.Entities;
using VehicleServiceApi.Data;

namespace VehicleServiceApi.Controllers;

[ApiController]
[Route("api/work-items")]
[Authorize]
public class WorkItemsController : ControllerBase
{
    private readonly AppDbContext _db;

    public WorkItemsController(AppDbContext db) => _db = db;

    // Both Admin and Service Advisor can read (advisor needs the list to build the bill)
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? q)
    {
        var query = _db.WorkItems.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(w => w.Name.Contains(q));
        return Ok(await query.OrderBy(w => w.Name).ToListAsync());
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(WorkItem w)
    {
        w.Id = 0;
        _db.WorkItems.Add(w);
        await _db.SaveChangesAsync();
        return Ok(new { w.Id });
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, WorkItem w)
    {
        var e = await _db.WorkItems.FindAsync(id);
        if (e == null) return NotFound();
        e.Name = w.Name; e.UnitPrice = w.UnitPrice;
        await _db.SaveChangesAsync();
        return Ok(new { e.Id });
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.WorkItems.FindAsync(id);
        if (e == null) return NotFound();
        if (await _db.BillItems.AnyAsync(b => b.WorkItemId == id))
            return Conflict(new { message = "This item is used in service bills and cannot be deleted." });
        _db.WorkItems.Remove(e);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
