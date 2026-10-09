using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleService.Entities;
using VehicleServiceApi.Data;

namespace VehicleServiceApi.Controllers
{

    // "Service Representatives" master data. Creating an advisor also creates a login
    // (email as username, default password Advisor@123) with the ServiceAdvisor role.
    [ApiController]
    [Route("api/advisors")]
   // [Authorize(Roles = "Admin")]
    public class AdvisorsController : ControllerBase
    {
        private const string DefaultPassword = "Advisor@123";
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly ILogger<AdvisorsController> _log;

        public AdvisorsController(AppDbContext db, UserManager<ApplicationUser> users, ILogger<AdvisorsController> log)
        {
            _db = db;
            _users = users;
            _log = log;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? q)
        {
            var query = _db.ServiceAdvisors.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(a => a.Name.Contains(q) || a.Email.Contains(q) || a.Phone.Contains(q));
            return Ok(await query.OrderBy(a => a.Name).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Create(ServiceAdvisor a)
        {
            a.Id = 0;
            if (await _users.FindByEmailAsync(a.Email) != null)
                return Conflict(new { message = "A user with this email already exists" });

            _db.ServiceAdvisors.Add(a);
            await _db.SaveChangesAsync();

            var user = new ApplicationUser { UserName = a.Email, Email = a.Email, FullName = a.Name, EmailConfirmed = true, ServiceAdvisorId = a.Id };
            var result = await _users.CreateAsync(user, DefaultPassword);
            if (!result.Succeeded)
            {
                _db.ServiceAdvisors.Remove(a);
                await _db.SaveChangesAsync();
                return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
            }
            await _users.AddToRoleAsync(user, "ServiceAdvisor");
            _log.LogInformation("Service advisor {Email} created", a.Email);
            return Ok(new { a.Id, message = $"Advisor created. Login password: {DefaultPassword}" });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, ServiceAdvisor a)
        {
            var e = await _db.ServiceAdvisors.FindAsync(id);
            if (e == null) return NotFound();
            e.Name = a.Name; e.Phone = a.Phone;      // email is the login name, so it is not changed here
            var user = await _users.Users.FirstOrDefaultAsync(u => u.ServiceAdvisorId == id);
            if (user != null) { user.FullName = a.Name; await _users.UpdateAsync(user); }
            await _db.SaveChangesAsync();
            return Ok(new { e.Id });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _db.ServiceAdvisors.FindAsync(id);
            if (e == null) return NotFound();
            if (await _db.ServiceRecords.AnyAsync(s => s.ServiceAdvisorId == id))
                return Conflict(new { message = "This advisor has service records and cannot be deleted." });
            var user = await _users.Users.FirstOrDefaultAsync(u => u.ServiceAdvisorId == id);
            if (user != null) await _users.DeleteAsync(user);
            _db.ServiceAdvisors.Remove(e);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}