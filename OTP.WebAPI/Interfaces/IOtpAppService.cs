namespace OTP.WebAPI.Interfaces
{
    public interface IOtpAppService
    {
        Task<string> RequestOtpAsync(string phoneNumber, string ipAddress);
        Task<bool> VerifyOtpAsync(string phoneNumber, string code);
    }
}
