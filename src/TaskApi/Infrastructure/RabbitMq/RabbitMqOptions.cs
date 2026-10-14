using System.ComponentModel.DataAnnotations;

namespace TaskApi.Infrastructure.RabbitMq;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required]
    public string HostName { get; init; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; init; } = 5672;

    [Required]
    public string UserName { get; init; } = "";

    public string Password { get; init; } = "";

    [Required]
    public string VirtualHost { get; init; } = "/";

    [Required]
    public string Exchange { get; init; } = "task.events";

    [Range(1, 60)]
    public int ConnectionTimeoutSeconds { get; init; } = 3;

    [Range(1, 60)]
    public int ShutdownDrainTimeoutSeconds { get; init; } = 5;

    [Range(0, 300)]
    public int ReconnectCooldownSeconds { get; init; } = 5;

    public TimeSpan ConnectionTimeout => TimeSpan.FromSeconds(ConnectionTimeoutSeconds);

    public TimeSpan ShutdownDrainTimeout => TimeSpan.FromSeconds(ShutdownDrainTimeoutSeconds);

    public TimeSpan ReconnectCooldown => TimeSpan.FromSeconds(ReconnectCooldownSeconds);
}
