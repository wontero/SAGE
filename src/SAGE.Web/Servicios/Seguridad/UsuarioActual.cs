using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SAGE.Web.Data;

namespace SAGE.Web.Servicios.Seguridad;

public sealed class UsuarioActual : IUsuarioActual
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _cargaLock = new(1, 1);

    private bool _cargado;
    private string? _usuarioId;
    private string _nombreCompleto = string.Empty;
    private bool _debeCambiarContrasena;
    private readonly HashSet<string> _roles = new(StringComparer.Ordinal);
    private List<EmpresaOpcion> _empresas = new();
    private int? _empresaActivaId;

    public UsuarioActual(
        AuthenticationStateProvider authStateProvider,
        IServiceScopeFactory scopeFactory)
    {
        _authStateProvider = authStateProvider;
        _scopeFactory = scopeFactory;
    }

    public string? UsuarioId => _usuarioId;
    public string NombreCompleto => _nombreCompleto;
    public bool EsOperador => EnRol(DatosIniciales.RolOperador);
    public bool EsAdministrador => EnRol(DatosIniciales.RolAdministrador);
    public bool EsTecnico => EnRol(DatosIniciales.RolTecnico);
    public bool EsConsulta => EnRol(DatosIniciales.RolConsulta);
    public bool DebeCambiarContrasena => _debeCambiarContrasena;
    public IReadOnlyList<EmpresaOpcion> Empresas => _empresas;
    public int? EmpresaActivaId => _empresaActivaId;
    public EmpresaOpcion? EmpresaActiva =>
        _empresaActivaId is int id
            ? _empresas.FirstOrDefault(e => e.Id == id)
            : null;

    public event Action? EmpresaCambiada;

    public bool EnRol(string rol) => _roles.Contains(rol);

    public bool PuedeVerEmpresa(int empresaId) =>
        PuedeVerEmpresa(_roles.Contains(DatosIniciales.RolOperador), _empresas, empresaId);

    public static bool PuedeVerEmpresa(
        bool esOperador,
        IReadOnlyList<EmpresaOpcion> empresas,
        int empresaId)
    {
        if (esOperador)
        {
            return false;
        }

        return empresas.Any(e => e.Id == empresaId);
    }

    public async Task CargarAsync()
    {
        if (_cargado)
        {
            return;
        }

        await _cargaLock.WaitAsync();
        try
        {
            if (_cargado)
            {
                return;
            }

            var auth = await _authStateProvider.GetAuthenticationStateAsync();
            var principal = auth.User;
            if (principal.Identity?.IsAuthenticated != true)
            {
                _cargado = true;
                return;
            }

            _usuarioId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (_usuarioId is null)
            {
                _cargado = true;
                return;
            }

            await using var scope = _scopeFactory.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = await userManager.FindByIdAsync(_usuarioId);
            if (user is null)
            {
                _cargado = true;
                return;
            }

            _nombreCompleto = user.NombreCompleto;
            _debeCambiarContrasena = user.DebeCambiarContrasena;
            _roles.Clear();
            foreach (var rol in await userManager.GetRolesAsync(user))
            {
                _roles.Add(rol);
            }

            _empresas = await db.UsuarioEmpresas.AsNoTracking()
                .Where(ue => ue.UsuarioId == user.Id && ue.Empresa.Activa)
                .OrderBy(ue => ue.Empresa.Nombre)
                .Select(ue => new EmpresaOpcion(ue.EmpresaId, ue.Empresa.Nombre))
                .ToListAsync();

            if (EsOperador)
            {
                _empresaActivaId = null;
            }
            else if (user.UltimaEmpresaId is int ultima && _empresas.Any(e => e.Id == ultima))
            {
                _empresaActivaId = ultima;
            }
            else
            {
                _empresaActivaId = _empresas.FirstOrDefault()?.Id;
            }

            _cargado = true;
        }
        finally
        {
            _cargaLock.Release();
        }
    }

    public async Task SeleccionarEmpresaAsync(int empresaId)
    {
        await CargarAsync();

        if (!PuedeVerEmpresa(empresaId))
        {
            throw new UnauthorizedAccessException("No puede seleccionar esa empresa.");
        }

        if (_empresaActivaId == empresaId)
        {
            return;
        }

        _empresaActivaId = empresaId;

        if (_usuarioId is not null)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(_usuarioId);
            if (user is not null)
            {
                user.UltimaEmpresaId = empresaId;
                await userManager.UpdateAsync(user);
            }
        }

        EmpresaCambiada?.Invoke();
    }
}
