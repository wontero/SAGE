using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SAGE.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<Empresa> Empresas => Set<Empresa>();

    public DbSet<Dispositivo> Dispositivos => Set<Dispositivo>();

    public DbSet<Medicion> Mediciones => Set<Medicion>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Empresa>(entity =>
        {
            entity.HasIndex(e => e.Nombre).IsUnique();
            entity.HasIndex(e => e.Nit).IsUnique();
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(u => u.Empresa)
                .WithMany(e => e.Usuarios)
                .HasForeignKey(u => u.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Dispositivo>(entity =>
        {
            entity.HasIndex(d => d.TokenHash).IsUnique();

            entity.HasOne(d => d.Empresa)
                .WithMany()
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

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
    }
}