using System.Reflection.Emit;

using KeyCard.Infrastructure.Models.Bookings;
using KeyCard.Infrastructure.Models.Entities;
using KeyCard.Infrastructure.Models.HouseKeeping;
using KeyCard.Infrastructure.Models.User;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KeyCard.Infrastructure.Models.AppDbContext
{
    public class ApplicationDBContext
    : IdentityDbContext<ApplicationUser, ApplicationUserRole, int>
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options) { }

        public DbSet<Hotel> Hotels => Set<Hotel>();
        public DbSet<RoomType> RoomTypes => Set<RoomType>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<Amenity> Amenities => Set<Amenity>();
        public DbSet<RoomTypeAmenity> RoomTypeAmenities => Set<RoomTypeAmenity>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<HousekeepingTask> HousekeepingTasks => Set<HousekeepingTask>();
        public DbSet<DigitalKey> DigitalKeys => Set<DigitalKey>();
        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
            builder.Properties<decimal>().HavePrecision(18, 2);
            builder.Properties<decimal?>().HavePrecision(18, 2);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Hotel → RoomType (no cascade)
            builder.Entity<RoomType>()
                .HasOne(rt => rt.Hotel)
                .WithMany(h => h.RoomTypes)
                .HasForeignKey(rt => rt.HotelId)
                .OnDelete(DeleteBehavior.NoAction);  // use NoAction (not Cascade)

            // Hotel → Room (no cascade)
            builder.Entity<Room>()
                .HasOne(r => r.Hotel)
                .WithMany(h => h.Rooms)
                .HasForeignKey(r => r.HotelId)
                .OnDelete(DeleteBehavior.NoAction);  // prevents multiple paths

            // RoomType → Room (no cascade)
            builder.Entity<Room>()
                .HasOne(r => r.RoomType)
                .WithMany()
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.NoAction);  // final link—no cascade

            // booking relationships
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

            builder.Entity<Invoice>(b =>
            {
                b.HasIndex(i => i.InvoiceNumber).IsUnique();
            });

            builder.Entity<DigitalKey>()
                .HasOne(dk => dk.Booking)
                .WithOne(b => b.DigitalKey)
                .HasForeignKey<DigitalKey>(dk => dk.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // housekeeping relationships
            builder.Entity<HousekeepingTask>()
                .HasOne(ht => ht.AssignedTo)
                .WithMany()
                .HasForeignKey(ht => ht.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<HousekeepingTask>()
                .HasOne(t => t.Room)
                .WithMany()
                .HasForeignKey(t => t.RoomId)
                .IsRequired(false);

            // for RoomTypeAmenity
            builder.Entity<Amenity>(b =>
            {
                b.Property(x => x.Key).IsRequired().HasMaxLength(64);
                b.Property(x => x.Label).IsRequired().HasMaxLength(100);
                b.Property(x => x.Description).HasMaxLength(256);
                b.Property(x => x.IconKey).HasMaxLength(64);
                b.HasIndex(x => x.Key).IsUnique();
            });

            // RoomTypeAmenity (many-to-many with payload)
            builder.Entity<RoomTypeAmenity>(b =>
            {
                b.HasOne(x => x.RoomType)
                    .WithMany(rt => rt.RoomTypeAmenities)
                    .HasForeignKey(x => x.RoomTypeId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Amenity)
                    .WithMany(a => a.RoomTypeAmenities)
                    .HasForeignKey(x => x.AmenityId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(x => x.AmenityId); // helpful for reverse lookups
            });
        }

    }
}
