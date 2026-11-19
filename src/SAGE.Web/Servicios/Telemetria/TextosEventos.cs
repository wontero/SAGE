namespace SAGE.Web.Servicios.Telemetria;

// Textos en español de los eventos del dispositivo (tabla de S23-M5).
// Agregado por Ethan (corrección a S23-M5) para /eventos y /monitoreo/{id}.
public static class TextosEventos
{
    public static string Motivo(string? motivo) => motivo switch
    {
        "intento_retorno_solar" => "Intento de retorno a solar",
        "cargador_sin_tension" => "Red sin tensión",
        "solar_comprobada" => "Solar comprobada",
        "prueba_solar_fallida" => "Prueba de solar fallida",
        "solar_insuficiente" => "Solar insuficiente",
        "sin_fuente_valida" => "Ninguna fuente válida",
        "cargador_recuperado" => "Red recuperada",
        "orden_modo" => "Cambio por orden",
        "orden_fallida" => "Orden fallida",
        "arranque" => "Arranque del dispositivo",
        null => "",
        _ => motivo
    };

    public static string Tipo(string? tipo) => tipo switch
    {
        "conmutacion" => "Conmutación",
        "estado" => "Cambio de estado",
        "alerta" => "Alerta",
        "reinicio" => "Reinicio",
        null => "",
        _ => tipo
    };

    public static string Estado(string? estado) => estado switch
    {
        "EN_CARGADOR" => "En red",
        "PROBANDO_SOLAR" => "Probando solar",
        "EN_SOLAR" => "En solar",
        "SIN_ALIMENTACION" => "Sin alimentación",
        null => "",
        _ => estado
    };

    public static string Origen(string? origen) => origen switch
    {
        "regla" => "Regla",
        "orden" => "Orden",
        "arranque" => "Arranque",
        null => "",
        _ => origen
    };

    // "Solar → Red", o "—" si el evento no movió el relé.
    public static string DeA(string? de, string? a)
    {
        if (de is null && a is null)
            return "—";

        return $"{Fuente(de)} → {Fuente(a)}";
    }

    private static string Fuente(string? fuente) =>
        fuente is null ? "—" : Formato.Fuente(fuente);
}
