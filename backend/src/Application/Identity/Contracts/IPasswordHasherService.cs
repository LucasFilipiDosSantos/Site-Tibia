namespace Application.Identity.Contracts;

public interface IPasswordHasherService
{
    string HashPassword(string password);
    bool VerifyHashedPassword(string hashedPassword, string providedPassword);
}
