using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Infrastructure;
using NotificationService.IntegrationTests.Server;
using NotificationService.Notifications;

namespace NotificationService.IntegrationTests.Features;

internal class DatabaseWrapper : IAsyncDisposable
{
    private readonly AsyncServiceScope _scope;
    private readonly SqlServerConnectionFactory _connectionFactory;

    public DatabaseWrapper(CustomTestApplicationFactory applicationFactory)
    {
        _scope = applicationFactory.Services.CreateAsyncScope();
        _connectionFactory = _scope.ServiceProvider
            .GetRequiredService<SqlServerConnectionFactory>();
    }

    public async Task ClearDatabaseAsync()
    {
        using var conn = _connectionFactory.Create();
        await conn.ExecuteScalarAsync("DELETE FROM Notifications");
    }

    public async Task<IEnumerable<Notification>> GetAllNotificationsAsync()
    {
        using var conn = _connectionFactory.Create();
        return await conn.QueryAsync<Notification>(
            "SELECT * FROM Notifications");
    }

    public async Task<Notification?> GetByIdAsync(Guid id)
    {
        using var conn = _connectionFactory.Create();
        var results = await conn.QueryAsync<Notification>(
            "SELECT * FROM Notifications WHERE Id = @id", new { id });
        return results.FirstOrDefault();
    }

    public async Task AddNotificationAsync(Notification notification)
    {
        using var conn = _connectionFactory.Create();
        var cmd = new CommandDefinition(
            "INSERT INTO Notifications VALUES (@Id, @From, @To, @Text)",
            notification);
        await conn.ExecuteScalarAsync(cmd);
    }

    public async ValueTask DisposeAsync()
    {
        await ClearDatabaseAsync();

        await _scope.DisposeAsync();

        GC.SuppressFinalize(this);
    }
}
