using DealTrack.Application.Common;
using DealTrack.Application.DTOs.TransferRequest;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "TeamLead")]
    public class TransferRequestsController : ControllerBase
    {
        private readonly ITransferRequestService _transferRequestService;

        public TransferRequestsController(ITransferRequestService transferRequestService)
        {
            _transferRequestService = transferRequestService;
        }

        [HttpPost]
        public async Task<ApiResponseT<TransferRequestResponseDto>> CreateTransferRequest(
            CreateTransferRequestDto dto, CancellationToken ct)
            => await _transferRequestService.CreateTransferRequestAsync(dto, ct);

        [HttpPut("{id}/respond")]
        public async Task<ApiResponse> RespondToTransferRequest(
            Guid id, RespondTransferRequestDto dto, CancellationToken ct)
            => await _transferRequestService.RespondToTransferRequestAsync(id, dto, ct);

        [HttpGet]
        public async Task<ApiResponseT<List<TransferRequestResponseDto>>> GetMyTransferRequests(CancellationToken ct)
            => await _transferRequestService.GetMyTransferRequestsAsync(ct);
    }
}