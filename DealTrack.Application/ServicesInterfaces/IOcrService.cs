using DealTrack.Application.Common;
using DealTrack.Application.Contracts;
using DealTrack.Application.DTOs.Ocr;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IOcrService
    {
        Task<ApiResponseT<List<OcrClientDto>>> ProcessImageAsync(OcrRequested request, CancellationToken ct = default);
        Task<ApiResponseT<int>> SaveClientsAsync(List<OcrClientDto> clients, CancellationToken ct = default);
        Task<ApiResponseT<List<string>>> CheckExistingPhonesAsync(List<string> phones, CancellationToken ct = default);
    }
}
