// ============================================================================
// DB SEEDER - INITIAL DATA SETUP
// populates database with sample data on first run
// creates admin user, sample rooms, room types, amenities, test guests
// makes it easy to start testing without manual data entry!
// ============================================================================

using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.Entities;
using KeyCard.Infrastructure.Models.User;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KeyCard.Infrastructure.Models.AppDbContext
{
    /// <summary>
    /// Database seeder - creates initial data for development and testing
    /// Runs on every app startup but only inserts if data is missing
    /// Safe to run multiple times - wont create duplicates!
    /// </summary>
    public static class DbSeeder
    {
        /// <summary>
        /// Main entry point - seeds all data in correct order
        /// Order matters! Hotels before rooms, rooms before bookings, etc.
        /// </summary>
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            // first: roles and admin user (needed for everything else)
            await SeedRolesAndAdminAsync(serviceProvider);
            
            // hotel info (name, address, contact)
            await SeedHotelsAsync(serviceProvider);

            // room types (Single, Double, Suite...) then actual rooms
            await SeedRoomTypesAsync(serviceProvider);
            await SeedRoomsAsync(serviceProvider);

            // amenities (WiFi, TV, etc.) and link them to room types
            await SeedAmenitiesAsync(serviceProvider);          
            await SeedRoomTypeAmenitiesAsync(serviceProvider); 

            // sample users for testing
            await SeedStaffUsersAsync(serviceProvider);
            await SeedGuestUsersAsync(serviceProvider);
        }

        // ==================== ROLES AND ADMIN ====================
        
        #region Admin and Roles
        /// <summary>
        /// Create default roles and a superadmin user
        /// Roles: Admin, Employee, HouseKeeping, Guest
        /// Admin account: admin / Admin@123 (change in production!!)
        /// </summary>
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationUserRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // the 4 main roles in our system
            string[] roles = { "Admin", "Employee", "HouseKeeping", "Guest" };

            // create roles if they dont exist yet
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new ApplicationUserRole { Name = role });
                }
            }

            // create the default admin account
            // WARNING: change password in production environment!
            var adminEmail = "admin@hotel.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    FirstName = "System",
                    LastName = "Administrator",
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true  // skip email verification
                };

                // Admin@123 - meets password requirements (uppercase, lowercase, number, special char)
                var result = await userManager.CreateAsync(adminUser, "Admin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");

                    // extra claim for future permission checks
                    await userManager.AddClaimAsync(adminUser,
                        new System.Security.Claims.Claim("Permission", "ManageSystem"));
                }
            }
        }
        #endregion

        // ==================== HOTEL INFO ====================
        
        #region Hotel
        /// <summary>
        /// Create the hotel record - name, address, contact info
        /// In production, this might be configured differently
        /// </summary>
        private static async Task SeedHotelsAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDBContext>();

            // already have a hotel? skip
            if (await context.Hotels.AnyAsync()) return;

            var hotel = new Hotel
            {
                Name = "KeyCard Grand Hotel",
                Address = "123 Ocean Drive",
                City = "Los Angeles",
                Country = "USA",
                ContactEmail = "info@keycardhotel.com",
                ContactPhone = "+1 (555) 555-5555",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 0 // System user ID
            };

            context.Hotels.Add(hotel);
            await context.SaveChangesAsync();
        }
        #endregion

        // ==================== ROOM TYPES ====================
        
        #region RoomTypes
        /// <summary>
        /// Create room type categories with different prices
        /// Single ($80) → Double ($120) → King ($160) → Deluxe ($220) → Penthouse ($350)
        /// </summary>
        private static async Task SeedRoomTypesAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDBContext>();
            if (await context.RoomTypes.AnyAsync()) return;

            var hotel = await context.Hotels.FirstAsync();

            // room types from budget to luxury
            var roomTypes = new[]
            {
                new RoomType { Name = "Single Bed", Description = "Compact room with a single bed, ideal for solo travelers.", Capacity = 1, BaseRate = 80, HotelId = hotel.Id },
                new RoomType { Name = "Double Bed", Description = "Comfortable room with two single beds or one double bed.", Capacity = 2, BaseRate = 120, HotelId = hotel.Id },
                new RoomType { Name = "King Size", Description = "Spacious room with a king-size bed and modern amenities.", Capacity = 2, BaseRate = 160, HotelId = hotel.Id },
                new RoomType { Name = "Deluxe Suite", Description = "Luxurious suite with lounge area, work desk, and balcony.", Capacity = 3, BaseRate = 220, HotelId = hotel.Id },
                new RoomType { Name = "Penthouse Suite", Description = "Top-floor suite with panoramic views, jacuzzi, and VIP service.", Capacity = 4, BaseRate = 350, HotelId = hotel.Id }
            };

            foreach (var type in roomTypes)
            {
                type.CreatedAt = DateTime.UtcNow;
                type.CreatedBy = 0;
            }

            context.RoomTypes.AddRange(roomTypes);
            await context.SaveChangesAsync();
        }
        #endregion

        // ==================== ACTUAL ROOMS ====================
        
        #region Rooms
        /// <summary>
        /// Create actual room instances for each room type
        /// Room numbers follow pattern: 1st floor = 100s, 2nd floor = 200s, etc.
        /// More singles and doubles, fewer penthouses (realistic distribution)
        /// </summary>
        private static async Task SeedRoomsAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDBContext>();

            if (await context.Rooms.AnyAsync()) return;

            var hotel = await context.Hotels.FirstAsync();
            var roomTypes = await context.RoomTypes.ToListAsync();

            var rooms = new List<Room>();

            foreach (var type in roomTypes)
            {
                // room numbers start at different floors based on type
                // singles on 1st floor (100s), doubles on 2nd (200s), etc.
                int start = type.Name switch
                {
                    "Single Bed" => 100,
                    "Double Bed" => 200,
                    "King Size" => 300,
                    "Deluxe Suite" => 400,
                    "Penthouse Suite" => 500,
                    _ => 600
                };

                // how many rooms of each type
                // lots of basic rooms, few fancy ones
                int count = type.Name switch
                {
                    "Single Bed" => 10,      // 100-109
                    "Double Bed" => 12,      // 200-211
                    "King Size" => 8,        // 300-307
                    "Deluxe Suite" => 5,     // 400-404
                    "Penthouse Suite" => 2,  // 500-501
                    _ => 0
                };

                for (int i = 0; i < count; i++)
                {
                    int roomNumber = start + i;
                    var floor = (roomNumber / 100).ToString();  // 305 → floor "3"

                    rooms.Add(new Room
                    {
                        RoomNumber = roomNumber.ToString(),
                        Floor = floor,
                        Status = RoomStatus.Vacant,  // all rooms start as vacant
                        RoomTypeId = type.Id,
                        HotelId = hotel.Id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 0
                    });
                }
            }

            context.Rooms.AddRange(rooms);
            await context.SaveChangesAsync();
        }
        #endregion

        // ==================== AMENITIES ====================
        
        #region Amenities
        /// <summary>
        /// Create amenity definitions - the features rooms can have
        /// WiFi, TV, coffee maker, etc.
        /// Each has a key (for code), label (for display), and icon
        /// </summary>
        private static async Task SeedAmenitiesAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDBContext>();
            if (await context.Amenities.AnyAsync()) return;

            var amenities = new List<Amenity>
            {
                // basic amenities most rooms have
                new Amenity { Key = "wifi", Label = "Free WiFi", IconKey = "wifi", CreatedAt = DateTime.UtcNow, CreatedBy = 0 },
                new Amenity { Key = "tv", Label = "Smart TV", IconKey = "tv", CreatedAt = DateTime.UtcNow, CreatedBy = 0 },
                
                // coffee options - basic rooms get regular, suites get espresso
                new Amenity { Key = "coffee", Label = "Coffee Maker", IconKey = "coffee", CreatedAt = DateTime.UtcNow, CreatedBy = 0 },
                new Amenity { Key = "espresso", Label = "Espresso Machine", IconKey = "coffee", CreatedAt = DateTime.UtcNow, CreatedBy = 0 },
                
                // capacity indicator
                new Amenity { Key = "guests", Label = "Max Guests", IconKey = "users", CreatedAt = DateTime.UtcNow, CreatedBy = 0 },
            };

            context.Amenities.AddRange(amenities);
            await context.SaveChangesAsync();
        }
        #endregion

        // ==================== ROOM TYPE AMENITY LINKS ====================
        
        #region RoomTypeAmenities
        /// <summary>
        /// Link amenities to room types with specific values
        /// e.g., "King Size room has 43 inch TV"
        /// Fancier rooms get better amenities (bigger TV, espresso instead of drip coffee)
        /// </summary>
        private static async Task SeedRoomTypeAmenitiesAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDBContext>();
            if (await context.RoomTypeAmenities.AnyAsync()) return;

            var hotel = await context.Hotels.FirstAsync();
            var roomTypes = await context.RoomTypes
                .Where(rt => rt.HotelId == hotel.Id)
                .ToListAsync();

            // get amenity IDs by key for easy lookup
            var amenityIds = await context.Amenities
                .ToDictionaryAsync(a => a.Key, a => a.Id);

            foreach (var rt in roomTypes)
            {
                // ===== WiFi - EVERYONE gets WiFi (its 2024!) =====
                context.RoomTypeAmenities.Add(new RoomTypeAmenity
                {
                    RoomTypeId = rt.Id,
                    AmenityId = amenityIds["wifi"],
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 0
                });

                // ===== TV - bigger rooms get bigger TVs =====
                var tvInches = rt.Name switch
                {
                    "Single Bed" => 32,       // compact room, small TV
                    "Double Bed" => 32,       // still modest
                    "King Size" => 43,        // nice upgrade
                    "Deluxe Suite" => 55,     // big screen!
                    "Penthouse Suite" => 65,  // home theater vibes
                    _ => 32
                };
                context.RoomTypeAmenities.Add(new RoomTypeAmenity
                {
                    RoomTypeId = rt.Id,
                    AmenityId = amenityIds["tv"],
                    Value = tvInches.ToString() + " inch",  // e.g., "55 inch"
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 0
                });

                // ===== Coffee - suites get fancy espresso, others get regular =====
                if (rt.Name is "Deluxe Suite" or "Penthouse Suite")
                {
                    // fancy rooms get espresso machine
                    context.RoomTypeAmenities.Add(new RoomTypeAmenity
                    {
                        RoomTypeId = rt.Id,
                        AmenityId = amenityIds["espresso"],
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 0
                    });
                }
                else
                {
                    // regular rooms get standard coffee maker
                    context.RoomTypeAmenities.Add(new RoomTypeAmenity
                    {
                        RoomTypeId = rt.Id,
                        AmenityId = amenityIds["coffee"],
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 0
                    });
                }
            }

            await context.SaveChangesAsync();
        }
        #endregion

        // ==================== SAMPLE GUEST ACCOUNTS ====================
        
        #region Guests
        /// <summary>
        /// Create sample guest accounts for testing
        /// All use password "Guest@123"
        /// These are the people who stay at the hotel
        /// </summary>
        private static async Task SeedGuestUsersAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationUserRole>>();

            // check if Guest role exists
            var guestRole = await roleManager.FindByNameAsync("Guest");
            if (guestRole == null)
                return;

            // already have guests? skip
            var anyGuest = (await userManager.GetUsersInRoleAsync("Guest")).Any();
            if (anyGuest)
                return;

            // sample guests with realistic-ish data
            var guests = new List<(string FirstName, string LastName, string Email, string Address, string Country)>
            {
                ("John", "Doe", "john.doe@guest.com", "123 Demo Street", "USA"),
                ("Jane", "Smith", "jane.smith@guest.com", "456 Ocean Ave", "USA"),
                ("John", "Brown", "robert.brown@guest.com", "789 Maple Road", "Canada"),
                ("Emily", "Davis", "emily.davis@guest.com", "22 Lakeview Blvd", "UK"),
                ("Michael", "Wilson", "michael.wilson@guest.com", "12 Greenway Crescent", "USA")
            };

            foreach (var (firstName, lastName, email, address, country) in guests)
            {
                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser != null) continue;

                var user = new ApplicationUser
                {
                    UserName = firstName + lastName,  // JohnDoe
                    Email = email,
                    FirstName = firstName,
                    LastName = firstName + " " + lastName,
                    FullName = lastName + " " + firstName,  // "Doe John"
                    Address = address,
                    Country = country,
                    EmailConfirmed = true
                };

                // Guest@123 - simple password for testing
                var result = await userManager.CreateAsync(user, "Guest@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Guest");
                }
            }
        }
        #endregion

        // ==================== SAMPLE STAFF ACCOUNTS ====================
        
        #region Staff
        /// <summary>
        /// Create sample staff accounts for testing
        /// Sarah = front desk employee, David = housekeeping
        /// All use password "Staff@123"
        /// </summary>
        private static async Task SeedStaffUsersAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // sample staff members
            var staffList = new List<(string FirstName, string LastName, string Email, string Role)>
            {
                ("Sarah", "Johnson", "sarah.johnson@hotel.com", "Employee"),    // front desk
                ("David", "Clark", "david.clark@hotel.com", "HouseKeeping")     // cleaning staff
            };

            foreach (var (firstName, lastName, email, role) in staffList)
            {
                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser != null) continue;

                var user = new ApplicationUser
                {
                    UserName = email,  // staff login with email
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    FullName = lastName + " " + firstName,
                    EmailConfirmed = true
                };

                // Staff@123 - simple password for testing
                var result = await userManager.CreateAsync(user, "Staff@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(user, role);
            }
        }
        #endregion
    }
}
