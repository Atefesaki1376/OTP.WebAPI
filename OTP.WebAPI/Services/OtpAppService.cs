using Microsoft.Extensions.Caching.Distributed;
using OTP.WebAPI.Interfaces;
using System.Text.RegularExpressions;

namespace OTP.WebAPI.Services
{
    public class OtpAppService : IOtpAppService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger _logger;
        private readonly Regex _phoneRegex = new Regex(@"^09[0-9]{9}$");

        public OtpAppService(IDistributedCache cache, ILogger<OtpAppService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<string> RequestOtpAsync(string phoneNumber, string ipAddress)
        {
            try
            {
                // Validate phone number
                if (!_phoneRegex.IsMatch(phoneNumber))
                {
                    _logger.LogWarning($"Invalid phone number: {phoneNumber}");
                    return "شماره تلفن نامعتبر است.";
                }

                // Rate limit per IP: max 5 requests per minute
                var ipKey = $"rate:ip:{ipAddress}";
                var ipCountBytes = await _cache.GetAsync(ipKey);
                int ipCount = ipCountBytes != null ? BitConverter.ToInt32(ipCountBytes, 0) : 0;

                // Log sequential behavior if requests are frequent
                if (ipCount > 2)
                {
                    _logger.LogWarning($"Sequential OTP requests detected from IP: {ipAddress}, Count: {ipCount + 1}");
                }

                if (ipCount >= 5)
                {
                    _logger.LogWarning($"Rate limit exceeded for IP: {ipAddress}, Count: {ipCount}");
                    return "تعداد درخواست‌ها از این IP بیش از حد مجاز است.";
                }

                // Rate limit per phone: 1 request per minute
                var phoneRateKey = $"rate:phone:{phoneNumber}";
                if (await _cache.GetAsync(phoneRateKey) != null)
                {
                    _logger.LogWarning($"Sequential OTP request blocked for phone: {phoneNumber}");
                    return "فقط یک درخواست در دقیقه مجاز است.";
                }

                // Generate OTP
                var otp = new Random().Next(100000, 999999).ToString();

                // Store OTP in Redis with 5 min expiration
                var otpKey = $"otp:phone:{phoneNumber}";
                await _cache.SetStringAsync(otpKey, otp, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

                // Increment IP count and set 1 min expiration
                ipCount++;
                await _cache.SetAsync(ipKey, BitConverter.GetBytes(ipCount), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                });

                // Set phone rate limit for 1 min
                await _cache.SetStringAsync(phoneRateKey, "locked", new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                });

                // Log OTP
                _logger.LogInformation($"OTP generated for {phoneNumber}: {otp}");

                return "کد OTP با موفقیت ارسال شد (در لاگ ثبت شد).";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطا در تولید OTP برای شماره {phoneNumber}");
                return "خطایی رخ داد. لطفاً دوباره تلاش کنید.";
            }
        }

        public async Task<bool> VerifyOtpAsync(string phoneNumber, string code)
        {
            try
            {
                var otpKey = $"otp:phone:{phoneNumber}";
                var storedOtp = await _cache.GetStringAsync(otpKey);
                if (storedOtp == code)
                {
                    await _cache.RemoveAsync(otpKey); // Invalidate OTP after use
                    _logger.LogInformation($"OTP verified for {phoneNumber}");
                    return true;
                }
                _logger.LogWarning($"Invalid OTP attempt for {phoneNumber}: {code}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطا در تأیید OTP برای شماره {phoneNumber}");
                return false;
            }
        }

    }
}
