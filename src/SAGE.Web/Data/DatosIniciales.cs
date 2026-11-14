using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SAGE.Web.Data;

public static class DatosIniciales
{
    public const string RolOperador = "Operador";
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
        foreach (var rol in new[] { RolOperador, RolAdministrador, RolTecnico, RolConsulta })
        {
            if (!await roleManager.RoleExistsAsync(rol))
            {
                await roleManager.CreateAsync(new IdentityRole(rol));
            }
        }

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        var energia = await AsegurarEmpresaAsync(
            db,
            nit: "1234567890",
            nombre: "Energía Andina S.R.L.",
            direccion: "Av. Principal 100",
            telefono: "70000000",
            correo: "contacto@energiaandina.test");

        var hotel = await AsegurarEmpresaAsync(
            db,
            nit: "2099887766",
            nombre: "Hotel Las Palmas",
            direccion: "Calle Comercio 50",
            telefono: "71112233",
            correo: "contacto@hotellaspalmas.test");

        await AsegurarUsuarioAsync(
            userManager, db,
            correo: "operador@sage.test",
            nombre: "Operador SAGE",
            rol: RolOperador,
            empresaIds: Array.Empty<int>());

        await AsegurarUsuarioAsync(
            userManager, db,
            correo: "admin@sage.test",
            nombre: "Administrador SAGE",
            rol: RolAdministrador,
            empresaIds: new[] { energia.Id, hotel.Id });

        await AsegurarUsuarioAsync(
            userManager, db,
            correo: "admin.palmas@sage.test",
            nombre: "Administrador Las Palmas",
            rol: RolAdministrador,
            empresaIds: new[] { hotel.Id });

        await AsegurarUsuarioAsync(
            userManager, db,
            correo: "tecnico@sage.test",
            nombre: "Técnico",
            rol: RolTecnico,
            empresaIds: new[] { energia.Id });

        await AsegurarUsuarioAsync(
            userManager, db,
            correo: "consulta@sage.test",
            nombre: "Consulta",
            rol: RolConsulta,
            empresaIds: new[] { energia.Id });

        await AsegurarUsuarioAsync(
            userManager, db,
            correo: "tecnico.palmas@sage.test",
            nombre: "Técnico Las Palmas",
            rol: RolTecnico,
            empresaIds: new[] { hotel.Id });

        if (!await db.ParametrosEconomicos.AnyAsync())
        {
            var operador = await userManager.FindByEmailAsync("operador@sage.test")
                ?? throw new InvalidOperationException("Falta operador@sage.test para el seed de parámetros.");

            db.ParametrosEconomicos.Add(new ParametrosEconomicos
            {
                Version = 1,
                TarifaBsKwh = 0.90m,
                CostoInstalacionBsKwp = 8000m,
                GeneracionKwhKwpMes = 125m,
                MantenimientoBsAnio = 600m,
                VigenteDesde = DateTime.UtcNow,
                CreadoPorId = operador.Id
            });
            await db.SaveChangesAsync();
        }
    }

    private static async Task<Empresa> AsegurarEmpresaAsync(
        ApplicationDbContext db,
        string nit,
        string nombre,
        string direccion,
        string telefono,
        string correo)
    {
        var empresa = await db.Empresas.FirstOrDefaultAsync(e => e.Nit == nit);
        if (empresa is null)
        {
            empresa = new Empresa
            {
                Nombre = nombre,
                Nit = nit,
                Direccion = direccion,
                Telefono = telefono,
                CorreoContacto = correo,
                Activa = true,
                FechaRegistro = DateTime.UtcNow
            };
            db.Empresas.Add(empresa);
            await db.SaveChangesAsync();
        }
        else
        {
            empresa.Nombre = nombre;
            empresa.Direccion = direccion;
            empresa.Telefono = telefono;
            empresa.CorreoContacto = correo;
            empresa.Activa = true;
            await db.SaveChangesAsync();
        }

        return empresa;
    }

    private static async Task AsegurarUsuarioAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        string correo,
        string nombre,
        string rol,
        IReadOnlyList<int> empresaIds)
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
                DebeCambiarContrasena = false
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
            user.DebeCambiarContrasena = false;
            await userManager.UpdateAsync(user);
        }

        var rolesActuales = await userManager.GetRolesAsync(user);
        foreach (var r in rolesActuales.Where(r => r != rol))
        {
            await userManager.RemoveFromRoleAsync(user, r);
        }

        if (!await userManager.IsInRoleAsync(user, rol))
        {
            await userManager.AddToRoleAsync(user, rol);
        }

        foreach (var empresaId in empresaIds)
        {
            var existe = await db.UsuarioEmpresas.AnyAsync(ue =>
                ue.UsuarioId == user.Id && ue.EmpresaId == empresaId);
            if (!existe)
            {
                db.UsuarioEmpresas.Add(new UsuarioEmpresa
                {
                    UsuarioId = user.Id,
                    EmpresaId = empresaId,
                    FechaVinculo = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
