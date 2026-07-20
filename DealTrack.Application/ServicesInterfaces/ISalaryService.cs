using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Salary;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface ISalaryService
    {
        Task<ApiResponse> SetBaseSalaryAsync(SetBaseSalaryDto dto, CancellationToken ct = default);
        Task<ApiResponse> AddAdjustmentAsync(AddAdjustmentDto dto, CancellationToken ct = default);
        Task<ApiResponse> DeleteAdjustmentAsync(Guid id, CancellationToken ct = default);
        Task<ApiResponseT<List<SalaryMemberDto>>> GetSalariesAsync(int month, int year, CancellationToken ct = default);
        Task<ApiResponseT<SalaryMemberDto>> GetMySalaryAsync(int month, int year, CancellationToken ct = default);
    }
}
