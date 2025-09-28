using KeyCard.Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace KeyCard.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> opts) : DbContext(opts)
{
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Booking>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ConfirmationCode).HasMaxLength(32).IsRequired();
            e.Property(x => x.GuestLastName).HasMaxLength(128).IsRequired();
            e.Property(x => x.Status).HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.ConfirmationCode).IsUnique();
        });
    }
}
