using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VehicleService.Entities
{
    public class BillItem
    {
        public int Id { get; set; }
        public int ServiceRecordId { get; set; }
        [JsonIgnore] public ServiceRecord? ServiceRecord { get; set; }
        public int WorkItemId { get; set; }
        [JsonIgnore] public WorkItem? WorkItem { get; set; }
        public int Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal LineTotal { get; set; }

    }
}
