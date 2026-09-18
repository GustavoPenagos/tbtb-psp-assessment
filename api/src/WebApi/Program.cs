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

// 7. Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PSP Assessment API — TBTB Global",
        Version = "v1",
        Description = "API RESTful para el Programa de Acompañamiento a Pacientes (PSP) en Colombia, Perú y Ecuador. Diseñada bajo estándares GxP, ALCOA+ y 21 CFR Part 11."
    });
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

// Swagger UI en Development y Production para evaluación
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PSP Assessment API v1");
    c.RoutePrefix = string.Empty; // Swagger en raíz "/"
});

app.UseCors("FrontendCorsPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();
