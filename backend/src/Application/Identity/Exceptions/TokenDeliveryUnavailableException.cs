namespace Application.Identity.Exceptions;

public sealed class TokenDeliveryUnavailableException : Exception
{
    public TokenDeliveryUnavailableException(string operation, string email, Exception innerException)
        : base($"Token delivery unavailable for operation '{operation}' and recipient '{email}'.", innerException)
    {
        Operation = operation;
        Email = email;
    }

    public string Operation { get; }

    public string Email { get; }
}
