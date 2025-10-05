using Microsoft.AspNetCore.Mvc;

namespace KeyCard.Api.Controllers
{
    public class RoomController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
