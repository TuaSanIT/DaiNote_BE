using Microsoft.AspNetCore.Mvc;

namespace dai.api.Controllers
{
    [ApiController]
    [Route("api/timezones")]
    public class TimeZoneController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetTimeZones()
        {
            var timeZones = TimeZoneInfo.GetSystemTimeZones()
                .Select(tz => tz.Id)
                .ToList();
            return Ok(timeZones);
        }
    }
}
