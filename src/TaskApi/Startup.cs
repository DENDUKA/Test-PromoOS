using System.Text.Json.Serialization;
using TaskApi.Common;
using TaskApi.Features;
using TaskApi.Features.Tasks;
using TaskApi.Infrastructure;

namespace TaskApi;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(10));

        services
            .AddFeatures()
            .AddInfrastructure();
    }

    public void Configure(WebApplication app)
    {
        app.UseExceptionHandler();

        app.MapTaskEndpoints();
        app.MapHealthChecks("/health");
    }
}
