using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;

using KeyCard.BusinessLogic.Commands.Guest.Rooms;

namespace KeyCard.BusinessLogic.Validators.Guest.Rooms
{
    public class RoomOptionsValidator: AbstractValidator<GetRoomOptionsCommand>
    {
        public RoomOptionsValidator()
        {
            // Today in UTC
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
             
            RuleFor(x => x.CheckIn)
              .Must(ci => ci >= today)
              .WithMessage("Check-in must be today or later.");

            RuleFor(x => x.CheckIn)
                .LessThan(x => x.CheckOut)
                .WithMessage("CheckIn date must be before CheckOut date.");

            RuleFor(x => x.Guests)
                .GreaterThan(0)
                .WithMessage("Guests must be at least 1.");

            RuleFor(x => x.Rooms)
                .GreaterThan(0)
                .WithMessage("Rooms must be at least 1.");
        }
    }
}
