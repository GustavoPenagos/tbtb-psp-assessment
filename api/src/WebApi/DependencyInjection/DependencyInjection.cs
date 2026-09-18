using System.Reflection;
using Application.Interfaces;
using Application.Mappings;
using Application.Services;
using Application.Validators;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.OpenApi.Models;
using WebApi.Services;

namespace WebApi.DependencyInjection;

/// <summary>
/// Contenedor modular para el registro e inyección de dependencias de la solución PSP.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra todas las dependencias de Infraestructura, Aplicación y Presentación (WebApi).
    /// </summary>
    public static IServiceCollection AddAppDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructureServices();
        services.AddApplicationServices();
        services.AddWebApiServices(configuration);
        return services;
    }

    /// <summary>
    /// 1. Servicios de Infraestructura: Base de datos SQL Server / LocalDB y Repositorios Dapper.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IRegistrationLinkRepository, RegistrationLinkRepository>();
        return services;
    }

    /// <summary>
    /// 2. Servicios de Aplicación: Casos de uso de negocio, AutoMapper y FluentValidation.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Servicios de dominio y casos de uso
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IRegistrationService, RegistrationService>();

        // AutoMapper para transformación desacoplada DTO <-> Entidad
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

        // Validadores FluentValidation con sanitización y reglas estrictas
        services.AddValidatorsFromAssemblyContaining<PatientRegisterRequestValidator>();

        return services;
    }

    /// <summary>
    /// 3. Servicios de WebApi: Logging forense, Controladores, Swagger OpenAPI y CORS.
    /// </summary>
    public static IServiceCollection AddWebApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Logger forense persistente en disco (E:\logs\logs_TBTB.PSP.text)
        services.AddSingleton<IFileLoggerService, FileLoggerService>();

        // Controladores de la API
        services.AddControllers();

        // Documentación Swagger / OpenAPI
        services.AddSwaggerDocumentation();

        // Política de CORS para el cliente Angular
        services.AddCorsPolicy(configuration);

        return services;
    }

    /// <summary>
    /// Configuración de Swagger / OpenAPI con metadatos farmacéuticos y documentación XML.
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "PSP Assessment API — TBTB Global",
                Version = "v1",
                Description = "API RESTful para el Programa de Acompañamiento a Pacientes (PSP) en Colombia (CO), Perú (PE) y Ecuador (EC).\n\n" +
                              "### Estándares y Políticas Implementadas:\n" +
                              "- **GxP / ALCOA+:** Trazabilidad inmutable, pistas de auditoría obligatorias (motivo ≥ 10 caracteres) y snapshots JSON.\n" +
                              "- **Seguridad OWASP:** Sanitización de entradas, prevención XSS y 100% de consultas parametrizadas mediante Stored Procedures.\n" +
                              "- **RFC 7807 (ProblemDetails):** Formato estándar de errores con códigos legibles y logging forense.\n" +
                              "- **Validación Multi-País:** Prefijos E.164 (+57, +51, +593) y formatos oficiales de identificación (CC, CE, DNI, RUT, PASAPORTE).",
                Contact = new OpenApiContact
                {
                    Name = "TBTB Global - PSP Engineering",
                    Email = "psp-support@tbtb-global.com"
                }
            });

            // Inclusión de comentarios XML de WebApi (Controladores y Respuestas)
            var xmlWebApi = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
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

        return services;
    }

    /// <summary>
    /// Configuración de CORS restrictivo para el origen del Frontend Angular.
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:4200" };

        services.AddCors(options =>
        {
            options.AddPolicy("FrontendCorsPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        return services;
    }
}
