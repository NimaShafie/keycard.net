using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;

using KeyCard.BusinessLogic.Commands.Admin.Tasks;

namespace KeyCard.BusinessLogic.Validators.Tasks
{
    public class CompleteTaskValidator : AbstractValidator<CompleteTaskCommand>
    {
        public CompleteTaskValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("TaskId must be a positive integer.");
        }
    }
}
