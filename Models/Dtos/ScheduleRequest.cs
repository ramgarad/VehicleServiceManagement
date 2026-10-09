using System.ComponentModel.DataAnnotations;

namespace VehicleService.Models.Dtos
{
    public class ScheduleRequest
    {
        [Range(1, int.MaxValue)] public int VehicleId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Select a service advisor")] public int ServiceAdvisorId { get; set; }
        public DateTime ScheduledDate { get; set; }
    }
}
