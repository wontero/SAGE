
namespace SAGE.Web.Servicios.Configuracion;

public sealed record ParametrosFuente(
    decimal VMin,
    decimal VRetorno,
    decimal IMinMa,
    decimal TFallaS,
    decimal TEstableS,
    decimal TPruebaS)
{
    public static ParametrosFuente Predeterminados =>
        new(
            4.50m,
            4.85m,
            20m,
            1.0m,
            30m,
            5m
        );
}
