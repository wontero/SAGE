using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SAGE.Web.Data;
using SAGE.Web.Servicios.Configuracion;
using SAGE.Web.Servicios.Dispositivo;
using SAGE.Web.Servicios.EnVivo;
using SAGE.Web.Servicios.Ordenes;
using SAGE.Web.Servicios.Seguridad;

namespace SAGE.Tests;

public class ProxyTests
{
    [Theory]
    [InlineData(DatosIniciales.RolConsulta, 1, "Su rol no permite cambiar el modo de fuente.")]
    [InlineData(DatosIniciales.RolTecnico, 2, "El dispositivo no pertenece a su empresa activa.")]
    [InlineData(DatosIniciales.RolAdministrador, 2, "El dispositivo no pertenece a su empresa activa.")]
    public async Task OrdenDenegadaNoDelegaNiGuarda(string rol, int empresaActiva, string mensaje)
    {
        await using var servicios = await CrearServiciosAsync();
        var canal = new CanalEspia();
        var proxy = new CanalOrdenesProtegido(canal, new Usuario(rol, empresaActiva),
            servicios.GetRequiredService<IServiceScopeFactory>());

        var error = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => proxy.EnviarModoAsync(1, "AUTO"));

        Assert.Equal(mensaje, error.Message);
        Assert.Null(canal.Ultima);
        using var scope = servicios.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Ordenes.ToListAsync());
    }

    [Fact]
    public async Task TecnicoDeEmpresaActivaDelega()
    {
        await using var servicios = await CrearServiciosAsync();
        var canal = new CanalEspia();
        var proxy = new CanalOrdenesProtegido(canal, new Usuario(DatosIniciales.RolTecnico, 1),
            servicios.GetRequiredService<IServiceScopeFactory>());

        var orden = await proxy.EnviarModoAsync(1, "AUTO");

        Assert.Same(canal.Ultima, orden);
        Assert.Equal(1, orden.DispositivoId);
    }

    [Fact]
    public async Task CanalBdSoloConsultaYCreaOrdenesDeLaEmpresaActiva()
    {
        await using var servicios = await CrearServiciosAsync();
        var scopes = servicios.GetRequiredService<IServiceScopeFactory>();
        var enVivo = new EstadoDispositivosEnVivo(NullLogger<EstadoDispositivosEnVivo>.Instance);
        var andina = new CanalOrdenesBD(scopes, new Usuario(DatosIniciales.RolAdministrador, 1), enVivo);
        var palmas = new CanalOrdenesBD(scopes, new Usuario(DatosIniciales.RolAdministrador, 2), enVivo);

        var orden = await andina.EnviarModoAsync(1, "AUTO");
        Assert.Equal(orden.Id, Assert.Single(await andina.UltimasAsync(1, 10)).Id);
        Assert.Empty(await palmas.UltimasAsync(1, 10));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => palmas.EnviarModoAsync(1, "CARGADOR"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => palmas.EnviarParametrosAsync(1, ParametrosFuente.Predeterminados));
        Assert.Equal(Orden.EstadoPendiente, Assert.Single(await andina.UltimasAsync(1, 10)).Estado);
    }

    [Theory]
    [InlineData(AccionUsuario.RestablecerContrasena)]
    [InlineData(AccionUsuario.Editar)]
    [InlineData(AccionUsuario.CambiarEstado)]
    public async Task GestionRequiereVinculoDelUsuarioConLaEmpresaActiva(AccionUsuario accion)
    {
        await using var servicios = await CrearServiciosAsync();
        using var scope = servicios.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.UsuarioEmpresas.Add(new UsuarioEmpresa { UsuarioId = "empleado", EmpresaId = 1 });
        await db.SaveChangesAsync();

        string[] roles = [DatosIniciales.RolTecnico];
        var andina = new Usuario(DatosIniciales.RolAdministrador, 1);
        var palmas = new Usuario(DatosIniciales.RolAdministrador, 2);
        Assert.Null(await ReglasUsuarios.ErrorDeGestionAsync(db, andina, "empleado", roles, accion));
        Assert.Equal("El usuario no pertenece a su empresa activa.",
            await ReglasUsuarios.ErrorDeGestionAsync(db, palmas, "empleado", roles, accion));
    }

    [Theory]
    [InlineData(AccionUsuario.RestablecerContrasena)]
    [InlineData(AccionUsuario.CambiarEstado)]
    public async Task OperadorSoloGestionaCuentasDeAdministradores(AccionUsuario accion)
    {
        await using var servicios = await CrearServiciosAsync();
        using var scope = servicios.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var operador = new Usuario(DatosIniciales.RolOperador, 1);

        Assert.Null(await ReglasUsuarios.ErrorDeGestionAsync(db, operador, "admin", [DatosIniciales.RolAdministrador], accion));
        Assert.NotNull(await ReglasUsuarios.ErrorDeGestionAsync(db, operador, "tecnico", [DatosIniciales.RolTecnico], accion));
    }

    [Fact]
    public async Task FabricaSimuladaUsaLecturasSinteticasYResuelveLaOrdenSinGuardarlaEnSql()
    {
        await using var servicios = await CrearServiciosAsync();
        var fabrica = new FabricaDispositivoSimulado(servicios.GetRequiredService<IServiceScopeFactory>(),
            new Usuario(DatosIniciales.RolTecnico, 1),
            new EstadoDispositivosEnVivo(NullLogger<EstadoDispositivosEnVivo>.Instance),
            new CanalOrdenesSimulado.Registro());
        var hasta = DateTime.UtcNow;
        var lector = fabrica.CrearLector();

        var lecturas = await lector.MedicionesAsync(1, hasta.AddMinutes(-1), hasta, 2);
        Assert.Equal(2, lecturas.Count);
        Assert.Equal(TimeSpan.FromSeconds(1), lecturas[0].Ts - lecturas[1].Ts);
        Assert.Empty(await lector.EventosAsync(1, hasta.AddMinutes(-1), hasta));
        var orden = await fabrica.CrearCanalOrdenes().EnviarModoAsync(1, "AUTO");
        Assert.Equal(Orden.EstadoEjecutada, orden.Estado);
        Assert.Equal(orden.Creada, orden.Resuelta);
        Assert.Same(orden, Assert.Single(await fabrica.CrearCanalOrdenes().UltimasAsync(1, 10)));

        using var scope = servicios.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.Ordenes.ToListAsync());
        Assert.Empty(await db.Mediciones.ToListAsync());
    }

    [Fact]
    public async Task LecturaPermiteConsultaDeEmpresaActivaYDeniegaOperador()
    {
        await using var servicios = await CrearServiciosAsync();
        var scopeFactory = servicios.GetRequiredService<IServiceScopeFactory>();
        var lector = new LectorEspia();
        var consulta = new LectorTelemetriaProtegido(lector, new Usuario(DatosIniciales.RolConsulta, 1), scopeFactory);
        var operador = new LectorTelemetriaProtegido(lector, new Usuario(DatosIniciales.RolOperador, 1), scopeFactory);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => operador.MedicionesAsync(1, DateTime.MinValue, DateTime.MaxValue, 1));
        Assert.Equal(0, lector.Llamadas);

        await consulta.MedicionesAsync(1, DateTime.MinValue, DateTime.MaxValue, 1);
        Assert.Equal(1, lector.Llamadas);
    }

    private static async Task<ServiceProvider> CrearServiciosAsync()
    {
        var nombre = Guid.NewGuid().ToString();
        var servicios = new ServiceCollection()
            .AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(nombre))
            .BuildServiceProvider();
        using var scope = servicios.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Users.Add(new ApplicationUser { Id = "prueba", UserName = "prueba" });
        db.Dispositivos.Add(new Dispositivo { Id = 1, EmpresaId = 1, Nombre = "Maqueta", TokenHash = "prueba" });
        await db.SaveChangesAsync();
        return servicios;
    }

    private sealed class Usuario(string rol, int empresaActiva) : IUsuarioActual
    {
        public string UsuarioId => "prueba";
        public string NombreCompleto => "Usuario de prueba";
        public bool EsOperador => EnRol(DatosIniciales.RolOperador);
        public bool EsAdministrador => EnRol(DatosIniciales.RolAdministrador);
        public bool EsTecnico => EnRol(DatosIniciales.RolTecnico);
        public bool EsConsulta => EnRol(DatosIniciales.RolConsulta);
        public bool DebeCambiarContrasena => false;
        public IReadOnlyList<EmpresaOpcion> Empresas => EsAdministrador ? [new(1, "Andina"), new(2, "Palmas")] : [EmpresaActiva];
        public int? EmpresaActivaId => empresaActiva;
        public EmpresaOpcion EmpresaActiva => new(empresaActiva, "Empresa activa");
        public bool EnRol(string valor) => rol == valor;
        public bool PuedeVerEmpresa(int id) => !EsOperador && Empresas.Any(e => e.Id == id);
        public Task CargarAsync() => Task.CompletedTask;
        public Task SeleccionarEmpresaAsync(int id) => throw new NotSupportedException();
        public event Action? EmpresaCambiada { add { } remove { } }
    }

    private sealed class CanalEspia : ICanalOrdenes
    {
        public Orden? Ultima { get; private set; }

        public Task<Orden> EnviarModoAsync(int dispositivoId, string modo) =>
            Task.FromResult(Ultima = new Orden { DispositivoId = dispositivoId, ValorJson = modo });

        public Task<Orden> EnviarParametrosAsync(int dispositivoId, ParametrosFuente p) => throw new NotSupportedException();
        public Task<IReadOnlyList<Orden>> UltimasAsync(int dispositivoId, int cantidad) => throw new NotSupportedException();
    }

    private sealed class LectorEspia : ILectorTelemetria
    {
        public int Llamadas { get; private set; }

        public Task<IReadOnlyList<Medicion>> MedicionesAsync(int id, DateTime desde, DateTime hasta, int maximo)
        {
            Llamadas++;
            return Task.FromResult<IReadOnlyList<Medicion>>([]);
        }

        public Task<IReadOnlyList<Evento>> EventosAsync(int id, DateTime desde, DateTime hasta) => throw new NotSupportedException();
    }
}
