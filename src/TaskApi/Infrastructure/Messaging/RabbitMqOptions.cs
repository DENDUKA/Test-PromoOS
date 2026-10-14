using System.ComponentModel.DataAnnotations;

namespace TaskApi.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required]
    public string HostName { get; init; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; init; } = 5672;

    // Без значения по умолчанию: логин/пароль приходят из окружения (Development-файл, env, секреты).
    [Required]
    public string UserName { get; init; } = "";

    public string Password { get; init; } = "";

    [Required]
    public string VirtualHost { get; init; } = "/";

    [Required]
    public string Exchange { get; init; } = "task.events";

    [Range(1, 60)]
    public int ConnectionTimeoutSeconds { get; init; } = 3;

    /// <summary>Сколько при остановке ждём отправки уже поставленных в очередь событий.</summary>
    [Range(1, 60)]
    public int ShutdownDrainTimeoutSeconds { get; init; } = 5;

    /// <summary>
    /// После неудачного подключения новые попытки не делаем это время: иначе при лежащем брокере
    /// каждое событие ждало бы таймаут подключения и очередь не разгребалась бы.
    /// </summary>
    [Range(0, 300)]
    public int ReconnectCooldownSeconds { get; init; } = 5;

    public TimeSpan ConnectionTimeout => TimeSpan.FromSeconds(ConnectionTimeoutSeconds);

    public TimeSpan ShutdownDrainTimeout => TimeSpan.FromSeconds(ShutdownDrainTimeoutSeconds);

    public TimeSpan ReconnectCooldown => TimeSpan.FromSeconds(ReconnectCooldownSeconds);
}
