using Microsoft.AspNetCore.HttpsPolicy;

namespace API.Auth;

public static class HttpsSecurityExtensions
{
    public static IApplicationBuilder UseHttpsSecurity(this IApplicationBuilder app)
    {
        var hostEnvironment = app.ApplicationServices.GetRequiredService<IHostEnvironment>();
        var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
        var httpsEnabled = configuration.GetValue("Https:Enabled", true);
        
        if (!hostEnvironment.IsDevelopment() && httpsEnabled)
        {
            app.UseHttpsRedirection();
        }

        if (!hostEnvironment.IsDevelopment() && httpsEnabled)
        {
            app.UseHsts();
        }

        return app;
    }

    /// <summary>
    /// Configures HSTS options for staging/production environments.
    /// Enforces max-age of 1 year, includeSubDomains, and preload per SEC-01 requirements.
    /// </summary>
    public static void ConfigureHstsOptions(HstsOptions options)
    {
        // max-age = 1 year (31536000 seconds) per D-12
        options.MaxAge = TimeSpan.FromDays(365);
        
        // includeSubDomains protects all subdomains
        options.IncludeSubDomains = true;
        
        // preload required for browser opt-in to HSTS preload list
        options.Preload = true;
    }
}
