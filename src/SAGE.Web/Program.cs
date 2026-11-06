using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using SAGE.Web.Components;
using SAGE.Web.Components.Account;
using SAGE.Web.Data;
using SAGE.Web.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

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

var app = builder.Build();

await DatosIniciales.InicializarAsync(app.Services);

// Uso: dotnet run --project src/SAGE.Web -- --importar-demo-tecnico
// Importa docs/ejemplos/facturas_validas.xlsx como técnico.palmas@sage.test (solo Development).
if (app.Environment.IsDevelopment() && args.Contains("--importar-demo-tecnico"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tecnico = await users.FindByEmailAsync("tecnico.palmas@sage.test")
        ?? throw new InvalidOperationException("Falta tecnico.palmas@sage.test en el seed.");
    if (tecnico.EmpresaId is null)
    {
        throw new InvalidOperationException("El técnico de Las Palmas no tiene empresa.");
    }

    // Limpia facturas previas del hotel para poder reimportar el ejemplo.
    var previas = db.Facturas.Where(f => f.EmpresaId == tecnico.EmpresaId);
    db.Facturas.RemoveRange(previas);
    await db.SaveChangesAsync();

    var ruta = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "docs", "ejemplos", "facturas_validas.xlsx"));
    await using var stream = File.OpenRead(ruta);
    var resultado = await FacturaExcelImporter.ImportarAsync(stream, tecnico.EmpresaId.Value, tecnico.Id, db);
    if (resultado.Errores.Count > 0)
    {
        Console.Error.WriteLine("Importación falló:");
        foreach (var e in resultado.Errores)
        {
            Console.Error.WriteLine(" - " + e);
        }

        return;
    }

    Console.WriteLine($"OK: técnico Las Palmas importó {resultado.Importadas} facturas desde facturas_validas.xlsx");
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    branch =>
    {
        branch.UseStatusCodePagesWithReExecute(
            "/not-found",
            createScopeForStatusCodePages: true);
    }); app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();
app.MapTelemetriaEndpoints();
app.MapFacturasEndpoints();

app.Run();
