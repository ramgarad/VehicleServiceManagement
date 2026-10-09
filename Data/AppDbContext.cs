using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using VehicleService.Entities;
using VehicleService.Models.Dtos;

namespace VehicleServiceApi.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<ServiceAdvisor> ServiceAdvisors => Set<ServiceAdvisor>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<ServiceRecord> ServiceRecords => Set<ServiceRecord>();
    public DbSet<BillItem> BillItems => Set<BillItem>();

    // Keyless types used only to read stored procedure results
    public DbSet<VehicleDueDto> VehiclesDue => Set<VehicleDueDto>();
    public DbSet<ServiceRecordListDto> ServiceRecordList => Set<ServiceRecordListDto>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<VehicleDueDto>().HasNoKey().ToView((string?)null);
        b.Entity<ServiceRecordListDto>().HasNoKey().ToView((string?)null);

        b.Entity<Vehicle>().HasIndex(v => v.RegistrationNumber).IsUnique();

        b.Entity<Vehicle>().HasOne(v => v.Customer).WithMany()
            .HasForeignKey(v => v.CustomerId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<ServiceRecord>().HasOne(s => s.Vehicle).WithMany()
            .HasForeignKey(s => s.VehicleId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ServiceRecord>().HasOne(s => s.ServiceAdvisor).WithMany()
            .HasForeignKey(s => s.ServiceAdvisorId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<BillItem>().HasOne(i => i.ServiceRecord).WithMany(s => s.Items)
            .HasForeignKey(i => i.ServiceRecordId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<BillItem>().HasOne(i => i.WorkItem).WithMany()
            .HasForeignKey(i => i.WorkItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
// "Service Representatives" master data. Creating an advisor also creates a login
// (email as username, default password Advisor@123) with the ServiceAdvisor role.