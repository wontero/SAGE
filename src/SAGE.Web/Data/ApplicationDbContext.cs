using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SAGE.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<Empresa> Empresas => Set<Empresa>();

    public DbSet<UsuarioEmpresa> UsuarioEmpresas => Set<UsuarioEmpresa>();

    public DbSet<Dispositivo> Dispositivos => Set<Dispositivo>();

    public DbSet<Medicion> Mediciones => Set<Medicion>();

    // S23-M5: Registro de eventos de dispositivos
    public DbSet<Evento> Eventos => Set<Evento>();

    // S23-M7: Alertas automáticas
    public DbSet<Alerta> Alertas => Set<Alerta>();

    public DbSet<Factura> Facturas => Set<Factura>();

    public DbSet<ParametrosEconomicos> ParametrosEconomicos
        => Set<ParametrosEconomicos>();

    public DbSet<TarifaEmpresa> TarifasEmpresa
        => Set<TarifaEmpresa>();

    public DbSet<Simulacion> Simulaciones
        => Set<Simulacion>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // =====================================================
        // EMPRESAS
        // =====================================================

        builder.Entity<Empresa>(entity =>
        {
            entity.HasIndex(e => e.Nombre).IsUnique();
            entity.HasIndex(e => e.Nit).IsUnique();
        });

        // =====================================================
        // USUARIOS Y EMPRESAS
        // =====================================================

        builder.Entity<UsuarioEmpresa>(entity =>
        {
            entity.HasKey(ue => new
            {
                ue.UsuarioId,
                ue.EmpresaId
            });

            entity.HasOne(ue => ue.Usuario)
                .WithMany(u => u.Empresas)
                .HasForeignKey(ue => ue.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ue => ue.Empresa)
                .WithMany(e => e.Usuarios)
                .HasForeignKey(ue => ue.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =====================================================
        // DISPOSITIVOS
        // =====================================================

        builder.Entity<Dispositivo>(entity =>
        {
            entity.HasIndex(d => d.TokenHash).IsUnique();

            entity.HasOne(d => d.Empresa)
                .WithMany()
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            // S23-M6: Factor de escala de la maqueta.
            // Solo multiplica potencia y energía en pantalla.
            entity.Property(d => d.FactorEscala)
                .HasPrecision(10, 2)
                .HasDefaultValue(1m);
        });

        // =====================================================
        // MEDICIONES
        // =====================================================

        builder.Entity<Medicion>(entity =>
        {
            entity.HasIndex(m => new
            {
                m.DispositivoId,
                m.BootId,
                m.Seq
            }).IsUnique();

            entity.HasOne(m => m.Dispositivo)
                .WithMany()
                .HasForeignKey(m => m.DispositivoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =====================================================
        // S23-M5: EVENTOS DE DISPOSITIVOS
        // =====================================================

        builder.Entity<Evento>(entity =>
        {
            entity.HasIndex(e => new
            {
                e.DispositivoId,
                e.BootId,
                e.Seq
            }).IsUnique();

            entity.HasOne(e => e.Dispositivo)
                .WithMany()
                .HasForeignKey(e => e.DispositivoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =====================================================
        // S23-M7: ALERTAS AUTOMÁTICAS
        // =====================================================

        builder.Entity<Alerta>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Tipo)
                .IsRequired()
                .HasMaxLength(50);

            // Campos nuevos de la guía oficial.
            entity.Property(a => a.Inicio)
                .HasColumnType("datetime2");

            entity.Property(a => a.Fin)
                .HasColumnType("datetime2");

            // Relación opcional con el evento
            // que originó la alerta.
            entity.HasOne(a => a.Evento)
                .WithMany()
                .HasForeignKey(a => a.EventoId)
                .OnDelete(DeleteBehavior.SetNull);

            // Compatibilidad temporal con la
            // implementación anterior.
            entity.Property(a => a.Mensaje)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(a => a.FechaCreacion)
                .IsRequired();

            // Una sola alerta activa por tipo
            // para cada dispositivo.
            entity.HasIndex(a => new
            {
                a.DispositivoId,
                a.Tipo
            })
            .IsUnique()
            .HasFilter("[Activa] = 1");

            entity.HasOne(a => a.Dispositivo)
                .WithMany()
                .HasForeignKey(a => a.DispositivoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =====================================================
        // FACTURAS
        // =====================================================

        builder.Entity<Factura>(entity =>
        {
            entity.HasIndex(f => new
            {
                f.EmpresaId,
                f.Anio,
                f.Mes
            }).IsUnique();

            entity.Property(f => f.Origen)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasOne(f => f.Empresa)
                .WithMany()
                .HasForeignKey(f => f.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.RegistradaPor)
                .WithMany()
                .HasForeignKey(f => f.RegistradaPorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =====================================================
        // PARÁMETROS ECONÓMICOS
        // =====================================================

        builder.Entity<ParametrosEconomicos>(entity =>
        {
            entity.HasIndex(p => p.Version).IsUnique();

            entity.Property(p => p.TarifaBsKwh)
                .HasPrecision(18, 4);

            entity.Property(p => p.CostoInstalacionBsKwp)
                .HasPrecision(18, 4);

            entity.Property(p => p.GeneracionKwhKwpMes)
                .HasPrecision(18, 4);

            entity.Property(p => p.MantenimientoBsAnio)
                .HasPrecision(18, 4);

            entity.HasOne(p => p.CreadoPor)
                .WithMany()
                .HasForeignKey(p => p.CreadoPorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =====================================================
        // TARIFAS POR EMPRESA
        // =====================================================

        builder.Entity<TarifaEmpresa>(entity =>
        {
            entity.Property(t => t.TarifaBsKwh)
                .HasPrecision(18, 4);

            entity.HasOne(t => t.Empresa)
                .WithMany()
                .HasForeignKey(t => t.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.CreadoPor)
                .WithMany()
                .HasForeignKey(t => t.CreadoPorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =====================================================
        // SIMULACIONES
        // =====================================================

        builder.Entity<Simulacion>(entity =>
        {
            entity.Property(s => s.TarifaUsadaBsKwh)
                .HasPrecision(18, 4);

            entity.Property(s => s.ConsumoMensualKwh)
                .HasPrecision(18, 4);

            entity.Property(s => s.KwpSolar)
                .HasPrecision(18, 4);

            entity.Property(s => s.KwpMixto)
                .HasPrecision(18, 4);

            entity.Property(s => s.InversionSolarBs)
                .HasPrecision(18, 4);

            entity.Property(s => s.KwpMixto)
                .HasPrecision(18, 4);

            entity.Property(s => s.InversionMixtoBs)
                .HasPrecision(18, 4);

            entity.Property(s => s.CostoAnualRedBs)
                .HasPrecision(18, 4);

            entity.Property(s => s.CostoAnualSolarBs)
                .HasPrecision(18, 4);

            entity.Property(s => s.CostoAnualMixtoBs)
                .HasPrecision(18, 4);

            entity.Property(s => s.CostoAcumRedBs)
                .HasPrecision(18, 4);

            entity.Property(s => s.CostoAcumSolarBs)
                .HasPrecision(18, 4);

            entity.Property(s => s.CostoAcumMixtoBs)
                .HasPrecision(18, 4);

            entity.Property(s => s.RecuperacionSolarAnios)
                .HasPrecision(18, 4);

            entity.Property(s => s.RecuperacionMixtoAnios)
                .HasPrecision(18, 4);

            entity.Property(s => s.Advertencias)
                .HasConversion(
                    v => string.Join('\n', v),
                    v => string.IsNullOrEmpty(v)
                        ? new List<string>()
                        : v.Split(
                            '\n',
                            StringSplitOptions.RemoveEmptyEntries
                        ).ToList()
                );

            entity.HasOne(s => s.Empresa)
                .WithMany()
                .HasForeignKey(s => s.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}