using Application.Identity.Contracts;
using Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity.Services;

public sealed class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<UserAccount> _hasher = new();

    public string HashPassword(string password)
    {
        return _hasher.HashPassword(new UserAccount("hash@internal.local", "placeholder"), password);
    }

    public bool VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(new UserAccount("hash@internal.local", "placeholder"), hashedPassword, providedPassword);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
