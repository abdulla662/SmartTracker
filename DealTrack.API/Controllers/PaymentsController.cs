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

        [HttpGet]
        public async Task<ApiResponseT<PagedResult<PaymentResponseDto>>> GetAll(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
            => await _paymentService.GetAllPaymentsAsync(page, pageSize, ct);

        [HttpGet("client/{clientId:guid}")]
        public async Task<ApiResponseT<PagedResult<PaymentResponseDto>>> GetForClient(
            Guid clientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            return await _paymentService.GetPaymentsForClientAsync(clientId, page, pageSize, ct);
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
