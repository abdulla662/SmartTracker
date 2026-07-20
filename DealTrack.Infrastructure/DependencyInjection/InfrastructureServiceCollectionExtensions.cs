using DealTrack.Application.Common;
using DealTrack.Infrastructure.Authorization;
using DealTrack.Application.Contracts;
using DealTrack.Application.Interfaces;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Infrastructure.Persistence;
using DealTrack.Infrastructure.Repositories;
using DealTrack.Infrastructure.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace DealTrack.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
     options.UseMySql(
         configuration.GetConnectionString("DefaultConnection"),
         new MySqlServerVersion(new Version(8, 0, 0))));

            services.AddScoped<RequestFilterContext>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IExcelExportService, ExcelExportService>();
            services.AddScoped<IExcelImportService, ExcelImportService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddHttpClient<ISubscriptionService, SubscriptionService>();
            services.AddIdentity<ApplicationUser, IdentityRole>()
          .AddEntityFrameworkStores<AppDbContext>()
          .AddDefaultTokenProviders();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = configuration["JwtSettings:Issuer"],
                        ValidAudience = configuration["JwtSettings:Audience"],

                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(
                                    configuration["JwtSettings:Key"]!))
                    };

                // SignalR passes the JWT via ?access_token= in WebSocket/SSE URLs
                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            context.Token = accessToken;
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization(options =>
            {
                // Policies read the live plan from DB via a requirement handler,
                // so a downgraded tenant cannot use a stale JWT to retain access.
                options.AddPolicy("ProOrEnterprise",    policy => policy.AddRequirements(new PlanRequirement("Pro", "Enterprise")));
                options.AddPolicy("AdvancedOrHigher",   policy => policy.AddRequirements(new PlanRequirement("Advanced", "Pro", "Enterprise")));
                options.AddPolicy("EnterpriseOnly",     policy => policy.AddRequirements(new PlanRequirement("Enterprise")));
            });

            services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PlanRequirementHandler>();

            services.AddMassTransit(x =>
            {
                // Register request client with 2-minute timeout — Claude Vision can take 15-30s
                x.AddRequestClient<OcrRequested>(new Uri("queue:ocr-requested"), RequestTimeout.After(m: 2));

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
                    {
                        h.Username(configuration["RabbitMQ:Username"] ?? "guest");
                        h.Password(configuration["RabbitMQ:Password"] ?? "guest");
                    });
                });
            });

            services.AddScoped<IOcrService, OcrService>();
            services.AddScoped<ITenantService, TenantService>();

            return services;
        }
    }
}