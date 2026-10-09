using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VehicleService.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }
        [Required, StringLength(20)] public string RegistrationNumber { get; set; } = "";
        [Required, StringLength(50)] public string Make { get; set; } = "";
        [Required, StringLength(50)] public string Model { get; set; } = "";
        [Range(1980, 2100, ErrorMessage = "Year must be between 1980 and 2100")] public int Year { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Please select a customer")] public int CustomerId { get; set; }
        [JsonIgnore] public Customer? Customer { get; set; }
        public DateTime? LastServiceDate { get; set; }
        public DateTime NextServiceDueDate { get; set; }

    }
}
