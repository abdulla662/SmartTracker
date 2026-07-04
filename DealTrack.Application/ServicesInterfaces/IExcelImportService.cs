using DealTrack.Application.DTOs.Clients;
using Microsoft.AspNetCore.Http;


namespace DealTrack.Application.ServicesInterfaces
{
    public interface IExcelImportService
    {
        Task<ImportResultDto> ImportClientsAsync(IFormFile file, CancellationToken ct = default);
    }
}
