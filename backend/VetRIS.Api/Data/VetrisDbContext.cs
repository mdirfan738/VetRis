using Microsoft.EntityFrameworkCore;
using VetRIS.Api.Models;

namespace VetRIS.Api.Data;

public class VetrisDbContext : DbContext
{
    public VetrisDbContext(DbContextOptions<VetrisDbContext> options) : base(options) { }

    public DbSet<Study> Studies => Set<Study>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Study>(entity =>
        {
            entity.ToTable("Studies");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.PatientId).IsRequired().HasMaxLength(128);
            entity.Property(s => s.S3Key).IsRequired().HasMaxLength(512);
            entity.Property(s => s.Status).IsRequired().HasMaxLength(64);
        });
    }
}
