using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SchoolAdmission.Tests.Support;

public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16").Build();
    private WebApplicationFactory<Program>? _factory;

    public HttpClient Client { get; private set; } = default!;
    public IServiceProvider Services => _factory!.Services;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Program.cs reads configuration from environment variables when the host is built.
        // The pool is capped below Postgres' default max_connections (100), so a burst of
        // requests queues inside the app instead of exhausting the database.
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING",
            _postgres.GetConnectionString() + ";Maximum Pool Size=40;Timeout=60");
        Environment.SetEnvironmentVariable("HOLD_DURATION_SECONDS", "600");

        _factory = new WebApplicationFactory<Program>();
        Client = _factory.CreateClient();   // builds the host, which also applies migrations
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // Helpers that reach into the database

    // Pushes a hold's expiry into the past, as if its time had run out
    public async Task ExpireHoldNowAsync(string holdId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Holds""
               SET ""ExpiresAt"" = clock_timestamp() - interval '5 seconds'
             WHERE ""Id"" = {holdId}");
    }

    public async Task<HoldStatus> GetStoredStatusAsync(string holdId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Holds.AsNoTracking()
            .Where(h => h.Id == holdId)
            .Select(h => h.Status)
            .SingleAsync();
    }

    public static async Task<bool> WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition()) return true;
            await Task.Delay(200);
        }
        return await condition();
    }
}