using DealTrack.Application.Common;
using DealTrack.Application.Contracts;
using DealTrack.Application.DTOs.Ocr;
using Microsoft.Extensions.Logging;
using DealTrack.Application.Interfaces;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using MassTransit;
using System.Text.RegularExpressions;
using System.Net;

namespace DealTrack.Infrastructure.Services
{
    public class OcrService : IOcrService
    {
        private readonly IRequestClient<OcrRequested> _client;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<OcrService> _logger;

        public OcrService(IRequestClient<OcrRequested> client, IUnitOfWork uow, ICurrentUserService currentUser, ILogger<OcrService> logger)
        {
            _client = client;
            _uow = uow;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<ApiResponseT<List<OcrClientDto>>> ProcessImageAsync(OcrRequested request, CancellationToken ct = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromMinutes(2));

            var response = await _client.GetResponse<OcrCompleted>(request, cts.Token);
            var result = response.Message;

            _logger.LogInformation("[OCR] Success={S} ExtractedText='{T}' Error='{E}'",
                result.Success, result.ExtractedText, result.ErrorMessage);

            if (!result.Success)
                return ApiResponseT<List<OcrClientDto>>.FailureResponse(result.ErrorMessage ?? "OCR failed.");

            var clients = ParseClients(result.ExtractedText);
            _logger.LogInformation("[OCR] ParseClients found {N} clients", clients.Count);

            if (clients.Count == 0)
                return ApiResponseT<List<OcrClientDto>>.FailureResponse("No clients found in the image.");

            return ApiResponseT<List<OcrClientDto>>.SuccessResponse(clients);
        }

        public async Task<ApiResponseT<int>> SaveClientsAsync(List<OcrClientDto> clients, CancellationToken ct = default)
        {
            if (clients == null || clients.Count == 0)
                return ApiResponseT<int>.FailureResponse("No clients to save.");

            var tenantId = _currentUser.TenantId;
            var userId   = _currentUser.UserId;

            var phoneRegex = new Regex(@"^(01[0-9]{9}|\+20[0-9]{10}|[0-9]{7,15})$");
            var seenPhones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int saved = 0;

            foreach (var dto in clients)
            {
                // Sanitize: strip HTML/script tags, decode entities, trim
                var name  = SanitizeText(dto.Name,  maxLength: 100);
                var phone = SanitizeText(dto.Phone, maxLength: 20);

                // Must have at least a name
                if (string.IsNullOrWhiteSpace(name)) continue;

                // Validate phone format if provided
                if (!string.IsNullOrWhiteSpace(phone) && !phoneRegex.IsMatch(phone)) continue;

                // Deduplicate within this batch
                if (!string.IsNullOrWhiteSpace(phone) && !seenPhones.Add(phone)) continue;

                // Deduplicate against existing DB records for this tenant
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    var exists = await _uow.Read<Client>()
                        .AnyAsync(c => c.TenantId == tenantId && c.Phone == phone, ct);
                    if (exists)
                    {
                        _logger.LogInformation("[OCR] Skipping duplicate phone {Phone}", phone);
                        continue;
                    }
                }

                var client = new Client(tenantId, name, phone, null, userId);
                await _uow.Write<Client>().AddAsync(client, ct);
                saved++;
            }

            if (saved == 0)
                return ApiResponseT<int>.FailureResponse("All entries were duplicates or invalid — nothing saved.");

            await _uow.SaveChangesAsync();
            return ApiResponseT<int>.SuccessResponse(saved);
        }

        public async Task<ApiResponseT<List<string>>> CheckExistingPhonesAsync(List<string> phones, CancellationToken ct = default)
        {
            if (phones == null || phones.Count == 0)
                return ApiResponseT<List<string>>.SuccessResponse(new List<string>());

            var tenantId = _currentUser.TenantId;
            var existing = new List<string>();

            foreach (var phone in phones.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                var trimmed = phone.Trim();
                var found = await _uow.Read<Client>()
                    .AnyAsync(c => c.TenantId == tenantId && c.Phone == trimmed, ct);
                if (found) existing.Add(trimmed);
            }

            return ApiResponseT<List<string>>.SuccessResponse(existing);
        }

        private static string SanitizeText(string? input, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            // Decode HTML entities first (e.g. &lt; → <)
            var decoded = WebUtility.HtmlDecode(input);

            // Strip any HTML/script tags
            var stripped = Regex.Replace(decoded, @"<[^>]*>", string.Empty);

            // Remove characters that have no place in a name or phone: only allow letters, digits, spaces, +, -, ()
            var cleaned = Regex.Replace(stripped, @"[^\p{L}\p{N}\s\+\-\(\)\.]", string.Empty);

            return cleaned.Trim().Length > maxLength
                ? cleaned.Trim()[..maxLength]
                : cleaned.Trim();
        }

        private static List<OcrClientDto> ParseClients(string text)
        {
            var result = new List<OcrClientDto>();
            var phoneRegex = new Regex(@"(01[0-9]{9}|\+20[0-9]{10}|[0-9]{7,15})");

            foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                var phoneMatch = phoneRegex.Match(trimmed);
                var phone = phoneMatch.Success ? phoneMatch.Value : "";
                var name = (string.IsNullOrEmpty(phone) ? trimmed : trimmed.Replace(phone, ""))
                    .Replace("-", "").Replace("|", "").Trim();

                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(phone)) continue;

                result.Add(new OcrClientDto { Name = name, Phone = phone });
            }

            return result;
        }
    }
}
