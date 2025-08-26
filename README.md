OTP Web API with Cache-Aside Pattern

A secure ASP.NET Core Web API (.NET 9) for generating and verifying One-Time Passwords (OTPs) using the Cache-Aside pattern with Redis, rate limiting, sequential behavior logging, and Scalar for API documentation.

Table of Contents





Overview



Cache-Aside Pattern



Sequential Behavior Logging



Features



Prerequisites



Setup and Installation



Running the API



Testing with Scalar



Rate Limiting



Security Considerations



Troubleshooting



Future Improvements

Overview

The OTP Web API provides endpoints for requesting and verifying OTPs for Iranian phone numbers, using the Cache-Aside pattern with Redis for caching. It includes rate limiting, sequential behavior logging for detecting frequent requests, and Scalar for interactive API documentation.





Framework: ASP.NET Core 9.0 (Preview)



Endpoints:





POST /api/otp/request: Request an OTP for a phone number.



POST /api/otp/verify: Verify an OTP.



Documentation: Scalar (accessible at https://localhost:5001/swagger).

Cache-Aside Pattern

The Cache-Aside pattern optimizes performance by checking Redis before generating new data:





OTP Storage:





Checks Redis for an existing OTP (otp:phone:{phoneNumber}).



Cache Hit: Reuses the cached OTP.



Cache Miss: Generates a new OTP and stores it in Redis with a 5-minute expiration.



Rate Limiting:





Per IP: Checks Redis for request count (rate:ip:{ip}). If not found, initializes to 0 with a 1-minute expiration.



Per Phone: Checks Redis for a lock (rate:phone:{phoneNumber}). If not found, allows the request and sets a lock with a 1-minute expiration.

Sequential Behavior Logging





Logs requests exceeding 2 per minute from the same IP as potential sequential behavior.



Example log: Sequential OTP requests detected from IP: 127.0.0.1, Count: 3, Timestamp: 2025-08-26 12:57:00.



Helps detect suspicious patterns (e.g., brute-force attempts).

Features





Phone Number Validation: Uses regex ^09[0-9]{9}$ for Iranian phone numbers.



Rate Limiting:





Max 5 requests per minute per IP.



Max 1 request per minute per phone number.



Sequential Behavior Logging: Logs frequent requests with timestamps.



Redis with Cache-Aside: Efficient caching with Redis.



Scalar Documentation: Interactive API testing via Scalar.



HTTPS: Enforced via Kestrel and redirection.

Prerequisites





.NET SDK 9.0: Install from Microsoft .NET 9.0.



Docker: For running Redis (docker run -d -p 6379:6379 redis).



Tools: Postman, VS Code with REST Client, or a browser for Scalar.

Setup and Installation





Clone the Repository (if applicable):

git clone <repository-url>
cd OTP.WebAPI



Install Dependencies:

dotnet restore



Run Redis:

docker run -d -p 6379:6379 --name redis redis
redis-cli -h localhost -p 6379 ping

Expected output: PONG.

Running the API





Start the API:

dotnet run





Listens on:





HTTP: http://localhost:5000



HTTPS: https://localhost:5001 (redirects to /swagger).



Console output:

info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
      Now listening on: http://localhost:5000



Access Scalar:





Open https://localhost:5001/swagger or https://localhost:5001/scalar.

Testing with Scalar





Request OTP:





Endpoint: POST /api/otp/request



Request Body:

{
    "PhoneNumber": "09123456789"
}



Response: "òœ OTP »« „Ê›ﬁ?  «—”«· ‘œ (œ— ·«ê À»  ‘œ).".



Check console for OTP (e.g., OTP generated for 09123456789: 123456).



Verify OTP:





Endpoint: POST /api/otp/verify



Request Body:

{
    "PhoneNumber": "09123456789",
    "Code": "123456"
}



Response: " √??œ ‘œ" or "òœ ‰«„⁄ »— «” ".



Test Sequential Behavior:





Send 3+ requests to /api/otp/request within a minute.



Check logs for:





Sequential OTP requests detected from IP: ..., Count: 3, Timestamp: ...



If rate limit exceeded: Rate limit exceeded for IP: ... or Sequential OTP request blocked for phone: ....



Test with REST Client:





Use otp-api.http:

@OTP.WebAPI_HostAddress = https://localhost:5001

POST {{OTP.WebAPI_HostAddress}}/api/otp/request
Content-Type: application/json
Accept: application/json

{
    "PhoneNumber": "09123456789"
}

###

POST {{OTP.WebAPI_HostAddress}}/api/otp/verify
Content-Type: application/json
Accept: application/json

{
    "PhoneNumber": "09123456789",
    "Code": "123456"
}

Rate Limiting





Per IP: Max 5 requests per minute, stored in Redis (rate:ip:{ip}).



Per Phone: Max 1 request per minute, stored in Redis (rate:phone:{phoneNumber}).



Cache-Aside: Checks Redis first, initializes on cache miss.



Error Messages:





" ⁄œ«œ œ—ŒÊ«” ùÂ« «“ «?‰ IP »?‘ «“ Õœ „Ã«“ «” ."



"›ﬁÿ ?ò œ—ŒÊ«”  œ— œﬁ?ﬁÂ „Ã«“ «” ."

Security Considerations





HTTPS: Enforced via UseHttpsRedirection and Kestrel.



Phone Validation: Regex ^09[0-9]{9}$.



OTP Security: 6-digit OTPs, 5-minute expiration, invalidated after use.



Rate Limiting: Prevents flooding and brute-force attacks.



Production Recommendations:





Secure Redis with password and TLS.



Add JWT authentication.



Use Serilog for persistent logging:

dotnet add package Serilog.AspNetCore

Troubleshooting





API Not Found on https://localhost:5001:





Verify API is running: dotnet run.



Check port conflicts: netstat -aon | findstr :5001.



Allow port 5001 in firewall:

netsh advfirewall firewall add rule name="Allow Port 5001" dir=in action=allow protocol=TCP localport=5001



Redis Connection Issues:





Verify Redis: redis-cli -h localhost -p 6379 ping.



Update options.Configuration in Program.cs if needed.



Sequential Behavior Not Logged:





Check logs for Sequential OTP requests detected.



Send 3+ requests within a minute to trigger.



Invalid Phone Number:





Ensure format matches ^09[0-9]{9}$ (e.g., 09123456789).

Future Improvements





Persistent Logging: Integrate Serilog for file-based logs.



Authentication: Add JWT for securing endpoints.



Advanced Rate Limiting: Use sliding window rate limiting.



SMS Integration: Add a real SMS provider for production.



Unit Tests: Add tests for OtpAppService using xUnit.