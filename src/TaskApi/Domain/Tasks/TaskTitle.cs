using System.Diagnostics.CodeAnalysis;

namespace TaskApi.Domain.Tasks;

/// <summary>
/// Value object заголовка. Инвариант: после trim не пустой и не длиннее <see cref="MaxLength"/>.
/// Пробельные строки считаются пустыми (иначе "   " пролезает как валидный Title).
/// </summary>
public sealed record TaskTitle
{
    public const int MaxLength = 200;

    public string Value { get; }

    private TaskTitle(string value) => Value = value;

    public static bool TryCreate(
        string? raw,
        [NotNullWhen(true)] out TaskTitle? title,
        [NotNullWhen(false)] out string? error)
    {
        var trimmed = raw?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            title = null;
            error = "Title must not be empty.";
            return false;
        }

        if (trimmed.Length > MaxLength)
        {
            title = null;
            error = $"Title must not exceed {MaxLength} characters.";
            return false;
        }

        title = new TaskTitle(trimmed);
        error = null;
        return true;
    }

    /// <summary>Для значений, которые уже прошли валидацию (чтение из БД, тесты).</summary>
    public static TaskTitle FromTrusted(string value) => new(value);

    public override string ToString() => Value;
}
