using KeyCard.Infrastructure.Identity;
using KeyCard.Infrastructure.Models.Bookings;
using KeyCard.Infrastructure.Models.Entities;
using KeyCard.Infrastructure.Models.HouseKeeping;
using KeyCard.Infrastructure.Models.Users;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KeyCard.Infrastructure.Models.AppDbContext
{
    public class ApplicationDBContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
            : base(options) { }

        // Core Hotel Entities
        public DbSet<Hotel> Hotels { get; set; } = default!;
        public DbSet<RoomType> RoomTypes { get; set; } = default!;
        public DbSet<Room> Rooms { get; set; } = default!;

        // Users
        public DbSet<GuestProfile> GuestProfiles { get; set; } = default!;
        public DbSet<StaffAccount> StaffAccounts { get; set; } = default!;

        // Bookings & Related
        public DbSet<Booking> Bookings { get; set; } = default!;
        public DbSet<Payment> Payments { get; set; } = default!;
        public DbSet<Invoice> Invoices { get; set; } = default!;
        public DbSet<DigitalKey> DigitalKeys { get; set; } = default!;

        // Housekeeping
        public DbSet<HousekeepingTask> HousekeepingTasks { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relationships
            modelBuilder.Entity<Room>()
                .HasOne(r => r.RoomType)
                .WithMany()                         // no Rooms collection on RoomType
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict); // optional, avoids cascading delete of rooms

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.GuestProfile)
                .WithMany(g => g.Bookings)
                .HasForeignKey(b => b.GuestProfileId);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Room)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Booking)
                .WithOne(b => b.Invoice)
                .HasForeignKey<Invoice>(i => i.BookingId);

            modelBuilder.Entity<DigitalKey>()
                .HasOne(dk => dk.Booking)
                .WithOne(b => b.DigitalKey)
                .HasForeignKey<DigitalKey>(dk => dk.BookingId);

            modelBuilder.Entity<HousekeepingTask>()
                .HasOne(ht => ht.Room)
                .WithMany()
                .HasForeignKey(ht => ht.RoomId);

            modelBuilder.Entity<HousekeepingTask>()
                .HasOne(ht => ht.AssignedTo)
                .WithMany()
                .HasForeignKey(ht => ht.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
