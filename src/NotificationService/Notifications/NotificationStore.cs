using Dapper;
using NotificationService.Infrastructure;

namespace NotificationService.Notifications;

internal class NotificationStore
{
    private readonly SqlServerConnectionFactory _connectionFactory;

    public NotificationStore(SqlServerConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task CreateAsync(Notification notification, CancellationToken token = default)
    {
        using var conn = _connectionFactory.Create();
        var cmd = new CommandDefinition(
            "INSERT INTO Notifications VALUES (@Id, @From, @To, @Text)",
            notification,
            cancellationToken: token);
        await conn.ExecuteScalarAsync(cmd);
    }

    public async Task DeleteAsync(Notification notification, CancellationToken token = default)
    {
        using var conn = _connectionFactory.Create();
        var cmd = new CommandDefinition(
            "DELETE FROM Notifications WHERE Id = @Id",
            notification,
            cancellationToken: token);
        await conn.ExecuteScalarAsync(cmd);
    }

    public async Task<IEnumerable<Notification>> GetAsync(CancellationToken token = default)
    {
        using var conn = _connectionFactory.Create();
        var cmd = new CommandDefinition(
            "SELECT * FROM Notifications",
            cancellationToken: token);
        return await conn.QueryAsync<Notification>(cmd);
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        using var conn = _connectionFactory.Create();
        var cmd = new CommandDefinition(
            "SELECT * FROM Notifications WHERE Id = @id",
            new { id },
            cancellationToken: token);
        var notifications = await conn.QueryAsync<Notification>(cmd);
        return notifications.FirstOrDefault();
    }
}
