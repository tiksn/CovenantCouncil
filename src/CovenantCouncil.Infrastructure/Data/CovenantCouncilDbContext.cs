using CovenantCouncil.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CovenantCouncil.Infrastructure.Data;

public sealed class CovenantCouncilDbContext(DbContextOptions<CovenantCouncilDbContext> options) : DbContext(options)
{
  public DbSet<PartyEntity> Parties => Set<PartyEntity>();

  public DbSet<CertificateEntity> Certificates => Set<CertificateEntity>();

  public DbSet<LicenseEntity> Licenses => Set<LicenseEntity>();

  public DbSet<ProtectionMetadataEntity> ProtectionMetadata => Set<ProtectionMetadataEntity>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<PartyEntity>(entity =>
    {
      entity.ToTable("parties");
      entity.HasKey(p => p.Id);
      entity.Property(p => p.Kind).HasConversion<string>();
    });

    modelBuilder.Entity<CertificateEntity>(entity =>
    {
      entity.ToTable("certificates");
      entity.HasKey(c => c.Id);
      entity.HasIndex(c => c.Thumbprint).IsUnique();
    });

    modelBuilder.Entity<LicenseEntity>(entity =>
    {
      entity.ToTable("licenses");
      entity.HasKey(l => l.Id);
      entity.HasOne(l => l.Party).WithMany().HasForeignKey(l => l.PartyId).OnDelete(DeleteBehavior.Restrict);
      entity.HasIndex(l => l.DescriptorDiscriminator);
      entity.HasIndex(l => l.SigningCertificateThumbprint);
    });

    modelBuilder.Entity<ProtectionMetadataEntity>(entity =>
    {
      entity.ToTable("protection_metadata");
      entity.HasKey(p => p.Id);
    });
  }
}
