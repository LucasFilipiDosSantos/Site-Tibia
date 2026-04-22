using API.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace UnitTests.Identity;

[Trait("Category", "IdentitySecurity")]
[Trait("Requirement", "AUTH-03")]
public sealed class AdminJwtAuthorizationTests
{
    [Fact]
    public async Task JwtValidation_ValidAdminToken_ReturnsOkForAdminProbe()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var token = BuildJwt(
            issuer: ApiFactory.Issuer,
            audience: ApiFactory.Audience,
            signingKey: ApiFactory.SigningKey,
            role: "Admin",
            emailVerified: true);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/auth/admin/probe");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task JwtValidation_WrongSigningKey_ReturnsUnauthorizedForAdminProbe()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var token = BuildJwt(
            issuer: ApiFactory.Issuer,
            audience: ApiFactory.Audience,
            signingKey: "abcdefghijklmnopqrstuvwxyz123456",
            role: "Admin",
            emailVerified: true);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/auth/admin/probe");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task JwtValidation_WrongIssuer_ReturnsUnauthorizedForAdminProbe()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var wrongIssuerToken = BuildJwt(
            issuer: "wrong-issuer",
            audience: ApiFactory.Audience,
            signingKey: ApiFactory.SigningKey,
            role: "Admin",
            emailVerified: true);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", wrongIssuerToken);
        var wrongIssuerResponse = await client.GetAsync("/auth/admin/probe");
        Assert.Equal(HttpStatusCode.Unauthorized, wrongIssuerResponse.StatusCode);
    }

    [Fact]
    public async Task JwtValidation_WrongAudience_ReturnsUnauthorizedForAdminProbe()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var wrongAudienceToken = BuildJwt(
            issuer: ApiFactory.Issuer,
            audience: "wrong-audience",
            signingKey: ApiFactory.SigningKey,
            role: "Admin",
            emailVerified: true);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", wrongAudienceToken);
        var wrongAudienceResponse = await client.GetAsync("/auth/admin/probe");
        Assert.Equal(HttpStatusCode.Unauthorized, wrongAudienceResponse.StatusCode);
    }

    [Fact]
    public async Task ValidNonAdminToken_ReturnsForbiddenForAdminProbe()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var token = BuildJwt(
            issuer: ApiFactory.Issuer,
            audience: ApiFactory.Audience,
            signingKey: ApiFactory.SigningKey,
            role: "Customer",
            emailVerified: true);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/auth/admin/probe");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InvalidToken_ReturnsUnauthorizedForAdminProbe()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.valid.jwt");
        var response = await client.GetAsync("/auth/admin/probe");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string BuildJwt(string issuer, string audience, string signingKey, string role, bool emailVerified)
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString()),
            new("email", "admin@test.com"),
            new("role", role),
            new("email_verified", emailVerified ? "true" : "false")
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(10).UtcDateTime,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public const string Issuer = "tibia-webstore";
        public const string Audience = "tibia-webstore-client";
        public const string SigningKey = "01234567890123456789012345678901";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = Issuer,
                    ["Jwt:Audience"] = Audience,
                    ["Jwt:SigningKey"] = SigningKey,
                    ["IdentityTokenDelivery:Provider"] = "inmemory",
                    ["MercadoPago:AccessToken"] = "TEST-access-token",
                    ["MercadoPago:PublicKey"] = "TEST-public-key",
                    ["MercadoPago:NotificationUrl"] = "https://test.local/api/payments/webhook",
                    ["MercadoPago:SuccessUrl"] = "https://test.local/checkout/success",
                    ["MercadoPago:FailureUrl"] = "https://test.local/checkout/failure",
                    ["MercadoPago:PendingUrl"] = "https://test.local/checkout/pending",
                    ["WhatsApp:AccessToken"] = "test-token",
                    ["WhatsApp:PhoneNumberId"] = "test-phone",
                    ["WhatsApp:WhatsAppBusinessId"] = "test-business",
                    ["Hangfire:Enabled"] = "false"
                });
            });
        }
    }
}
