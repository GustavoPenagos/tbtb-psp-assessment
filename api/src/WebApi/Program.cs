using WebApi.DependencyInjection;
using WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Registro modular de dependencias (Infraestructura, Aplicación, WebApi, Swagger y CORS)
builder.Services.AddAppDependencies(builder.Configuration);

var app = builder.Build();

// Middleware de Seguridad (Security Headers)
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// Middleware Global de Excepciones (RFC 7807 ProblemDetails + Logging Forense)
app.UseMiddleware<ExceptionMiddleware>();

// Swagger UI accesible en /swagger y con redirección desde /
app.UseSwagger(options =>
{
    options.RouteTemplate = "swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PSP Assessment API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "PSP API Docs — TBTB Global";
    c.DefaultModelsExpandDepth(1);
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    c.DisplayRequestDuration();
    c.EnableFilter();
});

// Redirección de la raíz "/" a "/swagger" (excluido de la documentación OpenAPI)
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.UseCors("FrontendCorsPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();
