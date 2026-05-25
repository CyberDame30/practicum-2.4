using Microsoft.AspNetCore.Builder; using Microsoft.Extensions.Hosting; using Microsoft.Extensions.DependencyInjection;
namespace Nimble.Modulith.ServiceDefaults;
public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder){ builder.Services.AddHealthChecks(); return builder; }
    public static WebApplication MapDefaultEndpoints(this WebApplication app){ app.MapHealthChecks("/health"); return app; }
}
