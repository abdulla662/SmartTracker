using DealTrack.API.DependencyInjection;
using DealTrack.API.ExceptionMiddleWare;
using DealTrack.API.Filters;
using DealTrack.Application.DependencyInjection;
using DealTrack.Infrastructure.BackgroundJobs;
using DealTrack.Infrastructure.DependencyInjection;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// Controllers & Swagger
builder.Services.AddControllers();
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

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();
builder.Services.AddScoped<MarkMissedFollowUpsJob>();

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
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire");

using (var scope = app.Services.CreateScope())
{
    RecurringJob.AddOrUpdate<MarkMissedFollowUpsJob>(
        "mark-missed-followups",
        job => job.ExecuteAsync(),
        Cron.Daily);
}
app.MapControllers();
app.Run();
