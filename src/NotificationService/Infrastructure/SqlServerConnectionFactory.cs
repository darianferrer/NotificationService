using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using NotificationService.Settings;

namespace NotificationService.Infrastructure;

internal class SqlServerConnectionFactory
{
    private readonly string _connectionString;

    public SqlServerConnectionFactory(IOptions<AppSettings> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public IDbConnection Create() => new SqlConnection(_connectionString);
}