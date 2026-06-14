using System.Collections.Concurrent;

namespace ScentMarekt.Server.Services;

public class LogBuffer
{
    private readonly ConcurrentQueue<string> _logs = new();
    private readonly int _maxLines;

    public LogBuffer(int maxLines = 500)
    {
        _maxLines = maxLines;
    }

    public void AddLog(string message)
    {
        _logs.Enqueue(message);
        while (_logs.Count > _maxLines)
        {
            _logs.TryDequeue(out _);
        }
    }

    public IReadOnlyList<string> GetLogs() => _logs.ToArray();
}

public class InMemoryLogger : ILogger
{
    private readonly string _categoryName;
    private readonly LogBuffer _buffer;

    public InMemoryLogger(string categoryName, LogBuffer buffer)
    {
        _categoryName = categoryName;
        _buffer = buffer;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        if (string.IsNullOrWhiteSpace(message)) return;

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var level = logLevel switch
        {
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "DBG"
        };

        var logEntry = $"[{timestamp}] [{level}] [{_categoryName}] {message}";
        _buffer.AddLog(logEntry);
    }
}

public class InMemoryLoggerProvider : ILoggerProvider
{
    private readonly LogBuffer _buffer;

    public InMemoryLoggerProvider(LogBuffer buffer)
    {
        _buffer = buffer;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new InMemoryLogger(categoryName, _buffer);
    }

    public void Dispose() { }
}

public static class InMemoryLoggerExtensions
{
    public static ILoggingBuilder AddInMemoryLogger(this ILoggingBuilder builder, LogBuffer buffer)
    {
        builder.AddProvider(new InMemoryLoggerProvider(buffer));
        return builder;
    }
}
