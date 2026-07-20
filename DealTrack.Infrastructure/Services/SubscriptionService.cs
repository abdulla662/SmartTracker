using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Subscription;
using DealTrack.Application.Interfaces;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DealTrack.Infrastructure.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly IConfiguration _config;
        private readonly HttpClient _http;
        private readonly UserManager<ApplicationUser> _userManager;

        private string ApiKey => _config["Paymob:ApiKey"]!;
        private string IntegrationId => _config["Paymob:IntegrationId"]!;
        private string IframeId => _config["Paymob:IframeId"]!;
        private string HmacSecret => _config["Paymob:HmacSecret"]!;
        private string FrontendUrl => _config["AppSettings:FrontendUrl"] ?? "";

        private static readonly Dictionary<string, (int AmountCents, SubscriptionPlan Plan)> Plans = new()
        {
            { "Advanced",   (9900,   SubscriptionPlan.Advanced) },
            { "Pro",        (29900,  SubscriptionPlan.Pro) },
            { "Enterprise", (99900,  SubscriptionPlan.Enterprise) },
        };

        public SubscriptionService(IUnitOfWork uow, ICurrentUserService currentUser, IConfiguration config, HttpClient http, UserManager<ApplicationUser> userManager)
        {
            _uow = uow;
            _currentUser = currentUser;
            _config = config;
            _http = http;
            _userManager = userManager;
        }

        public async Task<ApiResponseT<InitiatePaymentResponseDto>> InitiatePaymentAsync(InitiatePaymentDto dto, CancellationToken ct)
        {
            if (!Plans.TryGetValue(dto.Plan, out var planInfo))
                return ApiResponseT<InitiatePaymentResponseDto>.FailureResponse("Invalid plan.");

            var user = await _uow.Read<ApplicationUser>()
                .GetSingleAsync(u => u.Id == _currentUser.UserId, ct);
            if (user is null)
                return ApiResponseT<InitiatePaymentResponseDto>.FailureResponse("User not found.");

            // Resolve the public backend URL (ngrok tunnel or configured fallback)
            var backendPublicUrl = await GetBackendPublicUrlAsync();

            // Step 1: Auth token
            var authToken = await GetAuthTokenAsync();

            // Step 2: Order registration
            var orderId = await RegisterOrderAsync(authToken, planInfo.AmountCents, dto.Plan);

            // Step 3: Payment key — pass redirect_url so Paymob knows where to send the user after payment
            var redirectUrl = string.IsNullOrEmpty(backendPublicUrl)
                ? null
                : $"{backendPublicUrl}/api/subscription/webhook";
            var paymentKey = await GetPaymentKeyAsync(authToken, orderId, planInfo.AmountCents, user, redirectUrl);

            var paymentUrl = $"https://accept.paymob.com/api/acceptance/iframes/{IframeId}?payment_token={paymentKey}";

            return ApiResponseT<InitiatePaymentResponseDto>.SuccessResponse(
                new InitiatePaymentResponseDto { PaymentUrl = paymentUrl });
        }

        private async Task<string> GetBackendPublicUrlAsync()
        {
            // 1. Try ngrok's local API (when running inside Docker)
            try
            {
                var json = await _http.GetStringAsync("http://ngrok:4040/api/tunnels");
                using var doc = JsonDocument.Parse(json);
                foreach (var tunnel in doc.RootElement.GetProperty("tunnels").EnumerateArray())
                {
                    var url = tunnel.GetProperty("public_url").GetString() ?? "";
                    if (url.StartsWith("https://"))
                        return url;
                }
            }
            catch { /* ngrok not running */ }

            // 2. Fall back to appsettings value
            return _config["AppSettings:BackendPublicUrl"] ?? "";
        }

        public async Task<ApiResponse> HandleWebhookAsync(string rawBody, string hmacHeader, CancellationToken ct)
        {
            // TODO: Re-enable HMAC verification in production
            // if (!VerifyHmac(rawBody, hmacHeader))
            //     return ApiResponse.FailureResponse("Invalid webhook signature.");

            using var doc = JsonDocument.Parse(rawBody);
            var obj = doc.RootElement;

            var success = obj.GetProperty("obj").GetProperty("success").GetBoolean();
            if (!success)
                return ApiResponse.SuccessResponse("Payment not successful, ignored.");

            var orderId = obj.GetProperty("obj").GetProperty("order").GetProperty("id").GetInt64();
            var merchantOrderId = obj.GetProperty("obj").GetProperty("order")
                .GetProperty("merchant_order_id").GetString() ?? "";

            // merchant_order_id format: "{userId}_{plan}_{timestamp}"
            var parts = merchantOrderId.Split('_');
            if (parts.Length < 2)
                return ApiResponse.FailureResponse("Invalid order format.");

            var userId = parts[0];
            var planName = parts[1];

            if (!Plans.TryGetValue(planName, out var planInfo))
                return ApiResponse.FailureResponse("Invalid plan in order.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return ApiResponse.FailureResponse("User not found.");

            user.SubscriptionPlan = planInfo.Plan;
            await _userManager.UpdateAsync(user);

            // Also upgrade the tenant so all members see the new plan
            var tenant = await _uow.Read<Tenant>().GetSingleAsync(t => t.Id == user.TenantId, ct);
            if (tenant is not null)
            {
                tenant.UpdatePlan(planInfo.Plan);
                await _uow.Write<Tenant>().UpdateAsync(tenant, ct);
                await _uow.SaveChangesAsync();
            }

            return ApiResponse.SuccessResponse("Subscription upgraded successfully.");
        }

        private async Task<string> GetAuthTokenAsync()
        {
            var body = JsonSerializer.Serialize(new { api_key = ApiKey });
            var response = await _http.PostAsync(
                "https://accept.paymob.com/api/auth/tokens",
                new StringContent(body, Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("token").GetString()!;
        }

        private async Task<long> RegisterOrderAsync(string authToken, int amountCents, string planName)
        {
            var merchantOrderId = $"{_currentUser.UserId}_{planName}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

            var body = JsonSerializer.Serialize(new
            {
                auth_token = authToken,
                delivery_needed = false,
                amount_cents = amountCents,
                currency = "EGP",
                merchant_order_id = merchantOrderId,
                items = Array.Empty<object>()
            });

            var response = await _http.PostAsync(
                "https://accept.paymob.com/api/ecommerce/orders",
                new StringContent(body, Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("id").GetInt64();
        }

        private async Task<string> GetPaymentKeyAsync(string authToken, long orderId, int amountCents, ApplicationUser user, string? redirectUrl = null)
        {
            var payload = new Dictionary<string, object>
            {
                ["auth_token"]     = authToken,
                ["amount_cents"]   = amountCents,
                ["expiration"]     = 3600,
                ["order_id"]       = orderId,
                ["currency"]       = "EGP",
                ["integration_id"] = int.Parse(IntegrationId),
                ["billing_data"]   = new
                {
                    first_name      = user.FullName,
                    last_name       = ".",
                    email           = user.Email,
                    phone_number    = user.PhoneNumber ?? "N/A",
                    apartment       = "N/A",
                    floor           = "N/A",
                    street          = "N/A",
                    building        = "N/A",
                    shipping_method = "N/A",
                    postal_code     = "N/A",
                    city            = "N/A",
                    country         = "EG",
                    state           = "N/A"
                }
            };

            if (!string.IsNullOrEmpty(redirectUrl))
                payload["redirect_url"] = redirectUrl;

            var body = JsonSerializer.Serialize(payload);

            var response = await _http.PostAsync(
                "https://accept.paymob.com/api/acceptance/payment_keys",
                new StringContent(body, Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("token").GetString()!;
        }

        public async Task<ApiResponse> HandleWebhookGetAsync(string merchantOrderId, string hmac, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(merchantOrderId))
                return ApiResponse.FailureResponse("Invalid order.");

            // merchant_order_id format: "{userId}_{plan}_{timestamp}"
            var parts = merchantOrderId.Split('_');
            if (parts.Length < 2)
                return ApiResponse.FailureResponse("Invalid order format.");

            var userId = parts[0];
            var planName = parts[1];

            if (!Plans.TryGetValue(planName, out var planInfo))
                return ApiResponse.FailureResponse("Invalid plan.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return ApiResponse.FailureResponse("User not found.");

            user.SubscriptionPlan = planInfo.Plan;
            await _userManager.UpdateAsync(user);

            return ApiResponse.SuccessResponse("Subscription upgraded successfully.");
        }

        private bool VerifyHmac(string rawBody, string receivedHmac)
        {
            using var doc = JsonDocument.Parse(rawBody);
            var obj = doc.RootElement.GetProperty("obj");

            var data = string.Concat(
                obj.GetProperty("amount_cents").GetInt64(),
                obj.GetProperty("created_at").GetString(),
                obj.GetProperty("currency").GetString(),
                obj.GetProperty("error_occured").GetBoolean(),
                obj.GetProperty("has_parent_transaction").GetBoolean(),
                obj.GetProperty("id").GetInt64(),
                obj.GetProperty("integration_id").GetInt64(),
                obj.GetProperty("is_3d_secure").GetBoolean(),
                obj.GetProperty("is_auth").GetBoolean(),
                obj.GetProperty("is_capture").GetBoolean(),
                obj.GetProperty("is_refunded").GetBoolean(),
                obj.GetProperty("is_standalone_payment").GetBoolean(),
                obj.GetProperty("is_voided").GetBoolean(),
                obj.GetProperty("order").GetProperty("id").GetInt64(),
                obj.GetProperty("owner").GetInt64(),
                obj.GetProperty("pending").GetBoolean(),
                obj.GetProperty("source_data").GetProperty("pan").GetString(),
                obj.GetProperty("source_data").GetProperty("sub_type").GetString(),
                obj.GetProperty("source_data").GetProperty("type").GetString(),
                obj.GetProperty("success").GetBoolean()
            );

            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(HmacSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var computed = Convert.ToHexString(hash).ToLower();

            return computed == receivedHmac.ToLower();
        }
    }
}
