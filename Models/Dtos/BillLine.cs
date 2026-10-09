using System.ComponentModel.DataAnnotations;

namespace VehicleService.Models.Dtos
{
    public class BillLine
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a work item")] public int WorkItemId { get; set; }
        [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000")] public int Quantity { get; set; }

    }
}
