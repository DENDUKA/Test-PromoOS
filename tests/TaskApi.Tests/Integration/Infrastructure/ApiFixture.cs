using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskApi.Infrastructure.Persistence.EntityFramework;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace TaskApi.Tests.Integration.Infrastructure;

public class ApiFixture : IAsyncLifetime
{
    private const string PostgresVariable = "TASKAPI_TEST_POSTGRES";

    public static readonly string RabbitUser = Environment.GetEnvironmentVariable("TASKAPI_TEST_RABBITMQ_USER") ?? "taskapi";
    public static readonly string RabbitPassword = Environment.GetEnvironmentVariable("TASKAPI_TEST_RABBITMQ_PASSWORD") ?? "taskapi";

    private PostgreSqlContainer? _postgresContainer;
    private RabbitMqContainer? _rabbitContainer;
    private string _postgresConnectionString = "";
    private WebApplicationFactory<Program>? _factory;

    public InMemoryLoggerProvider Logs { get; } = new();

    public string RabbitHost { get; private set; } = "";

    public int RabbitPort { get; private set; }

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable(PostgresVariable) is { Length: > 0 } externalPostgres)
        {
            _postgresConnectionString = externalPostgres;
            RabbitHost = Environment.GetEnvironmentVariable("TASKAPI_TEST_RABBITMQ_HOST") ?? "localhost";
            RabbitPort = int.Parse(
                Environment.GetEnvironmentVariable("TASKAPI_TEST_RABBITMQ_PORT") ?? "5672",
                CultureInfo.InvariantCulture);
        }
        else
        {
            _postgresContainer = new PostgreSqlBuilder().WithImage("postgres:17-alpine").Build();
            _rabbitContainer = new RabbitMqBuilder()
                .WithImage("rabbitmq:4-management-alpine")
                .WithUsername(RabbitUser)
                .WithPassword(RabbitPassword)
                .Build();

            await Task.WhenAll(_postgresContainer.StartAsync(), _rabbitContainer.StartAsync());

            _postgresConnectionString = _postgresContainer.GetConnectionString();
            RabbitHost = _rabbitContainer.Hostname;
            RabbitPort = _rabbitContainer.GetMappedPublicPort(5672);
        }

        _factory = CreateFactory(RabbitHost, RabbitPort);
        Client = _factory.CreateClient();
    }

    public WebApplicationFactory<Program> CreateFactory(string rabbitHost, int rabbitPort, int rabbitConnectTimeoutSeconds = 3) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgresConnectionString,
                ["RabbitMq:HostName"] = rabbitHost,
                ["RabbitMq:Port"] = rabbitPort.ToString(CultureInfo.InvariantCulture),
                ["RabbitMq:UserName"] = RabbitUser,
                ["RabbitMq:Password"] = RabbitPassword,
                ["RabbitMq:ConnectionTimeoutSeconds"] = rabbitConnectTimeoutSeconds.ToString(CultureInfo.InvariantCulture),
            }));

            builder.ConfigureLogging(logging => logging.AddProvider(Logs));
        });

    public async Task ResetDatabaseAsync()
    {
        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();

        await db.Tasks.ExecuteDeleteAsync();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        if (_rabbitContainer is not null)
        {
            await _rabbitContainer.DisposeAsync();
        }

        if (_postgresContainer is not null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }
}
