using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.BusinessLogic.Commands;
using KeyCard.BusinessLogic.HubInterfaces;

using MediatR;

namespace KeyCard.BusinessLogic.Queries
{
    public class DemoCommandHandler
        : IRequestHandler<DemoCommand, string>
    {
        public DemoCommandHandler()
        {
        }

        public Task<string> Handle(DemoCommand request, CancellationToken ct)
        {
            return Task.FromResult("Hi " + request.s);
        }
    }

}
