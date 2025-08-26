using System.ComponentModel.DataAnnotations;

namespace OTP.WebAPI.Models
{
    public class OtpRequest
    {
        [Required]
        public string PhoneNumber { get; set; }
    }

    public class OtpVerify
    {
        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string Code { get; set; }
    }
}
