namespace Application.Identity.Contracts;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}
