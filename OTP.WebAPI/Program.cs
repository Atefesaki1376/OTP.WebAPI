using Microsoft.OpenApi.Models;
using OTP.WebAPI.Interfaces;
using OTP.WebAPI.Services;
using Scalar.AspNetCore;

namespace OTP.WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(5000); // http://localhost:5000
                options.ListenLocalhost(5001, listenOptions =>
                {
                    listenOptions.UseHttps(); // https://localhost:5001
                });
            });

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = "localhost:6379";
                options.InstanceName = "OtpApi:";
            });

            // Add services to the container.

            builder.Services.AddScoped<IOtpAppService, OtpAppService>();

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "OTP Web API", Version = "v1", Description = "API for requesting and verifying OTP codes" });
            });

            builder.Logging.AddConsole();
           
            var app = builder.Build(); 

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.MapControllers();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.MapScalarApiReference(options =>
                {
                    options.Title = "OTP Web API - Scalar";
                    options.OpenApiRoutePattern = "/swagger/v1/swagger.json";
                });
            }

            // Redirect root to Swagger (?� Scalar)
            app.MapGet("/", () => Results.Redirect("/swagger"));

            // app.MapGet("/", () => Results.Redirect("/scalar"));


            app.Run();
        }
    }
}
