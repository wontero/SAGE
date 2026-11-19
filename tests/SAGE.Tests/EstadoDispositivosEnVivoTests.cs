using Microsoft.Extensions.Logging.Abstractions;
using SAGE.Web.Servicios.EnVivo;
using Xunit;

namespace SAGE.Tests;

public class EstadoDispositivosEnVivoTests
{
    private static EstadoDispositivosEnVivo CrearServicio()
    {
        return new EstadoDispositivosEnVivo(
            NullLogger<EstadoDispositivosEnVivo>.Instance);
    }

    private static EstadoDispositivo CrearEstado(
        int dispositivoId = 1,
        decimal potenciaSolar = 967m)
    {
        return new EstadoDispositivo(
            DispositivoId: dispositivoId,
            EmpresaId: 3,
            TsServidor: DateTime.UtcNow,
            EnLinea: true,
            Modo: "AUTO",
            Estado: "EN_SOLAR",
            FuenteSeleccionada: "SOLAR",
            AlimentacionComprobada: true,
            VSol: 4.93m,
            ISolMa: 196.2m,
            PSolMw: potenciaSolar,
            WhSolAcum: 3.412m,
            VRed: 5.08m,
            IRedMa: 0.4m,
            PRedMw: 2m,
            WhRedAcum: 1.105m,
            FactorEscala: 1m
        );
    }

    [Fact]
    public void Actualizar_GuardaUltimoEstado()
    {
        var servicio = CrearServicio();
        var estado = CrearEstado();

        servicio.Actualizar(estado);

        var resultado = servicio.Obtener(1);

        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.DispositivoId);
        Assert.Equal(967m, resultado.PSolMw);
        Assert.True(resultado.EnLinea);
    }

    [Fact]
    public void Actualizar_ConservaEstadoMasReciente()
    {
        var servicio = CrearServicio();

        var reciente = CrearEstado() with
        {
            TsServidor = DateTime.UtcNow,
            PSolMw = 1500m
        };

        var antiguo = reciente with
        {
            TsServidor = reciente.TsServidor.AddSeconds(-10),
            PSolMw = 500m
        };

        servicio.Actualizar(reciente);
        servicio.Actualizar(antiguo);

        var resultado = servicio.Obtener(1);

        Assert.NotNull(resultado);
        Assert.Equal(1500m, resultado.PSolMw);
    }

    [Fact]
    public void ObservadorConError_NoImpideNotificarAlSiguiente()
    {
        var servicio = CrearServicio();
        var segundoRecibio = false;

        servicio.Cambio += _ =>
        {
            throw new InvalidOperationException(
                "Error simulado de un observador");
        };

        servicio.Cambio += _ =>
        {
            segundoRecibio = true;
        };

        servicio.Actualizar(CrearEstado());

        Assert.True(segundoRecibio);
    }

    [Fact]
    public void MarcarSinConexion_CambiaEstado()
    {
        var servicio = CrearServicio();

        servicio.Actualizar(CrearEstado());

        servicio.MarcarSinConexion(1);

        var resultado = servicio.Obtener(1);

        Assert.NotNull(resultado);
        Assert.False(resultado.EnLinea);
    }

    [Fact]
    public void MarcarSinConexion_NotificaCambio()
    {
        var servicio = CrearServicio();

        servicio.Actualizar(CrearEstado());

        EstadoDispositivo? recibido = null;

        servicio.Cambio += estado =>
        {
            recibido = estado;
        };

        servicio.MarcarSinConexion(1);

        Assert.NotNull(recibido);
        Assert.False(recibido.EnLinea);
    }

    [Fact]
    public void NotificarEventos_AvisaAlSuscriptor()
    {
        var servicio = CrearServicio();
        var dispositivoRecibido = 0;

        servicio.EventosRegistrados += id =>
        {
            dispositivoRecibido = id;
        };

        servicio.NotificarEventos(5);

        Assert.Equal(5, dispositivoRecibido);
    }

    [Fact]
    public void NotificarAlertas_AvisaEmpresaCorrecta()
    {
        var servicio = CrearServicio();
        var empresaRecibida = 0;

        servicio.AlertasCambiaron += id =>
        {
            empresaRecibida = id;
        };

        servicio.NotificarAlertas(3);

        Assert.Equal(3, empresaRecibida);
    }
}