using Microsoft.EntityFrameworkCore;
using SAGE.Web.Data;

namespace SAGE.Web.Servicios.Energia;

internal static class GeneradorHistorial
{
    internal static async Task<long> GenerarAsync(ApplicationDbContext db, int dispositivoId, int dias)
    {
        if (!await db.Dispositivos.AnyAsync(d => d.Id == dispositivoId))
            throw new ArgumentException("El dispositivo no existe.");

        var ahora = DateTime.UtcNow;
        var hastaUtc = ahora.AddTicks(-ahora.Ticks % TimeSpan.TicksPerSecond);
        var desdeUtc = hastaUtc.AddDays(-dias);
        var bootId = -ahora.Ticks;

        await using var transaccion = await db.Database.BeginTransactionAsync();
        var total = 0L;
        for (var dia = 0; dia < dias; dia++)
        {
            var inicio = desdeUtc.AddDays(dia);
            var primerSeq = (long)dia * 86400;
            var segundosSolarPrevios = (long)dia * 9 * 3600;
            total += await db.Database.ExecuteSqlInterpolatedAsync($"""
                WITH Digitos AS (SELECT n FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) d(n)),
                Segundos AS (
                    SELECT a.n + 10*b.n + 100*c.n + 1000*d.n + 10000*e.n AS Segundo
                    FROM Digitos a CROSS JOIN Digitos b CROSS JOIN Digitos c CROSS JOIN Digitos d CROSS JOIN Digitos e
                ), Instantes AS (
                    SELECT Segundo, DATEADD(second, Segundo, {inicio}) AS Ts FROM Segundos WHERE Segundo < 86400
                ), Lecturas AS (
                    SELECT *, CASE WHEN DATEPART(hour, DATEADD(hour, -4, Ts)) BETWEEN 8 AND 16 THEN 1 ELSE 0 END AS Solar
                    FROM Instantes
                ), Acumulados AS (
                    SELECT *, SUM(Solar) OVER (ORDER BY Segundo ROWS UNBOUNDED PRECEDING) AS SegundosSolar
                    FROM Lecturas
                )
                INSERT INTO Mediciones (DispositivoId, BootId, Seq, Ts, TsServidor, TsValida,
                    VSol, ISolMa, PSolMw, WhSolAcum, VRed, IRedMa, PRedMw, WhRedAcum,
                    Modo, Estado, FuenteSeleccionada, AlimentacionComprobada)
                SELECT {dispositivoId}, {bootId}, {primerSeq} + Segundo, Ts, Ts, 1,
                    CASE WHEN Solar = 1 THEN 4.9 ELSE 0.3 END, Solar * 190, Solar * 931,
                    ({segundosSolarPrevios} + SegundosSolar) * 0.931 / 3600,
                    5.05, (1 - Solar) * 190, (1 - Solar) * 959.5,
                    ({primerSeq} + Segundo + 1 - {segundosSolarPrevios} - SegundosSolar) * 0.9595 / 3600,
                    N'AUTO', CASE WHEN Solar = 1 THEN N'EN_SOLAR' ELSE N'EN_CARGADOR' END,
                    CASE WHEN Solar = 1 THEN N'SOLAR' ELSE N'CARGADOR' END, 1
                FROM Acumulados
                """);
        }

        await transaccion.CommitAsync();
        return total;
    }
}
