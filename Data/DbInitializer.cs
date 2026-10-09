using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VehicleService.Entities;

namespace VehicleServiceApi.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.EnsureCreatedAsync();   // creates the database + all tables
        await CreateStoredProceduresAsync(db);

        foreach (var r in new[] { "Admin", "ServiceAdvisor" })
            if (!await roles.RoleExistsAsync(r))
                await roles.CreateAsync(new IdentityRole(r));

        if (await users.FindByEmailAsync("admin@prime.com") == null)
        {
            var admin = new ApplicationUser { UserName = "admin@prime.com", Email = "admin@prime.com", FullName = "Prime Admin", EmailConfirmed = true };
            await users.CreateAsync(admin, "Admin@123");
            await users.AddToRoleAsync(admin, "Admin");
        }

        if (!await db.WorkItems.AnyAsync())
        {
            db.WorkItems.AddRange(
                new WorkItem { Name = "Engine Oil", UnitPrice = 650 },
                new WorkItem { Name = "Oil Filter", UnitPrice = 250 },
                new WorkItem { Name = "Fuel Filter", UnitPrice = 400 },
                new WorkItem { Name = "Air Filter", UnitPrice = 350 },
                new WorkItem { Name = "Wheel Alignment", UnitPrice = 800 },
                new WorkItem { Name = "Service Charges", UnitPrice = 1200 });
            await db.SaveChangesAsync();
        }

        if (!await db.ServiceAdvisors.AnyAsync())
        {
            var a1 = new ServiceAdvisor { Name = "Suresh Kulkarni", Email = "advisor1@prime.com", Phone = "9000000001" };
            var a2 = new ServiceAdvisor { Name = "Meena Joshi", Email = "advisor2@prime.com", Phone = "9000000002" };
            db.ServiceAdvisors.AddRange(a1, a2);
            await db.SaveChangesAsync();

            foreach (var a in new[] { a1, a2 })
            {
                var u = new ApplicationUser { UserName = a.Email, Email = a.Email, FullName = a.Name, EmailConfirmed = true, ServiceAdvisorId = a.Id };
                await users.CreateAsync(u, "Advisor@123");
                await users.AddToRoleAsync(u, "ServiceAdvisor");
            }
        }

        if (!await db.Customers.AnyAsync())
        {
            var c1 = new Customer { Name = "Rahul Sharma", Phone = "9876543210", Email = "rahul@example.com", Address = "Kothrud, Pune" };
            var c2 = new Customer { Name = "Priya Patil", Phone = "9123456780", Email = "priya@example.com", Address = "Baner, Pune" };
            var c3 = new Customer { Name = "Amit Deshmukh", Phone = "9988776655", Email = "amit@example.com", Address = "Hinjewadi, Pune" };
            db.Customers.AddRange(c1, c2, c3);
            await db.SaveChangesAsync();

            var today = DateTime.Today;
            db.Vehicles.AddRange(
                new Vehicle { RegistrationNumber = "MH12AB1234", Make = "Maruti", Model = "Swift", Year = 2021, CustomerId = c1.Id, NextServiceDueDate = today },
                new Vehicle { RegistrationNumber = "MH14CD5678", Make = "Hyundai", Model = "Creta", Year = 2022, CustomerId = c2.Id, NextServiceDueDate = today.AddDays(-2) },
                new Vehicle { RegistrationNumber = "MH12EF9012", Make = "Tata", Model = "Nexon", Year = 2023, CustomerId = c3.Id, NextServiceDueDate = today.AddDays(30) });
            await db.SaveChangesAsync();
        }
    }

    // Parameterised stored procedures (called from ServiceRecordsController)
    private static async Task CreateStoredProceduresAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE OR ALTER PROCEDURE sp_GetVehiclesDueForService @WeekEnd DATE
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT v.Id AS VehicleId, v.RegistrationNumber, v.Make, v.Model,
                       c.Name AS CustomerName, v.NextServiceDueDate
                FROM Vehicles v
                INNER JOIN Customers c ON c.Id = v.CustomerId
                WHERE CAST(v.NextServiceDueDate AS DATE) <= @WeekEnd
                  AND NOT EXISTS (SELECT 1 FROM ServiceRecords s WHERE s.VehicleId = v.Id AND s.Status = 1)
                ORDER BY v.NextServiceDueDate;
            END
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE OR ALTER PROCEDURE sp_GetServiceRecords @StatusFrom INT, @StatusTo INT, @AdvisorId INT = NULL
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT s.Id, s.VehicleId, v.RegistrationNumber, v.Make, v.Model,
                       c.Name AS CustomerName, a.Name AS AdvisorName,
                       s.ScheduledDate, s.Status, s.TotalAmount
                FROM ServiceRecords s
                INNER JOIN Vehicles v ON v.Id = s.VehicleId
                INNER JOIN Customers c ON c.Id = v.CustomerId
                INNER JOIN ServiceAdvisors a ON a.Id = s.ServiceAdvisorId
                WHERE s.Status BETWEEN @StatusFrom AND @StatusTo
                  AND (@AdvisorId IS NULL OR s.ServiceAdvisorId = @AdvisorId)
                ORDER BY s.ScheduledDate DESC, s.Id DESC;
            END
            """);
    }
}
// "Service Representatives" master data. Creating an advisor also creates a login
// (email as username, default password Advisor@123) with the ServiceAdvisor role.