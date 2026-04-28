using Microsoft.AspNetCore.Authentication.JwtBearer;
using NSwag.Generation.Processors.Security;

namespace Web.Api.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddOpenApiDocumentWithAuth(this IServiceCollection services)
    {
        services.AddOpenApiDocument(options =>
        {
            options.Title = "aspire-net10 API";
            options.Version = "v1";

            options.AddSecurity("Bearer", Enumerable.Empty<string>(), new NSwag.OpenApiSecurityScheme
            {
                Type = NSwag.OpenApiSecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT",
                Description = "Enter your JWT token"
            });

            options.OperationProcessors.Add(
                new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
        });
        return services;
    }
}
