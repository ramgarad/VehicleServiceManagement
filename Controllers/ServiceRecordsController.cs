using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using VehicleService;
using VehicleService.Entities;
using VehicleService.Models.Dtos;
using VehicleService.Services;
using VehicleServiceApi.Data;

namespace VehicleServiceApi.Controllers;

[ApiController]
[Route("api/service-records")]
[Authorize]
public class ServiceRecordsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ServiceRecordsController> _log;

    public ServiceRecordsController(AppDbContext db, ILogger<ServiceRecordsController> log)
    {
        _db = db;
        _log = log;
    }

    private int? AdvisorId() =>
        int.TryParse(User.FindFirst("advisorId")?.Value, out var id) ? id : null;

    // Calls stored procedure sp_GetServiceRecords
    private Task<List<ServiceRecordListDto>> GetRecords(int from, int to, int? advisorId) =>
        _db.ServiceRecordList
            .FromSqlRaw("EXEC sp_GetServiceRecords @StatusFrom, @StatusTo, @AdvisorId",
                new SqlParameter("@StatusFrom", SqlDbType.Int) { Value = from },
                new SqlParameter("@StatusTo", SqlDbType.Int) { Value = to },
                new SqlParameter("@AdvisorId", SqlDbType.Int) { Value = (object?)advisorId ?? DBNull.Value })
            .AsNoTracking()
            .ToListAsync();

    // ---------- ADMIN: home screen (3 lists) ----------
    [HttpGet("dashboard"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Dashboard()
    {
        var weekEnd = BillCalculator.WeekEnd(DateTime.Today);
        // Calls stored procedure sp_GetVehiclesDueForService
        var due = await _db.VehiclesDue
            .FromSqlRaw("EXEC sp_GetVehiclesDueForService @WeekEnd",
                new SqlParameter("@WeekEnd", SqlDbType.Date) { Value = weekEnd })
            .AsNoTracking()
            .ToListAsync();
        var under = await GetRecords((int)ServiceStatus.Scheduled, (int)ServiceStatus.Scheduled, null);
        var serviced = await GetRecords((int)ServiceStatus.Completed, (int)ServiceStatus.Dispatched, null);
        return Ok(new { dueThisWeek = due, underServicing = under, serviced });
    }

    // ---------- ADVISOR: home screen ----------
    [HttpGet("my"), Authorize(Roles = "ServiceAdvisor")]
    public async Task<IActionResult> My()
    {
        var advisorId = AdvisorId();
        if (advisorId == null) return Forbid();
        return Ok(await GetRecords((int)ServiceStatus.Scheduled, (int)ServiceStatus.Scheduled, advisorId));
    }

    // ---------- Single record (with bill items) ----------
    [HttpGet("{id:int}"), Authorize(Roles = "Admin,ServiceAdvisor")]
    public async Task<IActionResult> Get(int id)
    {
        var r = await _db.ServiceRecords.AsNoTracking()
            .Include(s => s.Vehicle).ThenInclude(v => v!.Customer)
            .Include(s => s.ServiceAdvisor)
            .Include(s => s.Items).ThenInclude(i => i.WorkItem)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (r == null) return NotFound();
        if (User.IsInRole("ServiceAdvisor") && r.ServiceAdvisorId != AdvisorId()) return Forbid();

        return Ok(new
        {
            r.Id,
            Status = (int)r.Status,
            r.ScheduledDate,
            r.CompletedDate,
            r.PaidDate,
            r.DispatchedDate,
            r.TotalAmount,
            r.PaymentMethod,
            Vehicle = new { r.Vehicle!.Id, r.Vehicle.RegistrationNumber, r.Vehicle.Make, r.Vehicle.Model, r.Vehicle.Year },
            Customer = new { r.Vehicle.Customer!.Name, r.Vehicle.Customer.Phone, r.Vehicle.Customer.Email, r.Vehicle.Customer.Address },
            AdvisorName = r.ServiceAdvisor!.Name,
            Items = r.Items.Select(i => new
            {
                i.Id,
                i.WorkItemId,
                WorkItemName = i.WorkItem!.Name,
                i.Quantity,
                i.UnitPrice,
                i.LineTotal
            })
        });
    }

    // ---------- ADMIN: schedule a due vehicle with an advisor ----------
    [HttpPost("schedule"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Schedule(ScheduleRequest req)
    {
        if (req.ScheduledDate.Date < DateTime.Today)
            return BadRequest(new { message = "Scheduled date cannot be in the past" });

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        if (!await _db.Vehicles.AnyAsync(v => v.Id == req.VehicleId))
            return NotFound(new { message = "Vehicle not found" });
        if (!await _db.ServiceAdvisors.AnyAsync(a => a.Id == req.ServiceAdvisorId))
            return BadRequest(new { message = "Service advisor not found" });
        if (await _db.ServiceRecords.AnyAsync(s => s.VehicleId == req.VehicleId && s.Status == ServiceStatus.Scheduled))
            return Conflict(new { message = "This vehicle is already scheduled for servicing" });

        var rec = new ServiceRecord
        {
            VehicleId = req.VehicleId,
            ServiceAdvisorId = req.ServiceAdvisorId,
            ScheduledDate = req.ScheduledDate.Date,
            Status = ServiceStatus.Scheduled
        };
        _db.ServiceRecords.Add(rec);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        _log.LogInformation("Vehicle {VehicleId} scheduled with advisor {AdvisorId} (record {Id})", req.VehicleId, req.ServiceAdvisorId, rec.Id);
        return Ok(new { rec.Id });
    }

    // ---------- ADVISOR: add bill items + complete ----------
    [HttpPost("{id:int}/complete"), Authorize(Roles = "ServiceAdvisor")]
    public async Task<IActionResult> Complete(int id, CompleteRequest req)
    {
        var advisorId = AdvisorId();

        // Serializable = strictest isolation level, so two requests can't complete the same record twice
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var rec = await _db.ServiceRecords.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == id);
        if (rec == null) return NotFound();
        if (rec.ServiceAdvisorId != advisorId) return Forbid();
        if (rec.Status != ServiceStatus.Scheduled)
            return Conflict(new { message = "This service record is already completed" });

        var ids = req.Items.Select(i => i.WorkItemId).Distinct().ToList();
        var catalog = await _db.WorkItems.Where(w => ids.Contains(w.Id)).ToDictionaryAsync(w => w.Id);

        (List<BillItem> Items, decimal Total) bill;
        try { bill = BillCalculator.Build(req.Items, catalog); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }

        foreach (var item in bill.Items) rec.Items.Add(item);
        rec.TotalAmount = bill.Total;
        rec.Status = ServiceStatus.Completed;
        rec.CompletedDate = DateTime.Now;

        var vehicle = await _db.Vehicles.FindAsync(rec.VehicleId);
        if (vehicle != null)
        {
            vehicle.LastServiceDate = DateTime.Today;
            vehicle.NextServiceDueDate = DateTime.Today.AddMonths(6);
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        _log.LogInformation("Service record {Id} completed. Total {Total}", id, bill.Total);
        return Ok(new { rec.Id, rec.TotalAmount });
    }

    // ---------- ADMIN: process payment ----------
    [HttpPost("{id:int}/pay"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Pay(int id, PayRequest req)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        var rec = await _db.ServiceRecords.FindAsync(id);
        if (rec == null) return NotFound();
        if (rec.Status != ServiceStatus.Completed)
            return Conflict(new { message = "Only completed service records can be paid" });

        rec.Status = ServiceStatus.Paid;
        rec.PaymentMethod = req.PaymentMethod;
        rec.PaidDate = DateTime.Now;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        _log.LogInformation("Service record {Id} paid by {Method}", id, req.PaymentMethod);
        return Ok(new { rec.Id });
    }

    // ---------- ADMIN: dispatch vehicle ----------
    [HttpPost("{id:int}/dispatch"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Dispatch(int id)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        var rec = await _db.ServiceRecords.FindAsync(id);
        if (rec == null) return NotFound();
        if (rec.Status != ServiceStatus.Paid)
            return Conflict(new { message = "Payment must be processed before dispatch" });

        rec.Status = ServiceStatus.Dispatched;
        rec.DispatchedDate = DateTime.Now;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        _log.LogInformation("Service record {Id} dispatched", id);
        return Ok(new { rec.Id });
    }
}
