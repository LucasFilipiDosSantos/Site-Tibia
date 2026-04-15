using Application.Identity.Contracts;
using Application.Identity.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Identity;

[Trait("Category", "IdentitySecurity")]
[Trait("Requirement", "AUTH-01")]
[Trait("Suite", "Phase01AuthRegression")]
[Trait("Plan", "01-06")]
public sealed class RegisterValidationErrorContractTests
{
    [Fact]
    public async Task Register_WeakPassword_ReturnsProblemDetailsBadRequest()
    {
        await using var factory = new RegisterApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/register",
            new { email = "weak@test.com", password = "weak" }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var payload = await response.Content.ReadFromJsonAsync<ProblemDetailsPayload>();
        Assert.NotNull(payload);
        Assert.Equal("Validation failed.", payload!.Title);
        Assert.Equal("Password does not meet complexity requirements.", payload.Detail);
        Assert.Equal(400, payload.Status);
    }

    [Fact]
    public async Task Register_StrongPassword_ReturnsSuccessContract()
    {
        await using var factory = new RegisterApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/register",
            new { email = "strong@test.com", password = "ValidPass123!" }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SuccessPayload>();
        Assert.NotNull(payload);
        Assert.Equal("Registration successful.", payload!.Message);
    }

    [Fact]
    public async Task Register_WeakPassword_ProblemDetails_DoesNotLeakStackTrace()
    {
        await using var factory = new RegisterApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/register",
            new { email = "nostack@test.com", password = "weak" }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Microsoft.", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("System.", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExceptionHandlerMiddleware", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GlobalExceptionHandler", body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RegisterApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private readonly InMemoryUserRepository _users = new();
        private readonly InMemoryRefreshSessionRepository _refreshSessions = new();
        private readonly InMemorySecurityTokenRepository _tokens = new();
        private readonly FixedClock _clock = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        private readonly FakeTokenService _tokenService = new();
        private readonly FakePasswordHasherService _passwordHasher = new();
        private readonly InMemoryIdentityTokenDelivery _tokenDelivery = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Jwt:Issuer"] = "tibia-webstore",
                        ["Jwt:Audience"] = "tibia-webstore-client",
                        ["Jwt:SigningKey"] = "01234567890123456789012345678901",
                    }
                );
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUserRepository>();
                services.RemoveAll<IRefreshSessionRepository>();
                services.RemoveAll<ISecurityTokenRepository>();
                services.RemoveAll<ITokenService>();
                services.RemoveAll<IPasswordHasherService>();
                services.RemoveAll<ISystemClock>();
                services.RemoveAll<IIdentityTokenDelivery>();

                services.AddSingleton<IUserRepository>(_users);
                services.AddSingleton<IRefreshSessionRepository>(_refreshSessions);
                services.AddSingleton<ISecurityTokenRepository>(_tokens);
                services.AddSingleton<ITokenService>(_tokenService);
                services.AddSingleton<IPasswordHasherService>(_passwordHasher);
                services.AddSingleton<ISystemClock>(_clock);
                services.AddSingleton<IIdentityTokenDelivery>(_tokenDelivery);
            });
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
        }
    }

    private sealed record ProblemDetailsPayload(string? Title, string? Detail, int? Status);

    private sealed record SuccessPayload(string Message);
}
