using DealTrack.Application.Common;
using DealTrack.Application.Contracts;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IOcrService
    {
        Task<ApiResponseT<OcrCompleted>> ProcessImageAsync(OcrRequested request, CancellationToken ct = default);
    }
}
