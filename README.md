OTP Web API
A secure and efficient ASP.NET Core Web API (.NET 9) for generating and verifying One-Time Passwords (OTPs) with rate limiting, sequential behavior logging, and Scalar for API documentation.
Table of Contents

Overview
Features
Prerequisites
Project Structure
Setup and Installation
Running the API
Testing with Scalar
Rate Limiting
Sequential Behavior Logging
Security Considerations
Troubleshooting
Future Improvements

Overview
The OTP Web API provides endpoints for requesting and verifying OTPs for Iranian phone numbers. It uses Redis for rate limiting and caching, logs sequential request behavior for security monitoring, and exposes a user-friendly Scalar interface for API documentation and testing.

Framework: ASP.NET Core 9.0 (Preview)
Endpoints:
POST /api/otp/request: Request an OTP for a phone number.
POST /api/otp/verify: Verify an OTP.


Documentation: Scalar (accessible at https://localhost:5001 or /swagger).

Features

Phone Number Validation: Validates Iranian phone numbers using the regex ^09[0-9]{9}$.
Rate Limiting:
Max 5 requests per minute per IP address.
Max 1 request per minute per phone number.


Sequential Behavior Logging: Logs frequent requests from the same IP for security monitoring.
Redis Integration: Uses Redis for storing OTPs and rate limiting data.
Scalar Documentation: Interactive API documentation with Scalar for easy testing.
HTTPS: Enforces secure communication with HTTPS redirection.

Prerequisites

.NET SDK 9.0: Install from Microsoft .NET 9.0 (Preview).
Redis: Running on localhost:6379 (e.g., via Docker: docker run -d -p 6379:6379 redis).
Tools: Postman, VS Code with REST Client, or a browser for Scalar testing.

Project Structure
OTP.WebAPI/
??? Controllers/
?   ??? OtpController.cs        # API endpoints for OTP request and verification
??? Models/
?   ??? OtpModels.cs           # Data models (OtpRequest, OtpVerify)
??? Services/
?   ??? OtpAppService.cs       # OTP business logic with Redis and logging
??? Interfaces/
?   ??? IOtpAppService.cs      # Service interface
??? Properties/
?   ??? launchSettings.json    # Configures HTTPS on port 5001
??? Program.cs                 # Main application setup with Kestrel and Scalar
??? Api.csproj                 # Project file with dependencies
??? otp-api.http               # Test file for REST Client

Setup and Installation

Clone the Repository (if applicable):git clone <repository-url>
cd OTP.WebAPI


Install Dependencies:dotnet restore


Run Redis:docker run -d -p 6379:6379 redis

Verify Redis is running:redis-cli -h localhost -p 6379 ping

Expected output: PONG.

Running the API

Start the API:
dotnet run


The API listens on:
HTTP: http://localhost:5000
HTTPS: https://localhost:5001 (preferred, redirects to /swagger).


Console output should show:info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
      Now listening on: http://localhost:5000




Access Scalar:

Open https://localhost:5001 in a browser.
You’ll be redirected to /swagger or can manually navigate to /scalar for the Scalar UI


