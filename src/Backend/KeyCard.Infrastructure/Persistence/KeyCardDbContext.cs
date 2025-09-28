using KeyCard.Domain.Rooms;
using Microsoft.EntityFrameworkCore;

namespace KeyCard.Infrastructure.Persistence;

/// <summary>EF Core DbContext lives in Infrastructure. Domain remains framework-free.</summary>
public class KeyCardDbContext : DbContext
{
    public DbSet<Room> Rooms => Set<Room>();

    public KeyCardDbContext(DbContextOptions<KeyCardDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Number).IsRequired().HasMaxLength(16);
            b.Property(x => x.Type).IsRequired().HasMaxLength(64);
            b.Property(x => x.State).HasConversion<string>();
        });
    }
}
