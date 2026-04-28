namespace Web.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseOpenApiWithUi(this WebApplication app)
    {
        app.UseOpenApi();
        app.UseSwaggerUi(settings =>
        {
            settings.Path = "/swagger";
        });
        return app;
    }
}
