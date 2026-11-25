using SAGE.Web.Data;

namespace SAGE.Web.Servicios.Telemetria;

public static class ConsultaEventos
{
    public static IQueryable<Evento> Filtrar(IQueryable<Evento> eventos, int empresaId, int? dispositivoId,
        string? tipo, DateTime? desde, DateTime? hasta)
    {
        var consulta = eventos.Where(e => e.Dispositivo.EmpresaId == empresaId);
        if (dispositivoId is not null) consulta = consulta.Where(e => e.DispositivoId == dispositivoId);
        if (!string.IsNullOrEmpty(tipo)) consulta = consulta.Where(e => e.Tipo == tipo);
        if (desde is not null) consulta = consulta.Where(e => (e.TsValida ? e.Ts : e.TsServidor) >= desde);
        if (hasta is not null) consulta = consulta.Where(e => (e.TsValida ? e.Ts : e.TsServidor) < hasta);
        return consulta;
    }

    public static IQueryable<Evento> Pagina(IQueryable<Evento> eventos, int pagina) => eventos
        .OrderByDescending(e => e.TsValida ? e.Ts : e.TsServidor).ThenByDescending(e => e.Id)
        .Skip(pagina * 50).Take(50);
}
