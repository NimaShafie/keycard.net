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
    public class RoomsService : IRoomsService
    {
        private readonly ApplicationDBContext _context;
        public RoomsService(ApplicationDBContext applicationDBContext) {
            this._context = applicationDBContext;
        }
        public async Task<RoomOptionsViewModel> GetRoomOptionsAsync(GetRoomOptionsCommand command, CancellationToken cancellationToken)
        {
            var nights = (command.CheckOut.ToDateTime(TimeOnly.MinValue) - command.CheckIn.ToDateTime(TimeOnly.MinValue)).Days;
            if (nights <= 0) throw new ArgumentException("Check-in must be before check-out.");

            // Guests needed per room (round up)
            var guestsPerRoom = (int)Math.Ceiling((double)command.Guests / command.Rooms);

            // check which rooms are available for given guests/rooms
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
                    VacantCount = _context.Rooms.Count(r =>
                        r.RoomTypeId == rt.Id &&
                        !r.IsDeleted &&
                        r.Status == RoomStatus.Vacant)
                })
                .Where(x => x.VacantCount >= command.Rooms)
                .ToListAsync(cancellationToken);

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
                    new List<RoomTypeViewModel>()
                );
            }

            var roomTypeIds = possibleRooms.Select(x => x.Id).ToList();

            // get all amenities for the possible rooms
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
                    rta.Value
                })
                .ToListAsync(cancellationToken);

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


            var options = possibleRooms.Select(x =>
            {
                // Pricing: prefer SeasonalRate when > 0 otherwise BaseRate
                decimal perNight = (decimal)(x.SeasonalRate != null ? x.SeasonalRate : x.BaseRate);
                var total = perNight * nights * command.Rooms;

                amenitiesByRt.TryGetValue(x.Id, out var amenityList);
                amenityList ??= Array.Empty<AmenityViewModel>();

                return new RoomTypeViewModel
                (
                    x.Id,
                    x.Name,
                    x.Description,
                    perNight,
                    false,
                    x.Capacity,
                    amenityList,
                    total
                );
            }).OrderBy(x => x.PricePerNight)
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
