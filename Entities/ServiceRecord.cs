using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VehicleService.Entities
{
    public class ServiceRecord
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        [JsonIgnore] public Vehicle? Vehicle { get; set; }
        public int ServiceAdvisorId { get; set; }
        [JsonIgnore] public ServiceAdvisor? ServiceAdvisor { get; set; }
        public DateTime ScheduledDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime? DispatchedDate { get; set; }
        public ServiceStatus Status { get; set; } = ServiceStatus.Scheduled;
        [Column(TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }
        public string? PaymentMethod { get; set; }
        [JsonIgnore] public ICollection<BillItem> Items { get; set; } = new List<BillItem>();

    }
}
