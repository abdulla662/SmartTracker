using DealTrack.Application.Common;
using DealTrack.Application.Contracts;
using DealTrack.Application.Interfaces;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using MassTransit;
using System.Text.RegularExpressions;

namespace DealTrack.Infrastructure.Services
{
    public class OcrService : IOcrService
    {
        private readonly IRequestClient<OcrRequested> _client;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public OcrService(IRequestClient<OcrRequested> client, IUnitOfWork uow, ICurrentUserService currentUser)
        {
            _client = client;
            _uow = uow;
            _currentUser = currentUser;
        }

        public async Task<ApiResponseT<OcrCompleted>> ProcessImageAsync(OcrRequested request, CancellationToken ct = default)
        {
            var response = await _client.GetResponse<OcrCompleted>(request, ct);
            var result = response.Message;

            if (!result.Success)
                return ApiResponseT<OcrCompleted>.FailureResponse(result.ErrorMessage ?? "OCR failed.");

            var clients = ParseClients(result.ExtractedText);
            if (clients.Count == 0)
                return ApiResponseT<OcrCompleted>.FailureResponse("No clients found in the image. Make sure each line has a name and phone number.");

            var tenantId = _currentUser.TenantId;
            var userId = _currentUser.UserId;

            foreach (var (name, phone) in clients)
            {
                var client = new Client(tenantId, name, phone, null, userId);
                await _uow.Write<Client>().AddAsync(client, ct);
            }

            await _uow.SaveChangesAsync();

            result.ImportedCount = clients.Count;
            return ApiResponseT<OcrCompleted>.SuccessResponse(result);
        }

        // Parses lines like: "محمد علي 01012345678" or "محمد علي - 01012345678"
        private static List<(string Name, string Phone)> ParseClients(string text)
        {
            var result = new List<(string, string)>();
            var phoneRegex = new Regex(@"(01[0-9]{9}|\+20[0-9]{10}|[0-9]{7,15})");

            foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                var phoneMatch = phoneRegex.Match(trimmed);
                if (!phoneMatch.Success) continue;

                var phone = phoneMatch.Value;
                var name = trimmed.Replace(phone, "")
                                  .Replace("-", "")
                                  .Replace("|", "")
                                  .Trim();

                if (string.IsNullOrWhiteSpace(name)) continue;

                result.Add((name, phone));
            }

            return result;
        }
    }
}
