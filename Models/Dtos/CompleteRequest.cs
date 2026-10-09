using System.ComponentModel.DataAnnotations;

namespace VehicleService.Models.Dtos
{
    public class CompleteRequest
    {
        [Required, MinLength(1, ErrorMessage = "Add at least one item")] public List<BillLine> Items { get; set; } = new();

    }
}
