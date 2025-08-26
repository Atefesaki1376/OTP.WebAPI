using Microsoft.AspNetCore.Mvc;
using OTP.WebAPI.Interfaces;
using OTP.WebAPI.Models;

namespace OTP.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OtpController : ControllerBase
{
    private readonly IOtpAppService _otpService;

    public OtpController(IOtpAppService otpService)
    {
        _otpService = otpService;
    }

    [HttpPost("request")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<IActionResult> RequestOtp([FromBody] OtpRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _otpService.RequestOtpAsync(request.PhoneNumber, ip);
        if (result.Contains("خطا") || result.Contains("نامعتبر") || result.Contains("بیش از حد"))
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("verify")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public async Task<IActionResult> VerifyOtp([FromBody] OtpVerify verify)
    {
        var isValid = await _otpService.VerifyOtpAsync(verify.PhoneNumber, verify.Code);
        return Ok(isValid ? "تأیید شد" : "کد نامعتبر است");
    }

}
