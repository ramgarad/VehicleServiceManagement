using System.ComponentModel.DataAnnotations;

namespace VehicleService.Entities
{
    public class ServiceAdvisor
    {
        public int Id { get; set; }
        [Required, StringLength(100)] public string Name { get; set; } = "";
        [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = "";
        [Required, RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Phone must be 10 digits")] public string Phone { get; set; } = "";

    }
}
