using dai.dataAccess.DbContext;
using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;
using Net.payOS;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using dai.core.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace dai.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly PayOS _payOS;

        public PaymentController(IConfiguration config, AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _config = config;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _payOS = new PayOS(
                _config["PayOS:ClientId"],
                _config["PayOS:ApiKey"],
                _config["PayOS:ChecksumKey"]
            );
        }

        private Guid? GetUserIdFromHeader()
        {
            if (Request.Headers.TryGetValue("UserId", out var userIdString) && Guid.TryParse(userIdString, out var userId))
            {
                return userId;
            }
            return null;
        }

        [HttpPost("upgrade-vip")]
        public async Task<IActionResult> CreatePaymentForVip()
        {
            var userId = GetUserIdFromHeader();
            if (userId == null)
                return Unauthorized(new { message = "User not logged in." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Unauthorized(new { message = "User not found." });

            var orderCode = int.Parse(DateTimeOffset.Now.ToString("ffffff"));


            var transaction = new TransactionModel
            {
                UserId = user.Id,
                OrderCode = orderCode,
                Amount = 49000,
                Status = "PENDING",
                CreatedAt = DateTime.Now,
                Description = "VIP Membership Payment"
            };
            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();


            var request = _httpContextAccessor.HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";

            var paymentRequest = new PaymentData(
                orderCode: orderCode,
                amount: 49000,
                description: "VIP_DaiNote",
                items: new List<ItemData> { new ItemData("VIP_Membership", 1, 49000) },
                cancelUrl: "https://dainote.netlify.app/payment-cancel",
                returnUrl: $"{baseUrl}/api/payment/payment-callback?orderCode={orderCode}"
            );

            try
            {
                var response = await _payOS.createPaymentLink(paymentRequest);
                return Ok(new { checkoutUrl = response.checkoutUrl });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error creating payment link: {ex.Message}");
                return BadRequest(new { message = "Failed to create payment link.", error = ex.Message });
            }
        }

        [HttpGet("payment-callback")]
        public async Task<IActionResult> PaymentCallback([FromQuery] int orderCode)
        {
            try
            {
                var paymentInfo = await _payOS.getPaymentLinkInformation(orderCode);
                Console.WriteLine($"Payment Info: {JsonConvert.SerializeObject(paymentInfo)}");

                if (paymentInfo.status == "PAID")
                {
                    var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.OrderCode == orderCode);
                    if (transaction == null)
                    {
                        Console.Error.WriteLine($"Transaction not found for OrderCode: {orderCode}");
                        return Redirect(_config["PayOS:ErrorUrl"]);
                    }

                    var user = await _context.Users.FindAsync(transaction.UserId);
                    if (user == null)
                    {
                        Console.Error.WriteLine($"User not found for UserId: {transaction.UserId}");
                        return Redirect(_config["PayOS:ErrorUrl"]);
                    }


                    transaction.Status = "PAID";
                    transaction.PaidAt = DateTime.Now;
                    await _context.SaveChangesAsync();


                    user.IsVipSupplier = true;
                    user.VipExpiryDate = DateTime.Now.AddMonths(1);
                    await _context.SaveChangesAsync();

                    return Redirect($"{_config["PayOS:SuccessUrl"]}?orderCode={orderCode}");
                }
                else
                {
                    Console.Error.WriteLine($"Payment not completed for OrderCode: {orderCode}, Status: {paymentInfo.status}");
                    return Redirect(_config["PayOS:ErrorUrl"]);
                }
            }
            catch (DbUpdateException ex)
            {
                Console.Error.WriteLine($"Database update error: {ex.InnerException?.Message ?? ex.Message}");
                return Redirect(_config["PayOS:ErrorUrl"]);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error: {ex.Message}");
                return Redirect(_config["PayOS:ErrorUrl"]);
            }
        }

        [HttpGet("user-status")]
        public async Task<IActionResult> GetUserStatus()
        {
            var userId = GetUserIdFromHeader();
            if (userId == null)
                return Unauthorized(new { message = "User not logged in." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(new
            {
                isVip = user.IsVipSupplier,
                vipExpiryDate = user.VipExpiryDate
            });
        }

        [HttpPost("confirm-webhook")]
        public async Task<IActionResult> ConfirmHook([FromBody] ConfirmWebhook body)
        {
            try
            {
                await _payOS.confirmWebhook(body.webhook_url);
                return Ok(new { message = "Webhook confirmed successfully." });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error confirming webhook: {ex.Message}");
                return BadRequest(new { message = "Failed to confirm webhook.", error = ex.Message });
            }
        }

        public class ConfirmWebhook
        {
            public string webhook_url { get; set; }
        }

    }
}
