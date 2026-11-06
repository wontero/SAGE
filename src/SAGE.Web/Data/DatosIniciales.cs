using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SAGE.Web.Data;

public static class DatosIniciales
{
    public const string RolAdministrador = "Administrador";
    public const string RolTecnico = "Tecnico";
    public const string RolConsulta = "Consulta";
    public const string PasswordPrueba = "Sage2026!";

    public static async Task InicializarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var env = sp.GetRequiredService<IHostEnvironment>();

        if (!env.IsDevelopment())
        {
            return;
        }

        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var rol in new[] { RolAdministrador, RolTecnico, RolConsulta })
        {
            if (!await roleManager.RoleExistsAsync(rol))
            {
                await roleManager.CreateAsync(new IdentityRole(rol));
            }
        }

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        await AsegurarUsuarioAsync(
            userManager,
            correo: "admin@sage.test",
            nombre: "Administrador SAGE",
            rol: RolAdministrador,
            empresaId: null);

        var empresa = await db.Empresas.FirstOrDefaultAsync(e => e.Nit == "1234567890");
        if (empresa is null)
        {
            empresa = new Empresa
            {
                Nombre = "Energía Andina S.R.L.",
                Nit = "1234567890",
                Direccion = "Av. Principal 100",
                Telefono = "70000000",
                CorreoContacto = "contacto@energiaandina.test",
                Activa = true,
                FechaRegistro = DateTime.UtcNow
            };
            db.Empresas.Add(empresa);
            await db.SaveChangesAsync();
        }
        else
        {
            empresa.Nombre = "Energía Andina S.R.L.";
            empresa.CorreoContacto = "contacto@energiaandina.test";
            await db.SaveChangesAsync();
        }

        await AsegurarUsuarioAsync(
            userManager,
            correo: "tecnico@sage.test",
            nombre: "Técnico",
            rol: RolTecnico,
            empresaId: empresa.Id);

        await AsegurarUsuarioAsync(
            userManager,
            correo: "consulta@sage.test",
            nombre: "Consulta",
            rol: RolConsulta,
            empresaId: empresa.Id);

        // Empresa del guion de demo (sección 7) para pruebas de Técnico / facturas.
        var hotel = await db.Empresas.FirstOrDefaultAsync(e => e.Nit == "2099887766");
        if (hotel is null)
        {
            hotel = new Empresa
            {
                Nombre = "Hotel Las Palmas",
                Nit = "2099887766",
                Direccion = "Calle Comercio 50",
                Telefono = "71112233",
                CorreoContacto = "contacto@hotellaspalmas.test",
                Activa = true,
                FechaRegistro = DateTime.UtcNow
            };
            db.Empresas.Add(hotel);
            await db.SaveChangesAsync();
        }

        await AsegurarUsuarioAsync(
            userManager,
            correo: "tecnico.palmas@sage.test",
            nombre: "Técnico Las Palmas",
            rol: RolTecnico,
            empresaId: hotel.Id);
    }

    private static async Task AsegurarUsuarioAsync(
        UserManager<ApplicationUser> userManager,
        string correo,
        string nombre,
        string rol,
        int? empresaId)
    {
        var user = await userManager.FindByEmailAsync(correo);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = correo,
                Email = correo,
                EmailConfirmed = true,
                NombreCompleto = nombre,
                Activo = true,
                EmpresaId = empresaId
            };

            var result = await userManager.CreateAsync(user, PasswordPrueba);
            if (!result.Succeeded)
            {
                var errores = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"No se pudo crear el usuario {correo}: {errores}");
            }
        }
        else
        {
            user.NombreCompleto = nombre;
            user.Activo = true;
            user.EmailConfirmed = true;
            user.EmpresaId = empresaId;
            await userManager.UpdateAsync(user);
        }

        if (!await userManager.IsInRoleAsync(user, rol))
        {
            await userManager.AddToRoleAsync(user, rol);
        }
    }
}
