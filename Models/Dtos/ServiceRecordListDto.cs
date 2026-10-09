namespace VehicleService.Models.Dtos
{
    public class ServiceRecordListDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string RegistrationNumber { get; set; } = "";
        public string Make { get; set; } = "";
        public string Model { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string AdvisorName { get; set; } = "";
        public DateTime ScheduledDate { get; set; }
        public int Status { get; set; }
        public decimal TotalAmount { get; set; }

    }
}
// add code here ok
