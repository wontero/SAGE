using SAGE.Web.Data;
using Xunit;

namespace SAGE.Tests;

// S23-M9: transiciones de la entidad Orden (patrón Command, lado servidor).
public class OrdenTests
{
    private static readonly DateTime T0 = new(2026, 11, 20, 14, 0, 0, DateTimeKind.Utc);

    private static Orden NuevaOrden() => new()
    {
        DispositivoId = 1,
        UsuarioId = "u1",
        Tipo = Orden.TipoModo,
        ValorJson = "\"SOLAR\"",
        Creada = T0
    };

    [Fact]
    public void Nueva_orden_esta_pendiente()
    {
        var orden = NuevaOrden();

        Assert.Equal(Orden.EstadoPendiente, orden.Estado);
        Assert.Null(orden.Entregada);
        Assert.Null(orden.Resuelta);
    }

    [Fact]
    public void Entregar_desde_pendiente_pasa_a_entregada()
    {
        var orden = NuevaOrden();

        orden.Entregar(T0.AddSeconds(2));

        Assert.Equal(Orden.EstadoEntregada, orden.Estado);
        Assert.Equal(T0.AddSeconds(2), orden.Entregada);
    }

    [Fact]
    public void Entregar_dos_veces_lanza_excepcion()
    {
        var orden = NuevaOrden();
        orden.Entregar(T0);

        Assert.Throws<InvalidOperationException>(() => orden.Entregar(T0.AddSeconds(1)));
    }

    [Fact]
    public void Resolver_ejecutada_desde_entregada()
    {
        var orden = NuevaOrden();
        orden.Entregar(T0.AddSeconds(1));

        orden.Resolver("ejecutada", null, T0.AddSeconds(2));

        Assert.Equal(Orden.EstadoEjecutada, orden.Estado);
        Assert.Null(orden.Motivo);
        Assert.Equal(T0.AddSeconds(2), orden.Resuelta);
    }

    [Fact]
    public void Resolver_rechazada_guarda_motivo()
    {
        var orden = NuevaOrden();
        orden.Entregar(T0.AddSeconds(1));

        orden.Resolver("rechazada", "solar_no_disponible", T0.AddSeconds(2));

        Assert.Equal(Orden.EstadoRechazada, orden.Estado);
        Assert.Equal("solar_no_disponible", orden.Motivo);
    }

    [Fact]
    public void Resolver_desde_pendiente_se_ignora()
    {
        var orden = NuevaOrden();

        orden.Resolver("ejecutada", null, T0.AddSeconds(1));

        Assert.Equal(Orden.EstadoPendiente, orden.Estado);
        Assert.Null(orden.Resuelta);
    }

    [Fact]
    public void Resolver_una_orden_ya_resuelta_se_ignora()
    {
        var orden = NuevaOrden();
        orden.Entregar(T0);
        orden.Resolver("ejecutada", null, T0.AddSeconds(1));

        orden.Resolver("rechazada", "orden_desconocida", T0.AddSeconds(2));

        Assert.Equal(Orden.EstadoEjecutada, orden.Estado);
        Assert.Equal(T0.AddSeconds(1), orden.Resuelta);
    }

    [Fact]
    public void Resolver_con_resultado_desconocido_se_ignora()
    {
        var orden = NuevaOrden();
        orden.Entregar(T0);

        orden.Resolver("turbo", null, T0.AddSeconds(1));

        Assert.Equal(Orden.EstadoEntregada, orden.Estado);
    }

    [Fact]
    public void Expirar_pendiente_por_vencimiento_usa_no_entregada()
    {
        var orden = NuevaOrden();

        orden.ExpirarPorVencimiento(T0.AddSeconds(31));

        Assert.Equal(Orden.EstadoExpirada, orden.Estado);
        Assert.Equal("no_entregada", orden.Motivo);
    }

    [Fact]
    public void Expirar_entregada_por_vencimiento_usa_sin_confirmacion()
    {
        var orden = NuevaOrden();
        orden.Entregar(T0);

        orden.ExpirarPorVencimiento(T0.AddSeconds(61));

        Assert.Equal(Orden.EstadoExpirada, orden.Estado);
        Assert.Equal("sin_confirmacion", orden.Motivo);
    }

    [Fact]
    public void Expirar_con_motivo_reemplazada()
    {
        var orden = NuevaOrden();

        orden.Expirar(T0.AddSeconds(5), Orden.MotivoReemplazada);

        Assert.Equal(Orden.EstadoExpirada, orden.Estado);
        Assert.Equal("reemplazada", orden.Motivo);
    }

    [Fact]
    public void Expirar_una_orden_resuelta_lanza_excepcion()
    {
        var orden = NuevaOrden();
        orden.Entregar(T0);
        orden.Resolver("ejecutada", null, T0.AddSeconds(1));

        Assert.Throws<InvalidOperationException>(
            () => orden.Expirar(T0.AddSeconds(2), Orden.MotivoReemplazada));
    }

    [Fact]
    public void Expirada_no_se_puede_entregar_despues()
    {
        var orden = NuevaOrden();
        orden.ExpirarPorVencimiento(T0.AddSeconds(31));

        Assert.Throws<InvalidOperationException>(() => orden.Entregar(T0.AddSeconds(32)));
    }

    [Theory]
    [InlineData(30, false)]
    [InlineData(31, true)]
    public void Pendiente_vence_despues_de_30_segundos(int segundos, bool vencida)
    {
        var orden = NuevaOrden();

        Assert.Equal(vencida, orden.Vencida(T0.AddSeconds(segundos)));
    }

    [Theory]
    [InlineData(60, false)]
    [InlineData(61, true)]
    public void Entregada_vence_60_segundos_despues_de_entregarse(int segundos, bool vencida)
    {
        var orden = NuevaOrden();
        var entrega = T0.AddSeconds(20);
        orden.Entregar(entrega);

        // A los 30 s de creada, entregada, todavía no vence.
        Assert.False(orden.Vencida(T0.AddSeconds(31)));
        Assert.Equal(vencida, orden.Vencida(entrega.AddSeconds(segundos)));
    }

    [Fact]
    public void Resuelta_nunca_vence()
    {
        var orden = NuevaOrden();
        orden.Entregar(T0);
        orden.Resolver("ejecutada", null, T0.AddSeconds(1));

        Assert.False(orden.Vencida(T0.AddHours(1)));
    }
}
