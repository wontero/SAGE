
using SAGE.Web.Data;

namespace SAGE.Web.Servicios.EnVivo;

// S23-M6: Estado de un dispositivo en tiempo real.
public sealed record EstadoDispositivo(
    int DispositivoId,
    int EmpresaId,
    DateTime TsServidor,
    bool EnLinea,
    string Modo,
    string Estado,
    string FuenteSeleccionada,
    bool AlimentacionComprobada,
    decimal VSol,
    decimal ISolMa,
    decimal PSolMw,
    decimal WhSolAcum,
    decimal VRed,
    decimal IRedMa,
    decimal PRedMw,
    decimal WhRedAcum,
    decimal FactorEscala)
{
    // Convertir una medición en el estado
    // que recibirán las pantallas de SAGE.
    public static EstadoDispositivo Desde(
        Medicion m,
        int empresaId,
        decimal factorEscala)
    {
        return new EstadoDispositivo(
            DispositivoId: m.DispositivoId,
            EmpresaId: empresaId,
            TsServidor: m.TsServidor,
            EnLinea: true,
            Modo: m.Modo,
            Estado: m.Estado,
            FuenteSeleccionada: m.FuenteSeleccionada,
            AlimentacionComprobada: m.AlimentacionComprobada,
            VSol: m.VSol,
            ISolMa: m.ISolMa,
            PSolMw: m.PSolMw,
            WhSolAcum: m.WhSolAcum,
            VRed: m.VRed,
            IRedMa: m.IRedMa,
            PRedMw: m.PRedMw,
            WhRedAcum: m.WhRedAcum,
            FactorEscala: factorEscala
        );
    }
}
