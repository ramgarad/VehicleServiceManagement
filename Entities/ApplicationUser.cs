using Microsoft.AspNetCore.Identity;

namespace VehicleService.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = "";
        public int? ServiceAdvisorId { get; set; }   // set only for Service Advisor users
    }

}
