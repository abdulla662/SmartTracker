using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Subscription;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly string _frontendUrl;

        public SubscriptionController(ISubscriptionService subscriptionService, IConfiguration config)
        {
            _subscriptionService = subscriptionService;
            _frontendUrl = config["AppSettings:FrontendUrl"] ?? "";
        }

        [HttpPost("initiate")]
        [Authorize]
        public async Task<ApiResponseT<InitiatePaymentResponseDto>> InitiatePayment(
            InitiatePaymentDto dto, CancellationToken ct)
            => await _subscriptionService.InitiatePaymentAsync(dto, ct);

        [HttpPost("webhook")]
        public async Task<IActionResult> WebhookPost(CancellationToken ct)
        {
            var hmac = Request.Query["hmac"].ToString();
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync(ct);

            var result = await _subscriptionService.HandleWebhookAsync(rawBody, hmac, ct);
            return Ok(result);
        }

        [HttpGet("webhook")]
        public async Task<IActionResult> WebhookGet(CancellationToken ct)
        {
            var query = Request.Query;
            var success = query["success"].ToString();
            var merchantOrderId = query["merchant_order_id"].ToString();
            var hmac = query["hmac"].ToString();

            if (success != "true" || string.IsNullOrEmpty(merchantOrderId))
                return Redirect($"{_frontendUrl}/payment/callback?success=false&merchant_order_id={Uri.EscapeDataString(merchantOrderId)}");

            var result = await _subscriptionService.HandleWebhookGetAsync(merchantOrderId, hmac, ct);

            var activated = result?.Success == true;
            var redirectUrl = activated
                ? $"{_frontendUrl}/payment/callback?success=true&merchant_order_id={Uri.EscapeDataString(merchantOrderId)}"
                : $"{_frontendUrl}/payment/callback?success=false&merchant_order_id={Uri.EscapeDataString(merchantOrderId)}";

            return Redirect(redirectUrl);
        }
    }
}
