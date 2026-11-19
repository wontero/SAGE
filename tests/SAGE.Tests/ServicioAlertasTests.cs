using Microsoft.EntityFrameworkCore;
using SAGE.Web.Data;
using SAGE.Web.Servicios.Alertas;

namespace SAGE.Tests;

public class ServicioAlertasTests
{
    private static ApplicationDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(opciones);
    }

    private static Dispositivo CrearDispositivo()
    {
        return new Dispositivo
        {
            Id = 1
        };
    }

    private static Evento CrearEvento(string motivo, long seq = 1)
    {
        return new Evento
        {
            DispositivoId = 1,
            BootId = 1,
            Seq = seq,
            Motivo = motivo
        };
    }

    [Theory]
    [InlineData("solar_insuficiente")]
    [InlineData("sin_fuente_valida")]
    [InlineData("cargador_sin_tension")]
    public async Task AbrirAlerta_CreaAlertaActiva(string tipo)
    {
        using var db = CrearContexto();
        var servicio = new ServicioAlertas(db);
        var dispositivo = CrearDispositivo();

        await servicio.EvaluarAsync(
            dispositivo,
            null,
            new[] { CrearEvento(tipo) });

        await db.SaveChangesAsync();

        var alerta = await db.Alertas.SingleAsync();

        Assert.Equal(tipo, alerta.Tipo);
        Assert.True(alerta.Activa);
        Assert.Null(alerta.Fin);
        Assert.NotEqual(default, alerta.Inicio);
    }

    [Fact]
    public async Task SolarInsuficiente_SeCierraConSolarComprobada()
    {
        using var db = CrearContexto();
        var servicio = new ServicioAlertas(db);
        var dispositivo = CrearDispositivo();

        await servicio.EvaluarAsync(
            dispositivo,
            null,
            new[] { CrearEvento("solar_insuficiente", 1) });

        await db.SaveChangesAsync();

        await servicio.EvaluarAsync(
            dispositivo,
            null,
            new[] { CrearEvento("solar_comprobada", 2) });

        await db.SaveChangesAsync();

        var alerta = await db.Alertas.SingleAsync();

        Assert.False(alerta.Activa);
        Assert.NotNull(alerta.Fin);
    }

    [Fact]
    public async Task SinFuenteValida_SeCierraAlRecuperarAlimentacion()
    {
        using var db = CrearContexto();
        var servicio = new ServicioAlertas(db);
        var dispositivo = CrearDispositivo();

        await servicio.EvaluarAsync(
            dispositivo,
            null,
            new[] { CrearEvento("sin_fuente_valida") });

        await db.SaveChangesAsync();

        var medicion = new Medicion
        {
            Estado = "EN_CARGADOR",
            VRed = 5.05m
        };

        await servicio.EvaluarAsync(
            dispositivo,
            medicion,
            Array.Empty<Evento>());

        await db.SaveChangesAsync();

        var alerta = await db.Alertas.SingleAsync();

        Assert.False(alerta.Activa);
        Assert.NotNull(alerta.Fin);
    }

    [Fact]
    public async Task CargadorSinTension_SeCierraAlRecuperarVoltaje()
    {
        using var db = CrearContexto();
        var servicio = new ServicioAlertas(db);
        var dispositivo = CrearDispositivo();

        await servicio.EvaluarAsync(
            dispositivo,
            null,
            new[] { CrearEvento("cargador_sin_tension") });

        await db.SaveChangesAsync();

        var medicion = new Medicion
        {
            Estado = "EN_CARGADOR",
            VRed = 5.05m
        };

        await servicio.EvaluarAsync(
            dispositivo,
            medicion,
            Array.Empty<Evento>());

        await db.SaveChangesAsync();

        var alerta = await db.Alertas.SingleAsync();

        Assert.False(alerta.Activa);
        Assert.NotNull(alerta.Fin);
    }

    [Fact]
    public async Task SinConexion_SeCierraConNuevaMedicion()
    {
        using var db = CrearContexto();
        var dispositivo = CrearDispositivo();

        db.Alertas.Add(new Alerta
        {
            DispositivoId = dispositivo.Id,
            Tipo = "sin_conexion",
            Mensaje = "Sin conexión.",
            Inicio = DateTime.UtcNow,
            FechaCreacion = DateTime.UtcNow,
            Activa = true
        });

        await db.SaveChangesAsync();

        var servicio = new ServicioAlertas(db);

        await servicio.EvaluarAsync(
            dispositivo,
            new Medicion
            {
                Estado = "EN_SOLAR",
                VRed = 5.05m
            },
            Array.Empty<Evento>());

        await db.SaveChangesAsync();

        var alerta = await db.Alertas.SingleAsync();

        Assert.False(alerta.Activa);
        Assert.NotNull(alerta.Fin);
    }

    [Theory]
    [InlineData("solar_insuficiente")]
    [InlineData("sin_fuente_valida")]
    [InlineData("cargador_sin_tension")]
    public async Task MismaAlerta_NoSeAbreDosVeces(string tipo)
    {
        using var db = CrearContexto();
        var servicio = new ServicioAlertas(db);
        var dispositivo = CrearDispositivo();

        await servicio.EvaluarAsync(
            dispositivo,
            null,
            new[] { CrearEvento(tipo, 1) });

        await db.SaveChangesAsync();

        await servicio.EvaluarAsync(
            dispositivo,
            null,
            new[] { CrearEvento(tipo, 2) });

        await db.SaveChangesAsync();

        var cantidad = await db.Alertas.CountAsync(
            a => a.Tipo == tipo && a.Activa);

        Assert.Equal(1, cantidad);
    }
}