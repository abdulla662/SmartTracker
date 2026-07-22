using DealTrack.API.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using DealTrack.API.ExceptionMiddleWare;
using DealTrack.API.Filters;
using DealTrack.API.Hubs;
using DealTrack.Application.DependencyInjection;
using DealTrack.Infrastructure.BackgroundJobs;
using DealTrack.Infrastructure.DependencyInjection;
using Hangfire;
using Hangfire.MySql;
using Hangfire.Storage;
using Serilog;
using System.Threading.RateLimiting;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq(builder.Configuration["Seq:Url"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// Controllers & Swagger
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.AddSignalR();
builder.Services.AddHostedService<DealTrack.API.Services.OcrPythonHostedService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Add Accept-Language header to every endpoint in Swagger UI
    options.OperationFilter<AcceptLanguageHeaderFilter>();
});

builder.Services.AddLocalization();

// Layer registrations
builder.Services.AddApiServices();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddRateLimiter(options =>
{
    // Login: 5 attempts per minute per IP — anti brute-force
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Public lookup endpoints: 10 per minute per IP — prevents enumeration scripts
    options.AddPolicy("public-lookup", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Sensitive auth actions: 3 per 15 minutes per IP — register, OTP, forgot-password
    options.AddPolicy("auth-sensitive", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(15),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.RejectionStatusCode = 429;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // SignalR hubs make many legitimate long-poll requests — exempt them entirely
        if (context.Request.Path.StartsWithSegments("/hubs"))
            return RateLimitPartition.GetNoLimiter("hubs");

        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? context.Connection.RemoteIpAddress?.ToString()
                     ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userId,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseStorage(new MySqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new MySqlStorageOptions())));

builder.Services.AddHangfireServer();
builder.Services.AddScoped<MarkMissedFollowUpsJob>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost",
                "http://localhost:80",
                "https://wilt-asleep-peroxide.ngrok-free.dev")
              .WithHeaders("Authorization", "Content-Type", "Accept-Language", "Accept", "X-Requested-With")
              .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
              .AllowCredentials());
});

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Request localization — reads Accept-Language header automatically
var supportedCultures = new[] { "en", "ar", "fr", "de" };
app.UseRequestLocalization(options =>
{
    options.SetDefaultCulture("en")
           .AddSupportedCultures(supportedCultures)
           .AddSupportedUICultures(supportedCultures);
});

app.UseHttpsRedirection();
app.UseCors("FrontendDev");
app.UseStaticFiles();
app.UseMiddleware<ExceptionMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});
using (var scope = app.Services.CreateScope())
{
    RecurringJob.AddOrUpdate<MarkMissedFollowUpsJob>(
        "mark-missed-followups",
        job => job.ExecuteAsync(),
        Cron.Daily);
}
// Run SQL scripts (Stored Procedures) on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DealTrack.Infrastructure.Persistence.AppDbContext>();
    try { await db.Database.MigrateAsync(); } catch { /* schema already up to date */ }
    await DealTrack.Infrastructure.Persistence.SqlScriptRunner.RunStoredProceduresAsync(db);
}

// Seed SuperAdmin account
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<DealTrack.Domain.Entities.ApplicationUser>>();
    var uow = scope.ServiceProvider.GetRequiredService<DealTrack.Application.Interfaces.IUnitOfWork>();

    const string superAdminEmail = "superadmin@dealtrack.com";
    const string superAdminPassword = "SuperAdmin@2024!";

    if (await userManager.FindByEmailAsync(superAdminEmail) == null)
    {
        // Ensure a system tenant exists for the SuperAdmin user (FK requires a real tenant row)
        var systemTenantName = "__DealTrack_System__";
        var systemTenant = await uow.Read<DealTrack.Domain.Entities.Tenant>()
            .GetSingleAsync(t => t.Name == systemTenantName);

        if (systemTenant == null)
        {
            systemTenant = new DealTrack.Domain.Entities.Tenant(systemTenantName, DealTrack.Domain.Enums.SubscriptionPlan.Free);
            await uow.Write<DealTrack.Domain.Entities.Tenant>().AddAsync(systemTenant);
            await uow.SaveChangesAsync();
        }

        var superAdmin = new DealTrack.Domain.Entities.ApplicationUser
        {
            FullName        = "Super Admin",
            Email           = superAdminEmail,
            UserName        = superAdminEmail,
            Role            = DealTrack.Domain.Enums.UserRole.SuperAdmin,
            TenantId        = systemTenant.Id,
            IsApproved      = true,
            HasBeenWelcomed = true,
        };
        await userManager.CreateAsync(superAdmin, superAdminPassword);
    }
}

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications").RequireCors("FrontendDev");
app.MapHub<ChatHub>("/hubs/chat").RequireCors("FrontendDev");
app.Run();
