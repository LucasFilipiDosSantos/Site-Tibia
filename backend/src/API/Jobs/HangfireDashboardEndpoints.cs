using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;

namespace API.Jobs;

public static class HangfireDashboardEndpoints
{
    public static WebApplication MapHangfireDashboard(this WebApplication app, string path = "/hangfire")
    {
        var env = app.Environment;

        app.UseHangfireDashboard(path, new DashboardOptions
        {
            Authorization = [new HangfireAuthorizationFilter(env)]
        });

        return app;
    }

    private sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private readonly IWebHostEnvironment _environment;

        public HangfireAuthorizationFilter(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public bool Authorize(DashboardContext context)
        {
            if (_environment.IsDevelopment())
            {
                return true;
            }

            return false;
        }
    }
}
