using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyCard.BusinessLogic.ViewModels.UserClaims
{
    public record UserClaimsViewModel (
        int UserId,
        string Email,
        string Role);

}
