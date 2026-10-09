using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleService.Entities
{
    public class WorkItem
    {
        public int Id { get; set; }
        [Required, StringLength(100)] public string Name { get; set; } = "";
        [Range(0.01, 1000000, ErrorMessage = "Price must be greater than 0")]
        [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }

    }
}
