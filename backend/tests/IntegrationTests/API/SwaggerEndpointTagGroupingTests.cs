using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace IntegrationTests.Swagger;

public sealed class SwaggerEndpointTagGroupingTests
{
    [Theory]
    [InlineData("/products", "get", "Public Catalog")]
    [InlineData("/products/{slug}", "get", "Public Catalog")]
    [InlineData("/auth/register", "post", "Auth")]
    [InlineData("/auth/login", "post", "Auth")]
    [InlineData("/auth/refresh", "post", "Auth")]
    [InlineData("/auth/verify-email/request", "post", "Auth")]
    [InlineData("/auth/verify-email/confirm", "post", "Auth")]
    [InlineData("/auth/password-reset/request", "post", "Auth")]
    [InlineData("/auth/password-reset/confirm", "post", "Auth")]
    [InlineData("/auth/admin/probe", "get", "Health/Probes")]
    [InlineData("/auth/verified/probe", "get", "Health/Probes")]
    [InlineData("/admin/catalog/categories", "post", "Admin Catalog")]
    [InlineData("/admin/catalog/categories/{slug}", "delete", "Admin Catalog")]
    [InlineData("/admin/catalog/products", "post", "Admin Catalog")]
    [InlineData("/admin/catalog/products/{slug}", "put", "Admin Catalog")]
    public async Task SwaggerV1_UsesAudienceTags(string route, string method, string expectedTag)
    {
        await using var factory = new SwaggerApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var tags = GetOperationTags(payload, route, method);

        var actualTag = Assert.Single(tags);
        Assert.Equal(expectedTag, actualTag);
    }

    private static IReadOnlyList<string> GetOperationTags(JsonElement swagger, string route, string method)
    {
        var paths = swagger.GetProperty("paths");
        var pathItem = paths.GetProperty(route);
        var operation = pathItem.GetProperty(method);
        var tags = operation.GetProperty("tags").EnumerateArray().Select(x => x.GetString()!).ToList();
        return tags;
    }

    private sealed class SwaggerApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(
                [
                    new KeyValuePair<string, string?>("Jwt:Issuer", "tibia-webstore"),
                    new KeyValuePair<string, string?>("Jwt:Audience", "tibia-webstore-client"),
                    new KeyValuePair<string, string?>("Jwt:SigningKey", "01234567890123456789012345678901"),
                    new KeyValuePair<string, string?>("IdentityTokenDelivery:Provider", "inmemory")
                ]);
            });
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
        }
    }
}
