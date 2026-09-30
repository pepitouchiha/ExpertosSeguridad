using ExpertosSeguridad.Application.Abstractions;

namespace ExpertosSeguridad.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
