using ClosedXML.Excel;
using DealTrack.Application.DTOs.Clients;
using DealTrack.Application.Interfaces;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.IO.Compression;

namespace DealTrack.Infrastructure.Services
{
    public class ExcelImportService : IExcelImportService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        private const long MaxFileSize = 5 * 1024 * 1024;
        private const int MaxRows = 1000;
        private static readonly byte[] XlsxMagicBytes = [0x50, 0x4B, 0x03, 0x04];
        private static readonly char[] ForbiddenFirstChars = ['=', '+', '-', '@', '\t', '\r'];

        public ExcelImportService(IUnitOfWork uow, ICurrentUserService currentUser)
        {
            _uow = uow;
            _currentUser = currentUser;
        }

        public async Task<ImportResultDto> ImportClientsAsync(IFormFile file, CancellationToken ct = default)
        {
            var result = new ImportResultDto();

            // Layer 1: Extension
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx")
                throw new InvalidOperationException("Only .xlsx files are allowed.");

            // Layer 2: File size
            if (file.Length > MaxFileSize)
                throw new InvalidOperationException("File size exceeds the 5MB limit.");

            // Layer 3 + 4 + 5: Read into memory, validate bytes + ZIP contents
            using var memStream = new MemoryStream();
            await file.CopyToAsync(memStream, ct);
            memStream.Position = 0;

            ValidateMagicBytes(memStream);
            memStream.Position = 0;
            ValidateZipContents(memStream);
            memStream.Position = 0;

            // Layer 6-9: Parse and validate data
            using var workbook = new XLWorkbook(memStream);
            var sheet = workbook.Worksheets.First();
            var rows = sheet.RowsUsed().Skip(1).ToList();

            if (rows.Count > MaxRows)
                throw new InvalidOperationException($"File exceeds the maximum of {MaxRows} rows.");

            var tenantId = _currentUser.TenantId;
            var userId = _currentUser.UserId;

            var existingPhones = (await _uow.Read<Client>()
                .ListAsync(c => c.TenantId == tenantId, ct))
                .Select(c => c.Phone)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var seenPhonesInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var rowNumber = row.RowNumber();

                var name  = SanitizeCell(row.Cell(1).GetString(), rowNumber, "Name",  result);
                var phone = SanitizeCell(row.Cell(2).GetString(), rowNumber, "Phone", result);
                var notes = row.Cell(3).GetString().Trim();

                if (name is null || phone is null)
                {
                    result.Skipped++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    result.Errors.Add($"Row {rowNumber}: Name is required.");
                    result.Skipped++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(phone))
                {
                    result.Errors.Add($"Row {rowNumber}: Phone is required.");
                    result.Skipped++;
                    continue;
                }

                if (existingPhones.Contains(phone) || seenPhonesInFile.Contains(phone))
                {
                    result.Errors.Add($"Row {rowNumber}: Phone '{phone}' already exists.");
                    result.Skipped++;
                    continue;
                }

                seenPhonesInFile.Add(phone);

                var client = new Client(tenantId, name, phone, notes, userId);
                await _uow.Write<Client>().AddAsync(client, ct);
                result.Imported++;
            }

            if (result.Imported > 0)
                await _uow.SaveChangesAsync();

            return result;
        }

        private static void ValidateMagicBytes(Stream stream)
        {
            var buffer = new byte[4];
            var read = stream.Read(buffer, 0, 4);
            if (read < 4 || !buffer.SequenceEqual(XlsxMagicBytes))
                throw new InvalidOperationException("File is not a valid Excel file.");
        }

        private static void ValidateZipContents(Stream stream)
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

            var forbiddenEntries = new[] { "vbaProject.bin", "activeX", "externalLinks" };

            foreach (var entry in zip.Entries)
            {
                foreach (var forbidden in forbiddenEntries)
                {
                    if (entry.FullName.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            $"File contains forbidden content: {forbidden}. Upload rejected for security reasons.");
                }
            }
        }

        private static string? SanitizeCell(string raw, int row, string field, ImportResultDto result)
        {
            var value = raw.Trim();

            if (string.IsNullOrEmpty(value))
                return value;

            // Layer 6: Formula injection prevention
            if (ForbiddenFirstChars.Contains(value[0]))
            {
                result.Errors.Add($"Row {row}: {field} contains forbidden characters (formula injection attempt).");
                return null;
            }

            // Layer 7: Max length
            if (value.Length > 200)
            {
                result.Errors.Add($"Row {row}: {field} exceeds maximum length of 200 characters.");
                return null;
            }

            return value;
        }
    }
}
