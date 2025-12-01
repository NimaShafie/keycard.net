// ============================================================================
// APPLICATION DB CONTEXT - THE DATABASE GATEWAY
// Entity Framework Core context - our connection to the database
// all queries and saves go through here
// think of it as the translator between C# objects and database tables
// ============================================================================

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
    /// <summary>
    /// Main database context for the hotel management system
    /// Inherits from IdentityDbContext for user authentication tables
    /// Contains DbSets for all our entities - rooms, bookings, guests, etc.
    /// </summary>
    public class ApplicationDBContext
        : IdentityDbContext<ApplicationUser, ApplicationUserRole, int>
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options) { }

        // ===== DbSets - each one maps to a database table =====
        public DbSet<Hotel> Hotels => Set<Hotel>();                    // hotel info (name, address, contact)
        public DbSet<RoomType> RoomTypes => Set<RoomType>();           // Single, Double, Suite, etc.
        public DbSet<Room> Rooms => Set<Room>();                       // actual rooms (101, 102, 103...)
        public DbSet<Amenity> Amenities => Set<Amenity>();             // WiFi, TV, Coffee maker...
        public DbSet<RoomTypeAmenity> RoomTypeAmenities => Set<RoomTypeAmenity>();  // which amenity in which room type
        public DbSet<Booking> Bookings => Set<Booking>();              // the core business - reservations!
        public DbSet<Payment> Payments => Set<Payment>();              // payment records
        public DbSet<Invoice> Invoices => Set<Invoice>();              // generated invoices (PDF links)
        public DbSet<HousekeepingTask> HousekeepingTasks => Set<HousekeepingTask>();  // cleaning tasks
        public DbSet<DigitalKey> DigitalKeys => Set<DigitalKey>();     // mobile room keys
        
        /// <summary>
        /// Configure conventions for all entities
        /// Sets default precision for decimal types (money needs 2 decimal places!)
        /// </summary>
        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
            // all decimals get 18 digits total, 2 after decimal point
            // good for money: 999999999999999999.99
            builder.Properties<decimal>().HavePrecision(18, 2);
            builder.Properties<decimal?>().HavePrecision(18, 2);
        }

        /// <summary>
        /// Configure entity relationships and constraints
        /// This is where we tell EF how tables relate to each other
        /// Very important for data integrity!
        /// </summary>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ===== HOTEL RELATIONSHIPS =====
            // we disable cascade delete to prevent accidental data loss
            // if you delete a hotel, you dont want ALL rooms and bookings to vanish!
            
            // Hotel → RoomType (no cascade delete)
            builder.Entity<RoomType>()
                .HasOne(rt => rt.Hotel)
                .WithMany(h => h.RoomTypes)
                .HasForeignKey(rt => rt.HotelId)
                .OnDelete(DeleteBehavior.NoAction);

            // Hotel → Room (no cascade delete)
            builder.Entity<Room>()
                .HasOne(r => r.Hotel)
                .WithMany(h => h.Rooms)
                .HasForeignKey(r => r.HotelId)
                .OnDelete(DeleteBehavior.NoAction);

            // RoomType → Room (no cascade delete)
            builder.Entity<Room>()
                .HasOne(r => r.RoomType)
                .WithMany()
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.NoAction);

            // ===== BOOKING RELATIONSHIPS =====
            // Restrict delete - cant delete a room if it has bookings!
            builder.Entity<Booking>()
                .HasOne(b => b.Room)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // cant delete a guest if they have bookings
            builder.Entity<Booking>()
                .HasOne(b => b.GuestProfile)
                .WithMany(g => g.Bookings)
                .HasForeignKey(b => b.GuestProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== INVOICE - one per booking =====
            // cascade delete OK here - if booking deleted, invoice goes too
            builder.Entity<Invoice>()
                .HasOne(i => i.Booking)
                .WithOne(b => b.Invoice)
                .HasForeignKey<Invoice>(i => i.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // invoice number must be unique! prevents duplicates
            builder.Entity<Invoice>(b =>
            {
                b.HasIndex(i => i.InvoiceNumber).IsUnique();
            });

            // ===== DIGITAL KEY - one per booking =====
            // cascade delete OK - if booking goes, key should go too
            builder.Entity<DigitalKey>()
                .HasOne(dk => dk.Booking)
                .WithOne(b => b.DigitalKey)
                .HasForeignKey<DigitalKey>(dk => dk.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== HOUSEKEEPING TASK RELATIONSHIPS =====
            // if staff member deleted, just set AssignedTo to null
            builder.Entity<HousekeepingTask>()
                .HasOne(ht => ht.AssignedTo)
                .WithMany()
                .HasForeignKey(ht => ht.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            // task doesnt require a room (could be general task)
            builder.Entity<HousekeepingTask>()
                .HasOne(t => t.Room)
                .WithMany()
                .HasForeignKey(t => t.RoomId)
                .IsRequired(false);

            // ===== AMENITY CONFIGURATION =====
            // amenities are things like WiFi, TV, minibar
            builder.Entity<Amenity>(b =>
            {
                b.Property(x => x.Key).IsRequired().HasMaxLength(64);   // short key like "wifi"
                b.Property(x => x.Label).IsRequired().HasMaxLength(100); // display name "Free WiFi"
                b.Property(x => x.Description).HasMaxLength(256);
                b.Property(x => x.IconKey).HasMaxLength(64);
                b.HasIndex(x => x.Key).IsUnique();  // each amenity key unique
            });

            // ===== ROOM TYPE AMENITIES =====
            // many-to-many: room types can have many amenities, amenities can be in many room types
            // this join table also has Value column (e.g., TV size: "55 inch")
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

                b.HasIndex(x => x.AmenityId); // makes "find rooms with WiFi" queries faster
            });
        }
    }
}
