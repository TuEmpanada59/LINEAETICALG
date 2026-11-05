using System.Text.Json.Serialization;
using LineaEtica.Api;
using LineaEtica.Api.Seguridad;
using LineaEtica.Domain;
using LineaEtica.Infrastructure;
using LineaEtica.Infrastructure.Servicios;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = _ =>
        new BadRequestObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Datos no válidos",
            Detail = "La solicitud no tiene el formato esperado.",
        }));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorErrores>();
builder.Services.AgregarInfraestructura(builder.Configuration);

builder.Services
    .AddAuthentication()
    .AddCookie(Esquemas.Seguimiento, o =>
    {
        o.Cookie.Name = "le_seguimiento";
        o.Cookie.Path = "/api/publico/seguimiento";
        o.Cookie.HttpOnly = true;
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        o.SlidingExpiration = true;
        // Es una API: sin redirecciones a páginas de inicio de sesión.
        o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    })
    .AddCookie(Esquemas.Gestion, o =>
    {
        o.Cookie.Name = "le_gestion";
        o.Cookie.Path = "/api/gestion";
        o.Cookie.HttpOnly = true;
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        o.SlidingExpiration = true;
        o.Events.OnValidatePrincipal = SesionesGestion.Validar;
        o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Politicas.Denunciante, p => p
        .AddAuthenticationSchemes(Esquemas.Seguimiento)
        .RequireAuthenticatedUser()
        .RequireClaim(Reclamos.Denuncia))
    .AddPolicy(Politicas.GestorInicial, p => p
        .AddAuthenticationSchemes(Esquemas.Gestion)
        .RequireAuthenticatedUser()
        .RequireClaim(Reclamos.Gestor))
    .AddPolicy(Politicas.Gestor, p => p
        .AddAuthenticationSchemes(Esquemas.Gestion)
        .RequireAuthenticatedUser()
        .RequireClaim(Reclamos.Etapa, Reclamos.EtapaCompleta))
    .AddPolicy(Politicas.Oficial, p => p
        .AddAuthenticationSchemes(Esquemas.Gestion)
        .RequireAuthenticatedUser()
        .RequireClaim(Reclamos.Etapa, Reclamos.EtapaCompleta)
        .RequireClaim(Reclamos.Rol, nameof(RolGestor.OficialCumplimiento)));

var app = builder.Build();

// Primer oficial: solo si se configuró Semilla:Oficial:* y la base aún no tiene gestores.
if (!string.IsNullOrWhiteSpace(app.Configuration["Semilla:Oficial:Correo"]))
{
    await using var alcance = app.Services.CreateAsyncScope();
    if (await alcance.ServiceProvider.GetRequiredService<GestoresService>().SembrarOficial(app.Configuration))
    {
        app.Logger.LogInformation("Se creó el oficial de cumplimiento inicial.");
    }
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Solo en desarrollo: confirma la conexión y las migraciones sin exponer la cadena.
    app.MapGet("/api/desarrollo/conexion", async (LineaEticaDbContext db) =>
    {
        if (!await db.Database.CanConnectAsync())
        {
            return Results.Problem("No se pudo conectar a la base de datos.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var pendientes = await db.Database.GetPendingMigrationsAsync();
        return Results.Ok(new
        {
            conectada = true,
            baseDatos = db.Database.GetDbConnection().Database,
            migracionesPendientes = pendientes.ToList(),
        });
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>Visible para las pruebas de integración.</summary>
public partial class Program;
