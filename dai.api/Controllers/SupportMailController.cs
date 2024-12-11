using AutoMapper.Internal;
using dai.api.Helper;
using dai.api.Services.ServicesAPI;
using dai.dataAccess.DbContext;
using Microsoft.AspNetCore.Mvc;

namespace dai.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupportMailController : Controller
    {
        private readonly IEmailService _emailService;

        public SupportMailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromBody] Mailrequest mailRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid email request");

            try
            {
                await _emailService.SendEmailAsync(mailRequest);
                return Ok("Email sent successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
