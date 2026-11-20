using Microsoft.EntityFrameworkCore;

namespace SAGE.Web.Servicios.Dispositivo;

internal sealed record PuntoPotenciaHistorial(
    DateTime Ts, [property: Precision(38, 6)] decimal PSolMw, [property: Precision(38, 6)] decimal PRedMw)
{
    internal const int MaximoPuntos = 1500;
}
