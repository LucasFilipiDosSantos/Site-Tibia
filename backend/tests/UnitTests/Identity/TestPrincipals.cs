using System.Security.Claims;

namespace UnitTests.Identity;

internal static class TestPrincipals
{
    public static ClaimsPrincipal WithClaims(params (string Type, string Value)[] claims)
    {
        var identity = new ClaimsIdentity(
            claims.Select(c => new Claim(c.Type, c.Value)),
            authenticationType: "Test");

        return new ClaimsPrincipal(identity);
    }
}
