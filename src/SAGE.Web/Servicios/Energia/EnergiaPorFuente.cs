namespace SAGE.Web.Servicios.Energia;

public sealed record EnergiaPorFuente(
    decimal WhSolar, decimal WhRed, TimeSpan TiempoSolar, TimeSpan TiempoRed, int Conmutaciones)
{
    public decimal TotalWh => WhSolar + WhRed;
    public decimal PorcentajeSolar => TotalWh == 0 ? 0 : WhSolar / TotalWh * 100;
}
