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
                _logger.LogInformation($"Processing OTP request for phone: {phoneNumber}, IP: {ipAddress}");

                // Validate phone number
                if (!_phoneRegex.IsMatch(phoneNumber))
                {
                    _logger.LogWarning($"Invalid phone number format: {phoneNumber}");
                    return "شماره تلفن نامعتبر است.";
                }

                // Rate limit per IP: max 5 requests per minute (Cache-Aside)
                var ipKey = $"rate:ip:{ipAddress}";
                _logger.LogDebug($"Checking IP rate limit cache for key: {ipKey}");
                var ipCountBytes = await _cache.GetAsync(ipKey);
                int ipCount = ipCountBytes != null ? BitConverter.ToInt32(ipCountBytes, 0) : 0;

                if (ipCountBytes == null)
                {
                    _logger.LogDebug($"Cache miss for IP rate limit key: {ipKey}, initializing count to 0");
                    ipCount = 0;
                    await _cache.SetAsync(ipKey, BitConverter.GetBytes(ipCount), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                    });
                }
                else
                {
                    _logger.LogDebug($"Cache hit for IP rate limit key: {ipKey}, count: {ipCount}");
                }

                // Log sequential behavior if more than 2 requests
                if (ipCount > 2)
                {
                    _logger.LogWarning($"Sequential OTP requests detected from IP: {ipAddress}, Count: {ipCount + 1}, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                }

                // Block if rate limit exceeded
                if (ipCount >= 5)
                {
                    _logger.LogWarning($"Rate limit exceeded for IP: {ipAddress}, Count: {ipCount}, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                    return "تعداد درخواست‌ها از این IP بیش از حد مجاز است.";
                }

                // Rate limit per phone: 1 request per minute (Cache-Aside)
                var phoneRateKey = $"rate:phone:{phoneNumber}";
                _logger.LogDebug($"Checking phone rate limit cache for key: {phoneRateKey}");
                var phoneLock = await _cache.GetStringAsync(phoneRateKey);
                if (phoneLock != null)
                {
                    _logger.LogWarning($"Sequential OTP request blocked for phone: {phoneNumber}, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                    return "فقط یک درخواست در دقیقه مجاز است.";
                }

                // Check for existing OTP (Cache-Aside)
                var otpKey = $"otp:phone:{phoneNumber}";
                _logger.LogDebug($"Checking OTP cache for key: {otpKey}");
                var cachedOtp = await _cache.GetStringAsync(otpKey);

                string otp;
                if (cachedOtp != null)
                {
                    _logger.LogDebug($"Cache hit for OTP key: {otpKey}, using cached OTP");
                    otp = cachedOtp;
                }
                else
                {
                    _logger.LogDebug($"Cache miss for OTP key: {otpKey}, generating new OTP");
                    otp = new Random().Next(100000, 999999).ToString();
                    await _cache.SetStringAsync(otpKey, otp, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });
                    _logger.LogDebug($"Stored new OTP in Redis with key: {otpKey}, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                }

                // Increment IP count and update cache
                ipCount++;
                await _cache.SetAsync(ipKey, BitConverter.GetBytes(ipCount), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                });
                _logger.LogDebug($"Updated IP rate limit count: {ipCount} for key: {ipKey}, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");

                // Set phone rate limit for 1 min
                await _cache.SetStringAsync(phoneRateKey, "locked", new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                });
                _logger.LogDebug($"Set phone rate limit for key: {phoneRateKey}, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");

                // Log OTP
                _logger.LogInformation($"OTP generated for {phoneNumber}: {otp}, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");

                return "کد OTP با موفقیت ارسال شد (در لاگ ثبت شد).";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating OTP for phone: {phoneNumber}, IP: {ipAddress}. Details: {ex.Message}, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                return "خطایی رخ داد. لطفاً دوباره تلاش کنید.";
            }
        }

        public async Task<bool> VerifyOtpAsync(string phoneNumber, string code)
        {
            try
            {
                _logger.LogInformation($"Verifying OTP for phone: {phoneNumber}, Code: {code}, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                var otpKey = $"otp:phone:{phoneNumber}";
                _logger.LogDebug($"Checking OTP cache for key: {otpKey}");
                var storedOtp = await _cache.GetStringAsync(otpKey);

                if (storedOtp == null)
                {
                    _logger.LogWarning($"Cache miss for OTP key: {otpKey}, OTP not found, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                    return false;
                }

                _logger.LogDebug($"Cache hit for OTP key: {otpKey}, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                if (storedOtp == code)
                {
                    await _cache.RemoveAsync(otpKey); // Invalidate OTP after use
                    _logger.LogInformation($"OTP verified for {phoneNumber}, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                    return true;
                }

                _logger.LogWarning($"Invalid OTP attempt for {phoneNumber}: {code}, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying OTP for phone: {phoneNumber}, Code: {code}. Details: {ex.Message}, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                return false;
            }
        }
    }
}
