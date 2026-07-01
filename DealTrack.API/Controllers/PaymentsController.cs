using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Payments;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet("client/{clientId:guid}")]
        public async Task<ApiResponseT<List<PaymentResponseDto>>> GetForClient(
            Guid clientId, CancellationToken ct)
        {
            return await _paymentService.GetPaymentsForClientAsync(clientId, ct);
        }

        [HttpPost]
        public async Task<ApiResponseT<PaymentResponseDto>> Create(
            [FromBody] CreatePaymentDto dto, CancellationToken ct)
        {
            return await _paymentService.CreatePaymentAsync(dto, ct);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ApiResponse> Delete(Guid id, CancellationToken ct)
        {
            return await _paymentService.DeletePaymentAsync(id, ct);
        }
        [HttpPut("{id:guid}")]
        public async Task<ApiResponse> UpdatePayment(Guid id, [FromBody] UpdatePaymentDto dto, CancellationToken ct)
        {
            return await _paymentService.UpdatePaymentAsync(id, dto, ct);
        }
    }
}
