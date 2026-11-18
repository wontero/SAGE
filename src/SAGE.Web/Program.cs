
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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<IUsuarioActual, UsuarioActual>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
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

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddMudServices();

// =====================================================
// S23-M5: PATRONES DE DISEÑO ADAPTER Y FACADE
// =====================================================

// Adapter: transforma los datos del ESP32
// en entidades del sistema SAGE.
builder.Services.AddScoped<
    IAdaptadorTelemetria,
    AdaptadorTelemetriaEsp32>();

// Facade: coordina la sincronización del dispositivo,
// las mediciones y el registro de eventos.
builder.Services.AddScoped<FachadaSincronizacion>();

// =====================================================

var app = builder.Build();

await DatosIniciales.InicializarAsync(app.Services);

if (app.Environment.IsDevelopment() && args.Contains("--importar-demo-tecnico"))
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

        foreach (var e in resultado.Errores)
        {
            Console.Error.WriteLine(" - " + e);
        }

        return;
    }

    Console.WriteLine(
        $"OK: técnico Las Palmas importó " +
        $"{resultado.Importadas} facturas " +
        "desde facturas_validas.xlsx");

    return;
}

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

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    branch =>
    {
        branch.UseStatusCodePagesWithReExecute(
            "/not-found",
            createScopeForStatusCodePages: true);
    });

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

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

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.MapTelemetriaEndpoints();

app.MapFacturasEndpoints();

app.Run();
