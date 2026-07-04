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
        private readonly IExcelExportService _excelExportService;
        private readonly IExcelImportService _excelImportService;

        public ClientsController(IClientService clientService, IExcelExportService excelExportService, IExcelImportService excelImportService)
        {
            _clientService = clientService;
            _excelExportService = excelExportService;
            _excelImportService = excelImportService;
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

        [HttpGet("export")]
        [Authorize(Policy = "ProOrEnterprise")]
        public async Task<IActionResult> ExportClients(CancellationToken ct)
        {
            var bytes = await _excelExportService.ExportClientsAsync(ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "clients.xlsx");
        }

        [HttpPost("import")]
        [Authorize(Policy = "ProOrEnterprise")]
        public async Task<ApiResponseT<ImportResultDto>> ImportClients(IFormFile file, CancellationToken ct)
        {
            var result = await _excelImportService.ImportClientsAsync(file, ct);
            return ApiResponseT<ImportResultDto>.SuccessResponse(result);
        }
    }
}
