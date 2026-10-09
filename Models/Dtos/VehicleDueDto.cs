namespace VehicleService.Models.Dtos
{
    public class VehicleDueDto
    
        {
    public int VehicleId { get; set; }
        public string RegistrationNumber { get; set; } = "";
        public string Make { get; set; } = "";
        public string Model { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public DateTime NextServiceDueDate { get; set; }
    }
}

