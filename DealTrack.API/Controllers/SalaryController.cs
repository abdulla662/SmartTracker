using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Salary;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SalaryController : ControllerBase
    {
        private readonly ISalaryService _salaryService;

        public SalaryController(ISalaryService salaryService)
        {
            _salaryService = salaryService;
        }

        [HttpPost("base")]
        public async Task<ApiResponse> SetBaseSalary(SetBaseSalaryDto dto, CancellationToken ct)
            => await _salaryService.SetBaseSalaryAsync(dto, ct);

        [HttpPost("adjustment")]
        public async Task<ApiResponse> AddAdjustment(AddAdjustmentDto dto, CancellationToken ct)
            => await _salaryService.AddAdjustmentAsync(dto, ct);

        [HttpDelete("adjustment/{id:guid}")]
        public async Task<ApiResponse> DeleteAdjustment(Guid id, CancellationToken ct)
            => await _salaryService.DeleteAdjustmentAsync(id, ct);

        [HttpGet]
        public async Task<ApiResponseT<List<SalaryMemberDto>>> GetSalaries(
            [FromQuery] int month, [FromQuery] int year, CancellationToken ct)
            => await _salaryService.GetSalariesAsync(month, year, ct);

        [HttpGet("my")]
        public async Task<ApiResponseT<SalaryMemberDto>> GetMySalary(
            [FromQuery] int month, [FromQuery] int year, CancellationToken ct)
            => await _salaryService.GetMySalaryAsync(month, year, ct);
    }
}
