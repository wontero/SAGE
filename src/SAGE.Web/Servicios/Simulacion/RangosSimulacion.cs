namespace SAGE.Web.Servicios.Simulacion;

public static class RangosSimulacion
{
    public static string? Potencia(decimal? valor) => valor is >= 0.1m and <= 5_000m
        ? null : "Ingresá una potencia entre 0,1 y 5.000 kWp.";

    public static string? MetaKwh(decimal? valor) => valor is >= 1m and <= 1_000_000m
        ? null : "Ingresá una meta entre 1 y 1.000.000 kWh/mes.";

    public static string? MantenimientoPorKwp(decimal? valor) => valor is >= 0m and <= 1_000m
        ? null : "Ingresá un mantenimiento por kWp entre 0 y 1.000 Bs al año.";

    public static string? Precio(decimal? valor) => valor is >= 2000m and <= 30000m
        ? null : "Ingresá un precio entre 2.000 y 30.000 Bs/kWp.";

    public static string? Mantenimiento(decimal? valor) => valor is >= 0m
        ? null : "Ingresá un mantenimiento de 0 Bs/año o más.";

    public static string? Produccion(decimal? valor) => valor is >= 50m and <= 200m
        ? null : "Ingresá una producción entre 50 y 200 kWh/kWp/mes.";

    public static string? Cobertura(decimal? valor) => valor is >= 50m and <= 300m
        ? null : "Ingresá una cobertura entre 50 y 300 %.";

    public static string? Inversion(decimal? valor) => valor is > 0m
        ? null : "Ingresá una inversión mayor que 0 Bs.";

    public static string? Presupuesto(decimal? valor) => valor is > 0m
        ? null : "Ingresá un presupuesto mayor que 0 Bs.";

    public static string? Tarifa(decimal? valor) => valor is >= 0.10m and <= 5.00m
        ? null : "La tarifa debe estar entre 0,1 y 5 Bs/kWh.";

    public static string? Horizonte(int? valor) => valor is >= 1 and <= 25
        ? null : "Ingresá un horizonte entre 1 y 25 años.";
}
