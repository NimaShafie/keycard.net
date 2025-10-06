using KeyCard.Core.Common;
using KeyCard.Infrastructure.Identity;
using KeyCard.Infrastructure.Models.Entities;
using KeyCard.Infrastructure.Models.Users;

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
        public static async Task SeedRolesAndAdminAsync(
            IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Admin", "Employee", "HouseKeeping", "Guest" };

            // Create roles if they don't exist
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new ApplicationRole { Name = role });
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
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123"); // default password

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");

                    // Add claims (optional)
                    await userManager.AddClaimAsync(adminUser, new System.Security.Claims.Claim("Permission", "ManageSystem"));
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
                CreatedBy = new Guid()
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
                new RoomType
                {
                    Name = "Single Bed",
                    Description = "Compact room with a single bed, ideal for solo travelers.",
                    Capacity = 1,
                    BaseRate = 80,
                    HotelId = hotel.Id,
                    CreatedBy = new Guid(),
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Name = "Double Bed",
                    Description = "Comfortable room with two single beds or one double bed.",
                    Capacity = 2,
                    BaseRate = 120,
                    HotelId = hotel.Id,
                    CreatedBy = new Guid(),
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Name = "King Size",
                    Description = "Spacious room with a king-size bed and modern amenities.",
                    Capacity = 2,
                    BaseRate = 160,
                    HotelId = hotel.Id,
                    CreatedBy = new Guid(),
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Name = "Deluxe Suite",
                    Description = "Luxurious suite with lounge area, work desk, and balcony.",
                    Capacity = 3,
                    BaseRate = 220,
                    HotelId = hotel.Id,
                    CreatedBy = new Guid(),
                    CreatedAt = DateTime.UtcNow
                },
                new RoomType
                {
                    Name = "Penthouse Suite",
                    Description = "Top-floor suite with panoramic views, jacuzzi, and VIP service.",
                    Capacity = 4,
                    BaseRate = 350,
                    HotelId = hotel.Id,
                    CreatedBy = new Guid(),
                    CreatedAt = DateTime.UtcNow
                }
            };

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
                // Assign numbering ranges based on room type
                int start = type.Name switch
                {
                    "Single Bed" => 100,
                    "Double Bed" => 200,
                    "King Size" => 300,
                    "Deluxe Suite" => 400,
                    "Penthouse Suite" => 500,
                    _ => 600
                };

                // Decide how many rooms of each type you want
                int count = type.Name switch
                {
                    "Single Bed" => 10,        // 10 single rooms (100–109)
                    "Double Bed" => 12,        // 12 doubles (200–211)
                    "King Size" => 8,          // 8 kings (300–307)
                    "Deluxe Suite" => 5,       // 5 deluxe suites (400–404)
                    "Penthouse Suite" => 2,    // 2 penthouses (500–501)
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
                        CreatedBy = new Guid()
                    });
                }
            }

            context.Rooms.AddRange(rooms);
            await context.SaveChangesAsync();
        }
        #endregion

        #region Guests
        private static async Task SeedGuestUsersAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDBContext>();

            // Prevent duplication if already seeded
            if (await context.GuestProfiles.AnyAsync())
                return;

            var guests = new List<(string FullName, string Email)>
            {
                ("John Doe", "john.doe@guest.com"),
                ("Jane Smith", "jane.smith@guest.com"),
                ("Robert Brown", "robert.brown@guest.com"),
                ("Emily Davis", "emily.davis@guest.com"),
                ("Michael Wilson", "michael.wilson@guest.com")
            };

            foreach (var (fullName, email) in guests)
            {
                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser != null) continue;

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, "Guest@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Guest");

                    var profile = new GuestProfile
                    {
                        UserId = user.Id,
                        Address = "123 Demo Street",
                        Country = "USA",
                        CreatedBy = new Guid(),
                        CreatedAt = DateTime.UtcNow
                    };

                    context.GuestProfiles.Add(profile);
                }
            }

            await context.SaveChangesAsync();
        }
        #endregion

        #region Staff
        private static async Task SeedStaffUsersAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDBContext>();

            if (await context.StaffProfiles.AnyAsync())
                return;

            var staffList = new List<(string FullName, string Email, string Department, string Role)>
            {
                ("Sarah Johnson", "sarah.johnson@hotel.com", "FrontDesk", "Employee"),
                ("David Clark", "david.clark@hotel.com", "Housekeeping", "HouseKeeping")
            };

            foreach (var (fullName, email, department, role) in staffList)
            {
                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser != null) continue;

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, "Staff@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);

                    var profile = new StaffProfile
                    {
                        UserId = user.Id,
                        Department = department,
                        IsDeleted = false,
                        CreatedBy = new Guid(),
                        CreatedAt = DateTime.UtcNow
                    };

                    context.StaffProfiles.Add(profile);
                }
            }

            await context.SaveChangesAsync();
        }
        #endregion


    }
}
