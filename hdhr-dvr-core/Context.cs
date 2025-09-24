namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Core;

using Microsoft.EntityFrameworkCore;

public class Context : DbContext {
    public DbSet<Series> Series { get; set; }
    public DbSet<Episode> Episodes { get; set; }

    public required string DbPath { get; init; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlite($"Data Source={DbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        _ = modelBuilder.Entity<Series>().OwnsOne(s => s.Metadata);
        _ = modelBuilder.Entity<Episode>().OwnsOne(s => s.Metadata);
    }
}
