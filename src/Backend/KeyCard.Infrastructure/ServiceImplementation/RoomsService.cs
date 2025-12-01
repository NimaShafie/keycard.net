// ============================================================================
// ROOMS SERVICE - ROOM SEARCH AND AVAILABILITY
// when guest wants to book, they first need to see whats available
// this service finds rooms that match their dates, guest count, and budget
// ============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.BusinessLogic.Commands.Guest.Rooms;
using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels.Rooms;
using KeyCard.Core.Common;
using KeyCard.Infrastructure.Models.AppDbContext;
using KeyCard.Infrastructure.Models.Entities;

using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json.Linq;

namespace KeyCard.Infrastructure.ServiceImplementation
{
    /// <summary>
    /// Rooms service - handles room search and availability checking
    /// Powers the booking flow where guest selects their room type
    /// </summary>
    public class RoomsService : IRoomsService
    {
        private readonly ApplicationDBContext _context;
        
        public RoomsService(ApplicationDBContext applicationDBContext) {
            this._context = applicationDBContext;
        }
        
        /// <summary>
        /// Search for available rooms based on dates and guest count
        /// Returns list of room types with pricing, amenities, availability
        /// This is the main search used in booking flow
        /// </summary>
        public async Task<RoomOptionsViewModel> GetRoomOptionsAsync(GetRoomOptionsCommand command, CancellationToken cancellationToken)
        {
            // calculate how many nights they are staying
            var nights = (command.CheckOut.ToDateTime(TimeOnly.MinValue) - command.CheckIn.ToDateTime(TimeOnly.MinValue)).Days;
            
            // sanity check: checkout must be after checkin!
            if (nights <= 0) throw new ArgumentException("Check-in must be before check-out.");

            // figure out how many guests need to fit in each room
            // e.g., 5 guests in 2 rooms = 3 guests per room (round up)
            var guestsPerRoom = (int)Math.Ceiling((double)command.Guests / command.Rooms);

            // ===== Find room types that can fit the guests =====
            // also count how many vacant rooms of each type we have
            var possibleRooms = await _context.RoomTypes
                .Where(rt => !rt.IsDeleted && rt.Capacity >= guestsPerRoom)
                .Select(rt => new
                {
                    rt.Id,
                    rt.Name,
                    rt.Description,
                    rt.Capacity,
                    rt.BaseRate,
                    rt.SeasonalRate,
                    // count available rooms of this type
                    VacantCount = _context.Rooms.Count(r =>
                        r.RoomTypeId == rt.Id &&
                        !r.IsDeleted &&
                        r.Status == RoomStatus.Vacant)
                })
                // only show room types where we have enough vacant rooms
                .Where(x => x.VacantCount >= command.Rooms)
                .ToListAsync(cancellationToken);

            // no rooms available? return empty result
            // guest will see "Sorry, no rooms available for these dates"
            if (possibleRooms.Count == 0)
            {
                return new RoomOptionsViewModel
                (
                    new SearchContextViewModel
                    (
                        command.CheckIn,
                        command.CheckOut,
                        nights,
                        command.Guests,
                        command.Rooms,
                        command.Currency
                    ),
                    new List<RoomTypeViewModel>()  // empty list
                );
            }

            var roomTypeIds = possibleRooms.Select(x => x.Id).ToList();

            // ===== Get amenities for each room type =====
            // WiFi, TV size, coffee maker, etc.
            var amenityRows = await _context.RoomTypeAmenities
                .AsNoTracking()
                .Where(rta => roomTypeIds.Contains(rta.RoomTypeId))
                .Select(rta => new
                {
                    rta.RoomTypeId,
                    rta.Amenity.Key,
                    rta.Amenity.Label,
                    rta.Amenity.Description,
                    rta.Amenity.IconKey,
                    rta.Value  // e.g., "55 inch" for TV
                })
                .ToListAsync(cancellationToken);

            // group amenities by room type for easy lookup
            var amenitiesByRt = amenityRows
                .GroupBy(x => x.RoomTypeId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<AmenityViewModel>)g.Select(x =>
                    {
                        return new AmenityViewModel
                        (
                            Key: x.Key,
                            Label: x.Label,
                            Description: x.Description,
                            IconKey: x.IconKey,
                            Value: x.Value
                        );
                    }).ToList()
                );

            // ===== Build the response with pricing =====
            var options = possibleRooms.Select(x =>
            {
                // use seasonal rate if set, otherwise fall back to base rate
                // seasonal rate is for holidays, peak season, etc.
                decimal perNight = (decimal)(x.SeasonalRate != null ? x.SeasonalRate : x.BaseRate);
                
                // total = price per night × nights × number of rooms
                var total = perNight * nights * command.Rooms;

                // get amenities for this room type
                amenitiesByRt.TryGetValue(x.Id, out var amenityList);
                amenityList ??= Array.Empty<AmenityViewModel>();

                return new RoomTypeViewModel
                (
                    x.Id,
                    x.Name,
                    x.Description,
                    perNight,
                    false,  // isPromotion - TODO: implement promotions
                    x.Capacity,
                    amenityList,
                    total
                );
            })
            .OrderBy(x => x.PricePerNight)  // cheapest first!
            .ToList();

            return new RoomOptionsViewModel(
                new SearchContextViewModel
                (
                    command.CheckIn,
                    command.CheckOut,
                    nights,
                    command.Guests,
                    command.Rooms,
                    command.Currency
                ),
                options
            );
        }
    }
}
