using System.ComponentModel.DataAnnotations;

namespace VehicleService.Models.Dtos
{
    public class PayRequest
    {
        [Required, RegularExpression("^(Cash|Card|UPI|Cheque)$", ErrorMessage = "Payment method must be Cash, Card, UPI or Cheque")]
        public string PaymentMethod { get; set; } = "";

    }
}
