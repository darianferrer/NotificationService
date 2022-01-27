using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using FluentMigrator.Runner;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationService.Infrastructure;
using WireMock.Server;

namespace NotificationService.IntegrationTests.Server;

public class CustomTestApplicationFactory : WebApplicationFactory<Program>
{
    private ICompositeService _compositeService;
    private IConfigurationRoot _configuration;

    public WireMockServer FunTranslationsServer { get; }

    public CustomTestApplicationFactory()
    {
        LoadConfiguration();

        SetupDatabaseContainer();

        var baseAddress = _configuration["AppSettings:FunTranslations"];
        FunTranslationsServer = WireMockServer.Start(baseAddress);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder
            .ConfigureAppConfiguration(c => c.AddConfiguration(_configuration))
            .UseEnvironment("test");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _compositeService.Dispose();
            FunTranslationsServer.Dispose();
        }
    }

    [MemberNotNull(nameof(_configuration))]
    private void LoadConfiguration()
    {
        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.test.json")
            .Build();
    }

    [MemberNotNull(nameof(_compositeService))]
    private void SetupDatabaseContainer()
    {
        var dockerFile = _configuration["DockerComposeFile"];

        Console.WriteLine("Starting Docker container with SQL database");
        _compositeService = new Builder()
            .UseContainer()
            .WaitForPort("1500/tcp", 5000)
            .UseCompose()
            .FromFile(dockerFile)
            .RemoveOrphans()
            .RemoveAllImages()
            .Build()
            .Start();

        using var serviceProvider = GetServiceProvider();
        MigrateDatabase(serviceProvider);
    }

    private ServiceProvider GetServiceProvider()
    {
        var connectionString = _configuration["AppSettings:ConnectionString"];
        var serviceProvider = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSqlServer()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(InitialMigration).Assembly)
                .For
                .Migrations())
            .BuildServiceProvider(false);
        return serviceProvider;
    }

    private static void MigrateDatabase(ServiceProvider serviceProvider)
    {
        Console.WriteLine("Starting migrating SQL database to latest version");
        var retries = 20;
        while (retries > 0)
        {
            using var scope = serviceProvider.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
            try
            {
                runner.MigrateUp();
                Console.WriteLine("Migration command succeed!");
                break;
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Migration command failed, trying again in 1s. Error: {ex.Message}");
                Thread.Sleep(1000);
                retries--;
            }
        }
    }
}