using System.Text.Json;
using SAGE.Simulador;
using Xunit;

namespace SAGE.Tests;

// S23-M11: máquina de estados del simulador (patrón State) y órdenes (patrón Command),
// con la planta determinista (sin ruido) y reloj simulado.
public class MaquinaEstadosTests
{
    private static readonly DateTime T0 = new(2026, 11, 20, 16, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Paso = TimeSpan.FromMilliseconds(100);

    private sealed class Banco
    {
        public Planta Planta { get; } = new(determinista: true) { IrradianciaFija = 1.0 };
        public ControladorFuente C { get; } = new();
        public ProcesadorOrdenes Procesador { get; }
        public DateTime Ahora { get; private set; } = T0;
        public List<ResultadoOrden> Resultados { get; } = new();

        public Banco(string modo = ControladorFuente.ModoAuto)
        {
            Procesador = new ProcesadorOrdenes(C, rutaEstado: null);
            C.Arrancar(Ahora, modo, null);
        }

        public void Avanzar(double segundos)
        {
            var pasos = (int)Math.Round(segundos * 10);

            for (var i = 0; i < pasos; i++)
                UnPaso();
        }

        // Avanza hasta que se cumpla la condición; devuelve los segundos transcurridos.
        public double AvanzarHasta(Func<ControladorFuente, bool> condicion, double maximoS)
        {
            var inicio = Ahora;

            while ((Ahora - inicio).TotalSeconds < maximoS)
            {
                UnPaso();

                if (condicion(C))
                    return (Ahora - inicio).TotalSeconds;
            }

            throw new Xunit.Sdk.XunitException(
                $"No se cumplió la condición en {maximoS} s (estado {C.Estado.Nombre}).");
        }

        public void Orden(long id, string tipo, string valorJson)
        {
            using var doc = JsonDocument.Parse(
                $"{{\"id\":{id},\"tipo\":\"{tipo}\",\"valor\":{valorJson}}}");

            if (Procesador.Procesar(doc.RootElement) is ResultadoOrden r)
                Resultados.Add(r);
        }

        public EventoDispositivo UltimoEvento => C.Eventos[^1];

        private void UnPaso()
        {
            Ahora += Paso;
            C.Paso(Planta.Medir(C.ReleEnSolar, Ahora, Paso.TotalSeconds), Ahora);
        }
    }

    // Arranca en Automático con sol pleno y espera a quedar EN_SOLAR.
    private static Banco EnSolarConSol()
    {
        var b = new Banco();
        b.AvanzarHasta(c => c.Estado == EstadosFuente.EnSolar, 60);
        return b;
    }

    [Fact]
    public void Arranque_en_red_con_evento_reinicio()
    {
        var b = new Banco();

        Assert.Equal("EN_CARGADOR", b.C.Estado.Nombre);
        Assert.False(b.C.ReleEnSolar);

        var e = Assert.Single(b.C.Eventos);
        Assert.Equal(("reinicio", null, "CARGADOR", "arranque", "arranque"), (e.Tipo, e.De, e.A, e.Motivo, e.Origen));
    }

    [Fact]
    public void Sombra_en_EN_SOLAR_pasa_a_red_en_2_segundos_o_menos()
    {
        var b = EnSolarConSol();
        b.Avanzar(5);

        b.Planta.Sombreado = true;
        var segundos = b.AvanzarHasta(c => c.Estado == EstadosFuente.EnCargador, 5);

        Assert.True(segundos <= 2.0, $"Tardó {segundos} s (RF12 pide ≤ 2 s).");

        var e = b.UltimoEvento;
        Assert.Equal(("conmutacion", "SOLAR", "CARGADOR", "EN_CARGADOR", "solar_insuficiente", "regla"),
            (e.Tipo, e.De, e.A, e.Estado, e.Motivo, e.Origen));
    }

    [Fact]
    public void Sol_despues_de_sombra_prueba_y_vuelve_a_solar()
    {
        var b = EnSolarConSol();
        b.Planta.Sombreado = true;
        b.AvanzarHasta(c => c.Estado == EstadosFuente.EnCargador, 5);

        b.Planta.Sombreado = false;
        var hastaPrueba = b.AvanzarHasta(c => c.Estado == EstadosFuente.ProbandoSolar, 60);

        // T_estable = 30 s (más 1 s de muestras tras mover el relé).
        Assert.InRange(hastaPrueba, 30, 33);
        Assert.Equal("intento_retorno_solar", b.UltimoEvento.Motivo);
        Assert.Equal("conmutacion", b.UltimoEvento.Tipo);

        b.AvanzarHasta(c => c.Estado == EstadosFuente.EnSolar, 10);

        var e = b.UltimoEvento;
        Assert.Equal(("estado", null, null, "EN_SOLAR", "solar_comprobada"),
            (e.Tipo, e.De, e.A, e.Estado, e.Motivo));
    }

    [Fact]
    public void Sol_falso_falla_la_prueba_duplica_la_espera_y_no_alterna_sin_fin()
    {
        var b = new Banco();
        // Irradiancia 0,1: en vacío 5 V (parece que hay sol), con carga 3,8 V.
        b.Planta.IrradianciaFija = 0.1;

        b.Avanzar(10 * 60);

        var intentos = b.C.Eventos.Where(e => e.Motivo == "intento_retorno_solar").ToList();
        var fallos = b.C.Eventos.Where(e => e.Motivo == "prueba_solar_fallida").ToList();

        Assert.NotEmpty(fallos);
        Assert.Equal(intentos.Count, fallos.Count);
        Assert.DoesNotContain(b.C.Eventos, e => e.Estado == "EN_SOLAR");

        // Cada intento llega después de la espera vigente: 30, 60, 120, 300 s...
        for (var i = 1; i < intentos.Count; i++)
        {
            var esperaS = ControladorFuente.EsperasS[Math.Min(i, ControladorFuente.EsperasS.Length - 1)];
            var desdeFallo = (intentos[i].Ts - fallos[i - 1].Ts).TotalSeconds;

            Assert.True(desdeFallo >= esperaS,
                $"Intento {i + 1} a los {desdeFallo} s del fallo anterior (espera {esperaS} s).");
        }

        // En 10 min: intentos a ~30 s, ~90 s, ~210 s y ~510 s; nunca más de uno por espera.
        Assert.InRange(intentos.Count, 3, 4);
        Assert.True(b.C.EsperaActualS >= 120);
    }

    [Fact]
    public void Espera_se_duplica_en_cada_prueba_fallida()
    {
        var b = new Banco();
        b.Planta.IrradianciaFija = 0.1;

        Assert.Equal(30, b.C.EsperaActualS);

        b.AvanzarHasta(c => c.Eventos.Any(e => e.Motivo == "prueba_solar_fallida"), 60);
        Assert.Equal(60, b.C.EsperaActualS);

        b.AvanzarHasta(c => c.Eventos.Count(e => e.Motivo == "prueba_solar_fallida") == 2, 120);
        Assert.Equal(120, b.C.EsperaActualS);
    }

    [Fact]
    public void Orden_SOLAR_con_panel_sombreado_se_rechaza()
    {
        var b = new Banco();
        b.Planta.Sombreado = true;
        b.Avanzar(5);

        b.Orden(10, "modo", "\"SOLAR\"");

        var r = Assert.Single(b.Resultados);
        Assert.Equal(("rechazada", "solar_no_disponible"), (r.Resultado, r.Motivo));
        Assert.Equal("EN_CARGADOR", b.C.Estado.Nombre);
        Assert.Equal(ControladorFuente.ModoAuto, b.C.Modo);
    }

    [Fact]
    public void Orden_repetida_se_ejecuta_una_sola_vez()
    {
        var b = new Banco();
        b.Avanzar(5);

        b.Orden(20, "modo", "\"CARGADOR\"");
        var eventosTrasPrimera = b.C.Eventos.Count;

        b.Orden(20, "modo", "\"CARGADOR\"");
        b.Orden(19, "modo", "\"AUTO\"");   // más vieja: tampoco se ejecuta

        var r = Assert.Single(b.Resultados);
        Assert.Equal(20, r.OrdenId);
        Assert.Equal("ejecutada", r.Resultado);
        Assert.Equal(ControladorFuente.ModoCargador, b.C.Modo);
        Assert.Equal(eventosTrasPrimera, b.C.Eventos.Count);
    }

    [Fact]
    public void Orden_SOLAR_con_sol_prueba_y_si_falla_informa_orden_fallida()
    {
        var b = new Banco();
        b.Avanzar(5);

        b.Orden(30, "modo", "\"SOLAR\"");

        Assert.Equal(("ejecutada", null), (b.Resultados[0].Resultado, b.Resultados[0].Motivo));
        Assert.Equal("PROBANDO_SOLAR", b.C.Estado.Nombre);
        Assert.Equal(("conmutacion", "orden_modo", "orden", (long?)30),
            (b.UltimoEvento.Tipo, b.UltimoEvento.Motivo, b.UltimoEvento.Origen, b.UltimoEvento.OrdenId));

        // El sol no alcanza con carga: vuelve a red y queda "Solar en respaldo".
        b.Planta.IrradianciaFija = 0.1;
        b.AvanzarHasta(c => c.Estado == EstadosFuente.EnCargador, 5);

        Assert.Equal(("orden_fallida", "orden", (long?)30),
            (b.UltimoEvento.Motivo, b.UltimoEvento.Origen, b.UltimoEvento.OrdenId));

        // No reintenta en modo Solar aunque vuelva el sol.
        b.Planta.IrradianciaFija = 1.0;
        b.Avanzar(120);
        Assert.Equal("EN_CARGADOR", b.C.Estado.Nombre);
        Assert.Equal(ControladorFuente.ModoSolar, b.C.Modo);
    }

    [Fact]
    public void Modo_red_con_red_caida_alerta_una_vez_y_no_cambia_de_fuente()
    {
        var b = new Banco();
        b.Avanzar(5);
        b.Orden(40, "modo", "\"CARGADOR\"");

        b.Planta.RedCortada = true;
        b.Avanzar(30);

        var alertas = b.C.Eventos.Where(e => e.Tipo == "alerta").ToList();
        var alerta = Assert.Single(alertas);
        Assert.Equal("cargador_sin_tension", alerta.Motivo);
        Assert.False(b.C.ReleEnSolar);
    }

    [Fact]
    public void Ambas_ramas_caidas_pasa_a_SIN_ALIMENTACION_y_se_recupera()
    {
        var b = new Banco();
        b.Planta.NocheForzada = true;
        b.Avanzar(5);

        b.Planta.RedCortada = true;
        b.AvanzarHasta(c => c.Estado == EstadosFuente.SinAlimentacion, 5);
        Assert.Equal(("estado", "sin_fuente_valida"), (b.UltimoEvento.Tipo, b.UltimoEvento.Motivo));

        b.Planta.RedCortada = false;
        b.AvanzarHasta(c => c.Estado == EstadosFuente.EnCargador, 5);
        Assert.Equal(("estado", "cargador_recuperado"), (b.UltimoEvento.Tipo, b.UltimoEvento.Motivo));
    }

    [Fact]
    public void Orden_de_parametros_valida_se_aplica_e_invalida_se_rechaza()
    {
        var b = new Banco();

        b.Orden(50, "parametros",
            "{\"version\":4,\"v_min\":4.5,\"v_retorno\":4.85,\"i_min_ma\":20,\"t_falla_s\":1.0,\"t_estable_s\":15,\"t_prueba_s\":5}");
        b.Orden(51, "parametros",
            "{\"version\":5,\"v_min\":5.5,\"v_retorno\":4.85,\"i_min_ma\":20,\"t_falla_s\":1.0,\"t_estable_s\":15,\"t_prueba_s\":5}");
        b.Orden(52, "turbo", "\"x\"");

        Assert.Equal("ejecutada", b.Resultados[0].Resultado);
        Assert.Equal(("rechazada", "parametros_invalidos"), (b.Resultados[1].Resultado, b.Resultados[1].Motivo));
        Assert.Equal(("rechazada", "orden_desconocida"), (b.Resultados[2].Resultado, b.Resultados[2].Motivo));
        Assert.Equal(4, b.C.ParametrosVersion);
        Assert.Equal(15m, b.C.Parametros.TEstableS);
    }

    [Fact]
    public void Estado_se_guarda_y_el_reinicio_incrementa_boot_id()
    {
        var ruta = Path.Combine(Path.GetTempPath(), $"sage_sim_{Guid.NewGuid():N}.json");

        try
        {
            var c1 = new ControladorFuente();
            var p1 = new ProcesadorOrdenes(c1, ruta);
            var arranque1 = p1.Arrancar();
            c1.Arrancar(T0, arranque1.Modo, arranque1.OrdenModoId);

            using (var doc = JsonDocument.Parse("{\"id\":7,\"tipo\":\"modo\",\"valor\":\"CARGADOR\"}"))
                p1.Procesar(doc.RootElement);

            var c2 = new ControladorFuente();
            var p2 = new ProcesadorOrdenes(c2, ruta);
            var arranque2 = p2.Arrancar();

            Assert.True(p1.BootId > 1_000_000_000, "Un estado nuevo no debe reutilizar boot_id bajos de corridas anteriores.");
            Assert.Equal(p1.BootId + 1, p2.BootId);
            Assert.Equal(7, p2.UltimaEjecutada);
            Assert.Equal(ControladorFuente.ModoCargador, arranque2.Modo);
        }
        finally
        {
            File.Delete(ruta);
        }
    }
}
