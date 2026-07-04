using DealTrack.Application.Common;
using DealTrack.Application.Contracts;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    public class ProcessImageRequest
    {
        public IFormFile Image { get; set; } = null!;
        public string ClientId { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OcrController : ControllerBase
    {
        private readonly IOcrService _ocrService;

        public OcrController(IOcrService ocrService)
        {
            _ocrService = ocrService;
        }

        [HttpPost("process")]
        [Consumes("multipart/form-data")]
        public async Task<ApiResponseT<OcrCompleted>> ProcessImage(
            [FromForm] ProcessImageRequest request,
            CancellationToken ct)
        {
            using var ms = new MemoryStream();
            await request.Image.CopyToAsync(ms, ct);
            var base64 = Convert.ToBase64String(ms.ToArray());

            var ocrRequest = new OcrRequested
            {
                ImageBase64 = base64,
                ClientId = request.ClientId,
                FileName = request.Image.FileName
            };

            return await _ocrService.ProcessImageAsync(ocrRequest, ct);
        }
    }
}
