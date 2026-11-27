namespace SAGE.Web.Data;

public enum OrigenTarifaSimulacion
{
    Facturas,
    Configurada,
    Manual
}

public enum ModoPropuesta
{
    Kwp,
    KwhMes
}

public sealed record DatosEscenarioSolar(
    string Instaladora, decimal BsPorKwp, decimal? MantenimientoBsAnio,
    decimal ProduccionKwhKwpMes, decimal? CotizacionBs = null, decimal? MantenimientoBsKwpAnio = null);

public sealed record FacturaBaseSimulacion(int Anio, int Mes, decimal ConsumoKwh, decimal MontoBs);

public sealed record SugerenciaPresupuesto(
    decimal PresupuestoBs, string Instaladora, decimal PotenciaKwp, decimal InversionBs,
    decimal SobranteBs, decimal Cobertura, decimal EnergiaCubiertaKwhMes, decimal EnergiaRedKwhMes);

public sealed record DetalleSimulacion(
    OrigenTarifaSimulacion OrigenTarifa, decimal TarifaUsadaBsKwh, decimal TarifaConfiguradaBsKwh,
    decimal Cobertura, DatosEscenarioSolar Solar, DatosEscenarioSolar Propuesta,
    decimal? PresupuestoBs, decimal ExcedenteSolarKwhMes, decimal ExcedentePropuestaKwhMes,
    IReadOnlyList<FacturaBaseSimulacion> Facturas, IReadOnlyList<string> Pasos,
    decimal? PotenciaPropuestaKwp = null, ModoPropuesta ModoPropuesta = ModoPropuesta.Kwp,
    decimal? MetaKwhMes = null, SugerenciaPresupuesto? Sugerencia = null);
