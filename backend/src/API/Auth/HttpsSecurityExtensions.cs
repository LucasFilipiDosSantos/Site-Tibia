namespace API.Auth;

public static class HttpsSecurityExtensions
{
    public static IApplicationBuilder UseHttpsSecurity(this IApplicationBuilder app)
    {
        var hostEnvironment = app.ApplicationServices.GetRequiredService<IHostEnvironment>();
        if (!hostEnvironment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        if (!hostEnvironment.IsDevelopment())
        {
            app.UseHsts();
        }

        return app;
    }
}
