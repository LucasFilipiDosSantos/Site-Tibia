using Application.Identity.Contracts;
using Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Infrastructure.Identity.Services;

public sealed class PasswordHasherService : IPasswordHasherService
{
    private static readonly UserAccount HashContextUser = new("Password Hash Context", "hash@internal.local", "placeholder");

    private readonly PasswordHasher<UserAccount> _hasher = new(
        new OptionsWrapper<PasswordHasherOptions>(
            new PasswordHasherOptions
            {
                IterationCount = 210_000
            }));

    public string HashPassword(string password)
    {
        return _hasher.HashPassword(HashContextUser, password);
    }

    public PasswordHashVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(HashContextUser, hashedPassword, providedPassword);
        return new PasswordHashVerificationResult(
            result is Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success
                or Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded,
            result == Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded);
    }
}

