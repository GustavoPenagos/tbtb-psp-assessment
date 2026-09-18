using Application.Interfaces;
using Application.Mappings;
using Application.Services;
using Application.Validators;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Repositories;
using WebApi.Middleware;
using WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de Base de Datos y Repositorios
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IRegistrationLinkRepository, RegistrationLinkRepository>();

// 2. Servicios de Aplicación
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();

// 3. Logger forense en disco (E:\logs\logs_TBTB.PSP.text)
builder.Services.AddSingleton<IFileLoggerService, FileLoggerService>();

// 4. AutoMapper (DTOs <-> Entidades de Dominio)
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

// 5. FluentValidation con sanitización y reglas estrictas
builder.Services.AddValidatorsFromAssemblyContaining<PatientRegisterRequestValidator>();

// 6. Controladores MVC
builder.Services.AddControllers();

// 7. Swagger / OpenAPI con estándares GxP y documentación XML
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PSP Assessment API — TBTB Global",
        Version = "v1",
        Description = "API RESTful para el Programa de Acompañamiento a Pacientes (PSP) en Colombia (CO), Perú (PE) y Ecuador (EC).\n\n" +
                      "### Estándares y Políticas Implementadas:\n" +
                      "- **GxP / ALCOA+:** Trazabilidad inmutable, pistas de auditoría obligatorias (motivo ≥ 10 caracteres) y snapshots JSON.\n" +
                      "- **Seguridad OWASP:** Sanitización de entradas, prevención XSS y 100% de consultas parametrizadas mediante Stored Procedures.\n" +
                      "- **RFC 7807 (ProblemDetails):** Formato estándar de errores con códigos legibles y logging forense.\n" +
                      "- **Validación Multi-País:** Prefijos E.164 (+57, +51, +593) y formatos oficiales de identificación (CC, CE, DNI, RUT, PASAPORTE).",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "TBTB Global - PSP Engineering",
            Email = "psp-support@tbtb-global.com"
        }
    });

    // Inclusión de comentarios XML de WebApi (Controladores y Respuestas)
    var xmlWebApi = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlWebApiFullPath = Path.Combine(AppContext.BaseDirectory, xmlWebApi);
    if (File.Exists(xmlWebApiFullPath))
    {
        options.IncludeXmlComments(xmlWebApiFullPath, includeControllerXmlComments: true);
    }

    // Inclusión de comentarios XML de Application (DTOs y Modelos)
    var xmlApp = "Application.xml";
    var xmlAppFullPath = Path.Combine(AppContext.BaseDirectory, xmlApp);
    if (File.Exists(xmlAppFullPath))
    {
        options.IncludeXmlComments(xmlAppFullPath);
    }
});

// 8. CORS restrictivo para Frontend Angular
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

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
