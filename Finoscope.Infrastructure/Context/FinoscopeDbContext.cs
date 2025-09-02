using Finoscope.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finoscope.Infrastructure.Context;


public class FinoscopeDbContext : DbContext
{
    public FinoscopeDbContext(DbContextOptions<FinoscopeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Musteri> Musteriler { get; set; }
    public DbSet<Fatura> Faturalar { get; set; }


    /// <summary>
    /// Entity konfigurasyonları Fluent API .
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Musteri>(entity =>
        {
            entity.ToTable("MUSTERI_TANIM_TABLE");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Unvan).HasColumnName("UNVAN").HasMaxLength(255);
            entity.HasMany(e => e.Faturalar)
                  .WithOne(f => f.Musteri)
                  .HasForeignKey(f => f.MusteriId);
        });

        modelBuilder.Entity<Fatura>(entity =>
        {
            entity.ToTable("MUSTERI_FATURA_TABLE");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.MusteriId).HasColumnName("MUSTERI_ID");
            entity.Property(e => e.FaturaTarihi).HasColumnName("FATURA_TARIHI");
            entity.Property(e => e.FaturaTutari).HasColumnName("FATURA_TUTARI").HasColumnType("decimal(18,2)");
            entity.Property(e => e.OdemeTarihi).HasColumnName("ODEME_TARIHI");
        });
    }
}
