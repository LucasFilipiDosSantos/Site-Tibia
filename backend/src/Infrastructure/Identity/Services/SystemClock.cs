using Application.Identity.Contracts;

namespace Infrastructure.Identity.Services;

public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
