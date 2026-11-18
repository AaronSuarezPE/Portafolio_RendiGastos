using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RendiGastos.Api.Configuracion;
using RendiGastos.Api.Datos;
using RendiGastos.Api.Servicios;
using Serilog;
using Serilog.Events;

// Las columnas llegan como cod_rndcn y las clases tienen CodRndcn.
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((contexto, log) => log
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    // ManejadorExcepciones ya deja cada error en el log con el nivel que corresponde
    .MinimumLevel.Override("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogEventLevel.Fatal)
    .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "Logs", "rendigastos-.log"),
        formatProvider: System.Globalization.CultureInfo.InvariantCulture,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} | {Level:u3} | {SourceContext} | {Message:lj} {Exception}{NewLine}"));

var configuracion = builder.Configuration;
var servicios = builder.Services;

servicios.Configure<OpcionesArchivos>(configuracion.GetSection(OpcionesArchivos.Seccion));
servicios.Configure<OpcionesJwt>(configuracion.GetSection(OpcionesJwt.Seccion));
servicios.Configure<OpcionesPush>(configuracion.GetSection(OpcionesPush.Seccion));
servicios.Configure<OpcionesDemo>(configuracion.GetSection(OpcionesDemo.Seccion));

// Acceso a datos y servicios de negocio
servicios.AddSingleton<IFabricaConexion, FabricaConexion>();
servicios.AddSingleton<IUsuarioRepositorio, UsuarioRepositorio>();
servicios.AddSingleton<ICategoriaRepositorio, CategoriaRepositorio>();
servicios.AddSingleton<IRendicionRepositorio, RendicionRepositorio>();
servicios.AddSingleton<IAvisoRepositorio, AvisoRepositorio>();
servicios.AddSingleton<IClaveService, ClaveService>();
servicios.AddSingleton<ITokenService, TokenService>();
servicios.AddSingleton<INotificador, NotificadorPush>();
servicios.AddScoped<ISeguridad, Seguridad>();
servicios.AddScoped<IAutenticacionService, AutenticacionService>();
servicios.AddScoped<IUsuarioService, UsuarioService>();
servicios.AddScoped<ICategoriaService, CategoriaService>();
servicios.AddScoped<IRendicionService, RendicionService>();
servicios.AddScoped<IAvisoService, AvisoService>();
servicios.AddHostedService<SembradorDemo>();

// Autenticación con JWT (el rol real se vuelve a verificar en la base en cada acción)
servicios.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.MapInboundClaims = false);
servicios.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<OpcionesJwt>>((jwt, opciones) =>
    {
        var o = opciones.Value;
        if (o.Clave.Length < 32)
            throw new InvalidOperationException("Configura Jwt:Clave con al menos 32 caracteres (variable Jwt__Clave o user-secrets).");

        jwt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = o.Emisor,
            ValidAudience = o.Audiencia,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.Clave)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
servicios.AddAuthorization();
servicios.AddExceptionHandler<ManejadorExcepciones>();

// CORS: solo los sitios indicados en la configuración
var origenes = configuracion.GetSection("Cors:Origenes").Get<string[]>() ?? [];
servicios.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(origenes).AllowAnyHeader().WithMethods("GET", "POST", "PUT", "DELETE")));

// Límite de intentos de ingreso por dirección: frena la fuerza bruta
var limiteLogin = configuracion.GetValue("Limites:LoginPorMinuto", 10);
servicios.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = async (contexto, ct) =>
        await contexto.HttpContext.Response.WriteAsJsonAsync(
            new { mensaje = "Hiciste demasiados intentos. Espera un minuto e inténtalo de nuevo." }, ct);

    o.AddPolicy("autenticacion", contexto => RateLimitPartition.GetFixedWindowLimiter(
        $"ip:{contexto.Connection.RemoteIpAddress}",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = limiteLogin, Window = TimeSpan.FromMinutes(1) }));
});

// Swagger: documentación y pruebas de la API (botón «Authorize» para pegar el token de la sesión)
servicios.AddEndpointsApiExplorer();
servicios.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "RendiGastos API", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header,
        Description = "Pega el token que devuelve el ingreso (sin la palabra Bearer)."
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
    });
});

servicios.AddControllers().ConfigureApiBehaviorOptions(o =>
    o.InvalidModelStateResponseFactory = _ =>
        new BadRequestObjectResult(new { mensaje = "La solicitud no es válida." }));

builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 12 * 1024 * 1024);

var app = builder.Build();

if (string.IsNullOrWhiteSpace(configuracion.GetConnectionString("RendiGastos")))
    throw new InvalidOperationException("Falta la cadena de conexión ConnectionStrings:RendiGastos.");

app.UseSerilogRequestLogging();

app.UseExceptionHandler(_ => { });

app.UseSwagger();
app.UseSwaggerUI(o => o.RoutePrefix = "swagger");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Logger.LogInformation("RendiGastos iniciada. Avisos push: {Push}.",
    app.Services.GetRequiredService<IOptions<OpcionesPush>>().Value.Habilitado ? "activos" : "solo dentro de la aplicación");

app.Run();

public partial class Program;
