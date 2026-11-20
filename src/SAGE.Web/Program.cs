
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using SAGE.Web.Components;
using SAGE.Web.Components.Account;
using SAGE.Web.Data;
using SAGE.Web.Api;
using SAGE.Web.Servicios.Seguridad;
using SAGE.Web.Servicios.Telemetria;
using SAGE.Web.Servicios.Alertas;
using SAGE.Web.Servicios.EnVivo;
using SAGE.Web.Servicios.Ordenes;
using SAGE.Web.Servicios.Dispositivo;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// COMPONENTES BLAZOR
// =====================================================

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<IdentityRedirectManager>();

builder.Services.AddScoped<
    AuthenticationStateProvider,
    IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddScoped<IUsuarioActual, UsuarioActual>();

// =====================================================
// AUTENTICACIÓN Y AUTORIZACIÓN
// =====================================================

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme =
        IdentityConstants.ApplicationScheme;

    options.DefaultSignInScheme =
        IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

builder.Services.AddAuthorization();

// =====================================================
// BASE DE DATOS SQL SERVER
// =====================================================

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// =====================================================
// ASP.NET CORE IDENTITY
// =====================================================

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;

    options.Stores.SchemaVersion =
        IdentitySchemaVersions.Version3;

    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

builder.Services.AddSingleton<
    IEmailSender<ApplicationUser>,
    IdentityNoOpEmailSender>();

// =====================================================
// MUDBLAZOR
// =====================================================

builder.Services.AddMudServices();

// =====================================================
// S23-M5: PATRONES ADAPTER Y FACADE
// =====================================================

// Patrón Adapter: convierte la telemetría
// recibida del ESP32 en entidades de SAGE.
builder.Services.AddScoped<
    IAdaptadorTelemetria,
    AdaptadorTelemetriaEsp32>();

// Patrón Facade: coordina la autenticación,
// las mediciones, los eventos y las alertas.
builder.Services.AddScoped<FachadaSincronizacion>();

// =====================================================
// S23-M6: SINGLETON + OBSERVER
// =====================================================

// Mantiene el último estado de los dispositivos
// y notifica a los componentes suscritos.
//
// Singleton: una instancia para toda la aplicación.
// Observer: notifica cambios en tiempo real.
builder.Services.AddSingleton<EstadoDispositivosEnVivo>();

// =====================================================
// S23-M7: SERVICIO DE ALERTAS
// =====================================================

// Evalúa los eventos nuevos recibidos.
// Abre y cierra alertas según las reglas del proyecto.
builder.Services.AddScoped<ServicioAlertas>();

// =====================================================
// S23-M7: VIGILANTE DE CONEXIÓN
// =====================================================

// Revisa cada 5 segundos los dispositivos activos.
// Si pasan más de 30 segundos sin telemetría,
// registra una alerta sin_conexion.
//
// Solo se registra el vigilante nuevo.
builder.Services.AddHostedService<VigilanteConexion>();

// =====================================================
// S23-M9: ÓRDENES (PATRÓN COMMAND)
// =====================================================

if (builder.Configuration["Dispositivo:Modo"] == "Simulado")
{
    builder.Services.AddSingleton<CanalOrdenesSimulado.Registro>();
    builder.Services.AddScoped<IFabricaDispositivo, FabricaDispositivoSimulado>();
}
else
{
    builder.Services.AddScoped<IFabricaDispositivo, FabricaDispositivoReal>();
}

// Cada 1 s expira las órdenes que no se entregaron (30 s)
// o que no se confirmaron (60 s).
builder.Services.AddHostedService<ExpiradorOrdenes>();

// =====================================================
// CONSTRUIR APLICACIÓN
// =====================================================

var app = builder.Build();

// =====================================================
// DATOS INICIALES
// =====================================================

await DatosIniciales.InicializarAsync(app.Services);

// =====================================================
// IMPORTACIÓN DE DEMOSTRACIÓN
// =====================================================

