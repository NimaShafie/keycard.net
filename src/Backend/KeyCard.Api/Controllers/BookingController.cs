using KeyCard.Application.Bookings;
using Microsoft.AspNetCore.Mvc;

namespace KeyCard.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class BookingsController(IBookingService svc) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await svc.ListAsync(ct));

    [HttpGet("{code}")]
    public async Task<IActionResult> ByCode(string code, CancellationToken ct)
        => (await svc.GetByCodeAsync(code, ct)) is { } dto ? Ok(dto) : NotFound();
}
