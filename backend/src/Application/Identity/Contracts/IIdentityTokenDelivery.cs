namespace Application.Identity.Contracts;

public interface IIdentityTokenDelivery
{
    Task DeliverEmailVerificationTokenAsync(EmailVerificationTokenDeliveryPayload payload, CancellationToken cancellationToken = default);
    Task DeliverPasswordResetTokenAsync(PasswordResetTokenDeliveryPayload payload, CancellationToken cancellationToken = default);
}

public sealed record EmailVerificationTokenDeliveryPayload(
    Guid UserId,
    string Email,
    string RawToken,
    DateTimeOffset ExpiresAtUtc);

public sealed record PasswordResetTokenDeliveryPayload(
    Guid UserId,
    string Email,
    string RawToken,
    DateTimeOffset ExpiresAtUtc);
