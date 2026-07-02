using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Clients;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _clientService;

        public ClientsController(IClientService clientService)
        {
            _clientService = clientService;
        }

        [HttpGet]
        public async Task<ApiResponseT<PagedResult<ClientResponseDto>>> GetClients(
            [FromQuery] ClientFilterDto filter, CancellationToken ct)
        {
            return await _clientService.GetClientsAsync(filter, ct);
        }

        [HttpGet("{id:guid}")]
        public async Task<ApiResponseT<ClientResponseDto>> GetClient(Guid id, CancellationToken ct)
        {
            return await _clientService.GetClientByIdAsync(id, ct);
        }

        [HttpPost]
        public async Task<ApiResponseT<ClientResponseDto>> CreateClient(
            [FromBody] CreateClientDto dto, CancellationToken ct)
        {
            return await _clientService.CreateClientAsync(dto, ct);
        }

        [HttpPut("{id:guid}")]
        public async Task<ApiResponse> UpdateClient(
            Guid id, [FromBody] UpdateClientDto dto, CancellationToken ct)
        {
            return await _clientService.UpdateClientAsync(id, dto, ct);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ApiResponse> DeleteClient(Guid id, CancellationToken ct)
        {
            return await _clientService.DeleteClientAsync(id, ct);
        }
        [HttpPut("{id}/reassign")]
        [Authorize(Roles = "Admin,TeamLead")]
        public async Task<ApiResponse> ReassignClient(Guid id, ReassignClientDto dto, CancellationToken ct)
    => await _clientService.ReassignClientAsync(id, dto, ct);
    }
}
