namespace Application.Identity.Contracts;

public interface IPasswordHasherService
{
    string HashPassword(string password);
    PasswordHashVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword);
}

public sealed record PasswordHashVerificationResult(bool Succeeded, bool NeedsRehash);