if (app.Environment.IsDevelopment() &&
    args.Contains("--importar-demo-tecnico"))
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    var users = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    var tecnico = await users.FindByEmailAsync(
        "tecnico.palmas@sage.test")
        ?? throw new InvalidOperationException(
            "Falta tecnico.palmas@sage.test en el seed.");

    var empresaId = await db.UsuarioEmpresas
        .Where(ue => ue.UsuarioId == tecnico.Id)
        .OrderBy(ue => ue.EmpresaId)
        .Select(ue => (int?)ue.EmpresaId)
        .FirstOrDefaultAsync()
        ?? throw new InvalidOperationException(
            "El técnico de Las Palmas no tiene empresa.");

    var previas = db.Facturas
        .Where(f => f.EmpresaId == empresaId);

    db.Facturas.RemoveRange(previas);

    await db.SaveChangesAsync();

    var ruta = Path.GetFullPath(
        Path.Combine(
            app.Environment.ContentRootPath,
            "..",
            "..",
            "docs",
            "ejemplos",
            "facturas_validas.xlsx"));

    await using var stream = File.OpenRead(ruta);

    var resultado = await FacturaExcelImporter.ImportarAsync(
        stream,
        empresaId,
        tecnico.Id,
        db);

    if (resultado.Errores.Count > 0)
    {
        Console.Error.WriteLine("Importación falló:");

        foreach (var error in resultado.Errores)
        {
            Console.Error.WriteLine(" - " + error);
        }

        return;
    }

    Console.WriteLine(
        $"OK: técnico Las Palmas importó " +
        $"{resultado.Importadas} facturas " +
        "desde facturas_validas.xlsx");

    return;
}

// =====================================================
// MANEJO DE ERRORES
// =====================================================

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler(
        "/Error",
        createScopeForErrors: true);

    app.UseHsts();
}

// =====================================================
// PÁGINAS DE ERROR
// =====================================================

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    branch =>
    {
        branch.UseStatusCodePagesWithReExecute(
            "/not-found",
            createScopeForStatusCodePages: true);
    });

// =====================================================
// S23-M5: HTTP PARA EL ESP32
// =====================================================

// Durante la demostración, el ESP32 se comunica
// mediante HTTP dentro de la red local.
//
// Las demás rutas mantienen la redirección HTTPS.
app.UseWhen(
    context => !context.Request.Path
        .StartsWithSegments("/api/dispositivo"),
    branch =>
    {
        branch.UseHttpsRedirection();
    });

// =====================================================
// AUTENTICACIÓN Y AUTORIZACIÓN
// =====================================================

app.UseAuthentication();
app.UseAuthorization();

// =====================================================
// CAMBIO OBLIGATORIO DE CONTRASEÑA
// =====================================================

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var path = context.Request.Path.Value ?? "";

        var excluida =
            path.StartsWith(
                "/Account",
                StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(
                "/_framework",
                StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(
                "/_content",
                StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(
                "/_blazor",
                StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(
                "/api",
                StringComparison.OrdinalIgnoreCase) ||
            Path.HasExtension(path);

        if (!excluida)
        {
            var userManager = context.RequestServices
                .GetRequiredService<UserManager<ApplicationUser>>();

            var user = await userManager.GetUserAsync(
                context.User);

            if (user?.DebeCambiarContrasena == true)
            {
                context.Response.Redirect(
                    "/Account/CambiarContrasenaInicial");

                return;
            }
        }
    }

    await next();
});

// =====================================================
// ANTIFORGERY
// =====================================================

app.UseAntiforgery();

// =====================================================
// ARCHIVOS ESTÁTICOS
// =====================================================

app.MapStaticAssets();

// =====================================================
// COMPONENTES BLAZOR
// =====================================================

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// =====================================================
// ENDPOINTS
// =====================================================

app.MapAdditionalIdentityEndpoints();

app.MapTelemetriaEndpoints();

app.MapFacturasEndpoints();

// =====================================================
// INICIAR APLICACIÓN
// =====================================================

app.Run();
