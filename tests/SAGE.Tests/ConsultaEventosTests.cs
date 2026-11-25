using Microsoft.EntityFrameworkCore;
using SAGE.Web.Data;
using SAGE.Web.Servicios.Telemetria;

namespace SAGE.Tests;

public class ConsultaEventosTests
{
    [Fact]
    public void FiltraEmpresaDispositivoTipoYFechasAntesDePaginar()
    {
        var fecha = new DateTime(2026, 11, 20, 4, 0, 0, DateTimeKind.Utc);
        var dispositivo = new Dispositivo { Id = 1, EmpresaId = 1 };
        var otro = new Dispositivo { Id = 2, EmpresaId = 2 };
        var eventos = Enumerable.Range(1, 120).Select(i => new Evento
        {
            Id = i, DispositivoId = 1, Dispositivo = dispositivo, Tipo = "estado",
            TsValida = true, Ts = fecha.AddMinutes(i)
        }).Concat(new[] {
            new Evento { Id = 121, DispositivoId = 2, Dispositivo = otro, Tipo = "estado", TsValida = true, Ts = fecha },
            new Evento { Id = 122, DispositivoId = 1, Dispositivo = dispositivo, Tipo = "alerta", TsValida = true, Ts = fecha }
        }).AsQueryable();
        var filtrados = ConsultaEventos.Filtrar(eventos, 1, 1, "estado", fecha.AddMinutes(11), fecha.AddMinutes(112));
        Assert.Equal(101, filtrados.Count());
        var segunda = ConsultaEventos.Pagina(filtrados, 1).ToList();
        Assert.Equal(50, segunda.Count);
        Assert.Equal(61, segunda[0].Id);
        Assert.Equal(12, segunda[^1].Id);
        Assert.Single(ConsultaEventos.Pagina(filtrados, 2));

        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost;Database=prueba;Integrated Security=true;TrustServerCertificate=true").Options;
        using var db = new ApplicationDbContext(opciones);
        var sql = ConsultaEventos.Pagina(ConsultaEventos.Filtrar(db.Eventos, 1, 1, "estado", fecha, fecha.AddDays(1)), 1).ToQueryString();
        Assert.Contains("WHERE", sql);
        Assert.Contains("OFFSET", sql);
        Assert.Contains("FETCH", sql);
    }
}
