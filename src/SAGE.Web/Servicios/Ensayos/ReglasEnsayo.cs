namespace SAGE.Web.Servicios.Ensayos;

// S23-M12: intervalo de un ensayo existente (Fin = null: en curso, sin final).
public sealed record IntervaloEnsayo(int Id, DateTime Inicio, DateTime? Fin);

// S23-M12: reglas de validación de los ensayos (todas las horas en UTC).
public static class ReglasEnsayo
{
    // Margen para que "ahora" escrito en el formulario no cuente como futuro por segundos.
    public static readonly TimeSpan ToleranciaFuturo = TimeSpan.FromMinutes(1);

    // Devuelve el mensaje de error en español, o null si el ensayo es válido.
    // idActual: el ensayo que se está editando/finalizando (se excluye de la superposición).
    public static string? Validar(
        DateTime inicio,
        DateTime? fin,
        DateTime ahora,
        IEnumerable<IntervaloEnsayo> otrosDelDispositivo,
        int? idActual = null)
    {
        if (inicio > ahora + ToleranciaFuturo)
            return "El inicio no puede ser una fecha futura.";

        if (fin is DateTime f && f <= inicio)
            return "El fin debe ser posterior al inicio.";

        var superpuesto = otrosDelDispositivo
            .Where(o => o.Id != idActual)
            .OrderBy(o => o.Inicio)
            .FirstOrDefault(o => SeSuperponen(inicio, fin, o.Inicio, o.Fin));

        if (superpuesto is not null)
            return $"Se superpone con el ensayo del {Formato.FechaCorta(superpuesto.Inicio)}";

        return null;
    }

    // Dos intervalos [a1, a2) y [b1, b2) se superponen si a1 < b2 y b1 < a2.
    // Un fin null es "sin final" (en curso).
    public static bool SeSuperponen(DateTime inicioA, DateTime? finA, DateTime inicioB, DateTime? finB)
    {
        var aAntesDeFinB = finB is null || inicioA < finB.Value;
        var bAntesDeFinA = finA is null || inicioB < finA.Value;

        return aAntesDeFinB && bAntesDeFinA;
    }
}
