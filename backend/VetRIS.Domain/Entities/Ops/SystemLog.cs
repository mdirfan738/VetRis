using VetRIS.Domain.Common;

namespace VetRIS.Domain.Entities.Ops;

/// <summary>Persisted mirror of a subset of Serilog output (warnings/errors) for in-app diagnostics screens.</summary>
public class SystemLog : BaseEntity
{
    public string Level { get; private set; } = default!; // Information, Warning, Error, Critical
    public string Message { get; private set; } = default!;
    public string? Exception { get; private set; }
    public string? Source { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTime TimestampUtc { get; private set; }

    private SystemLog()
    {
    } // EF Core

    public SystemLog(string level, string message, string? exception = null, string? source = null, string? correlationId = null)
    {
        if (string.IsNullOrWhiteSpace(level)) throw new ArgumentException("Log level is required.", nameof(level));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Log message is required.", nameof(message));

        Level = level;
        Message = message;
        Exception = exception;
        Source = source;
        CorrelationId = correlationId;
        TimestampUtc = DateTime.UtcNow;
    }
}
