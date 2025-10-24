using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;

using KeyCard.BusinessLogic.Commands.Admin.Tasks;

namespace KeyCard.BusinessLogic.Validators.Admin.Tasks
{
    public class DeleteTaskValidator : AbstractValidator<DeleteTaskCommand>
    {
        public DeleteTaskValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("TaskId must be a positive integer.");
        }
    }
}
