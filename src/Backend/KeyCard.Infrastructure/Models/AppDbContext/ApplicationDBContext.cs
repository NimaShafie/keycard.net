using KeyCard.Infrastructure.Identity;
using KeyCard.Infrastructure.Models.Bookings;
using KeyCard.Infrastructure.Models.Entities;
using KeyCard.Infrastructure.Models.HouseKeeping;
using KeyCard.Infrastructure.Models.Users;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KeyCard.Infrastructure.Models.AppDbContext
{
    public class ApplicationDBContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options) { }

        public DbSet<Hotel> Hotels => Set<Hotel>();
        public DbSet<RoomType> RoomTypes => Set<RoomType>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<GuestProfile> GuestProfiles => Set<GuestProfile>();
        public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<HousekeepingTask> HousekeepingTasks => Set<HousekeepingTask>();
        public DbSet<DigitalKey> DigitalKeys => Set<DigitalKey>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Hotel → RoomType (no cascade)
            builder.Entity<RoomType>()
                .HasOne(rt => rt.Hotel)
                .WithMany(h => h.RoomTypes)
                .HasForeignKey(rt => rt.HotelId)
                .OnDelete(DeleteBehavior.NoAction);  // 👈 use NoAction (not Cascade)

            // Hotel → Room (no cascade)
            builder.Entity<Room>()
                .HasOne(r => r.Hotel)
                .WithMany(h => h.Rooms)
                .HasForeignKey(r => r.HotelId)
                .OnDelete(DeleteBehavior.NoAction);  // 👈 prevents multiple paths

            // RoomType → Room (no cascade)
            builder.Entity<Room>()
                .HasOne(r => r.RoomType)
                .WithMany()
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.NoAction);  // 👈 final link—no cascade

            // Other relationships
            builder.Entity<Booking>()
                .HasOne(b => b.Room)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Booking>()
                .HasOne(b => b.GuestProfile)
                .WithMany(g => g.Bookings)
                .HasForeignKey(b => b.GuestProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Invoice>()
                .HasOne(i => i.Booking)
                .WithOne(b => b.Invoice)
                .HasForeignKey<Invoice>(i => i.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DigitalKey>()
                .HasOne(dk => dk.Booking)
                .WithOne(b => b.DigitalKey)
                .HasForeignKey<DigitalKey>(dk => dk.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<HousekeepingTask>()
                .HasOne(ht => ht.AssignedTo)
                .WithMany()
                .HasForeignKey(ht => ht.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);
        }

    }
}
