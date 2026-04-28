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

    public static void ConfigureHstsOptions(HstsOptions options)
    {
        options.MaxAge = TimeSpan.FromDays(365);
        options.IncludeSubDomains = true;
        options.Preload = true;
    }
}
