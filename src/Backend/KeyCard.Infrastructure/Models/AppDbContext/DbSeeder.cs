using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.Entities;
using KeyCard.Infrastructure.Models.User;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KeyCard.Infrastructure.Models.AppDbContext
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            await SeedRolesAndAdminAsync(serviceProvider);
            await SeedHotelsAsync(serviceProvider);
            await SeedRoomTypesAsync(serviceProvider);
            await SeedRoomsAsync(serviceProvider);
            await SeedStaffUsersAsync(serviceProvider);
            await SeedGuestUsersAsync(serviceProvider);
        }

        #region Admin and Roles
        /// <summary>
        /// Seed default roles and an admin user
        /// </summary>
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationUserRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Admin", "Employee", "HouseKeeping", "Guest" };

            // Create roles if they don't exist
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new ApplicationUserRole { Name = role });
                }
            }

            // Create a default Admin
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
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");

                    // Optional claim
                    await userManager.AddClaimAsync(adminUser,
                        new System.Security.Claims.Claim("Permission", "ManageSystem"));
                }
            }
        }
        #endregion

        #region Hotel
        private static async Task SeedHotelsAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDBContext>();

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
                CreatedBy = 0 // System
            };

            context.Hotels.Add(hotel);
            await context.SaveChangesAsync();
        }
        #endregion

        #region RoomTypes
        private static async Task SeedRoomTypesAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDBContext>();
            if (await context.RoomTypes.AnyAsync()) return;

            var hotel = await context.Hotels.FirstAsync();

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
                type.CreatedBy = 0; // System
            }

            context.RoomTypes.AddRange(roomTypes);
            await context.SaveChangesAsync();
        }
        #endregion

        #region Rooms
        private static async Task SeedRoomsAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDBContext>();

            if (await context.Rooms.AnyAsync()) return;

            var hotel = await context.Hotels.FirstAsync();
            var roomTypes = await context.RoomTypes.ToListAsync();

            var rooms = new List<Room>();

            foreach (var type in roomTypes)
            {
                int start = type.Name switch
                {
                    "Single Bed" => 100,
                    "Double Bed" => 200,
                    "King Size" => 300,
                    "Deluxe Suite" => 400,
                    "Penthouse Suite" => 500,
                    _ => 600
                };

                int count = type.Name switch
                {
                    "Single Bed" => 10,
                    "Double Bed" => 12,
                    "King Size" => 8,
                    "Deluxe Suite" => 5,
                    "Penthouse Suite" => 2,
                    _ => 0
                };

                for (int i = 0; i < count; i++)
                {
                    int roomNumber = start + i;
                    var floor = (roomNumber / 100).ToString();

                    rooms.Add(new Room
                    {
                        RoomNumber = roomNumber.ToString(),
                        Floor = floor,
                        Status = RoomStatus.Vacant,
                        RoomTypeId = type.Id,
                        HotelId = hotel.Id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 0 // System
                    });
                }
            }

            context.Rooms.AddRange(rooms);
            await context.SaveChangesAsync();
        }
        #endregion

        #region Guests
        #region Guests
        private static async Task SeedGuestUsersAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationUserRole>>();

            // Just check if any user is already in the Guest role
            var guestRole = await roleManager.FindByNameAsync("Guest");
            if (guestRole == null)
                return; // Role not created yet (shouldn't happen)

            var anyGuest = (await userManager.GetUsersInRoleAsync("Guest")).Any();
            if (anyGuest)
                return; // Already seeded

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
                    UserName = firstName + lastName,
                    Email = email,
                    FirstName = firstName,
                    LastName = firstName + " " + lastName,
                    FullName = lastName + " " + firstName,
                    Address = address,
                    Country = country,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, "Guest@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Guest");
                }
            }
        }
        #endregion

        #endregion

        #region Staff
        private static async Task SeedStaffUsersAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var staffList = new List<(string FirstName, string LastName, string Email, string Role)>
            {
                ("Sarah", "Johnson" ,"sarah.johnson@hotel.com", "Employee"),
                ("David" , "Clark", "david.clark@hotel.com", "HouseKeeping")
            };

            foreach (var (firstName, lastName, email, role) in staffList)
            {
                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser != null) continue;

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    FullName = lastName + " " + firstName,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, "Staff@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(user, role);
            }
        }
        #endregion
    }
}
