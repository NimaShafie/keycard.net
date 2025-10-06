using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyCard.BusinessLogic.ViewModels.UserClaims
{
    public record UserClaimsViewModel (
        Guid UserId,
        string Email,
        string Role);

}
