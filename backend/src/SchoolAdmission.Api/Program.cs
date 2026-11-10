using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Middleware;
using SchoolAdmission.Api.Models;
using SchoolAdmission.Application;
using SchoolAdmission.Application.Common;
using SchoolAdmission.Infrastructure;
using SchoolAdmission.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Port: http://localhost:8080 unless ASPNETCORE_URLS says otherwise
// the bonus second process sets ASPNETCORE_URLS=http://+:8081
if (string.IsNullOrWhiteSpace(builder.Configuration["ASPNETCORE_URLS"]))
    builder.WebHost.UseUrls("http://+:8080");

// Configuration from environment variables
var connectionString = builder.Configuration["DB_CONNECTION_STRING"];
if (string.IsNullOrWhiteSpace(connectionString))
    connectionString = "Host=localhost;Port=5432;Database=admission;Username=admission;Password=admission";

var holdSeconds = AdmissionOptions.DefaultHoldDurationSeconds;
var holdSecondsRaw = builder.Configuration["HOLD_DURATION_SECONDS"];
if (!string.IsNullOrWhiteSpace(holdSecondsRaw)
    && (!int.TryParse(holdSecondsRaw, out holdSeconds) || holdSeconds <= 0))
{
    throw new InvalidOperationException("HOLD_DURATION_SECONDS must be a positive integer.");
}

// Services
builder.Services.AddApplication(new AdmissionOptions { HoldDurationSeconds = holdSeconds });
builder.Services.AddInfrastructure(connectionString);

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Malformed JSON or wrong types: same { error, message } shape as every other error.
        options.InvalidModelStateResponseFactory = _ => new BadRequestObjectResult(
            new ErrorResponse("VALIDATION_ERROR",
                "The request is invalid. Please check the submitted values and try again."));
    });

// No cookies or credentials are used, so any origin is safe and avoids
// localhost vs 127.0.0.1 problems when the frontend is opened.
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Pipeline (CORS first so error responses also carry CORS headers)
app.UseCors();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

app.MapGet("/health", async (AppDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "healthy" })
        : Results.Json(new { status = "unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable));

// Apply migrations on startup, waiting for the database if needed
await MigrateDatabaseAsync(app);

await app.RunAsync();

static async Task MigrateDatabaseAsync(WebApplication app)
{
    const int maxAttempts = 10;

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            app.Logger.LogWarning(ex,
                "Database not ready (attempt {Attempt}/{Max}). Retrying in 3 seconds...",
                attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

// Needed so the integration tests can use WebApplicationFactory<Program>
public partial class Program { }