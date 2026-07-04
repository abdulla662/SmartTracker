using DealTrack.Application.Common;
using DealTrack.Application.Contracts;
using DealTrack.Application.ServicesInterfaces;
using MassTransit;

namespace DealTrack.Infrastructure.Services
{
    public class OcrService : IOcrService
    {
        private readonly IRequestClient<OcrRequested> _client;

        public OcrService(IRequestClient<OcrRequested> client)
        {
            _client = client;
        }

        public async Task<ApiResponseT<OcrCompleted>> ProcessImageAsync(OcrRequested request, CancellationToken ct = default)
        {
            var response = await _client.GetResponse<OcrCompleted>(request, ct);
            var result = response.Message;

            if (!result.Success)
                return ApiResponseT<OcrCompleted>.FailureResponse(result.ErrorMessage ?? "OCR failed.");

            return ApiResponseT<OcrCompleted>.SuccessResponse(result);
        }
    }
}
