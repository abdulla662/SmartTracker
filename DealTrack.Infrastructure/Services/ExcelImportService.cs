using ClosedXML.Excel;
using DealTrack.Application.DTOs.Clients;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Constants;
using DealTrack.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using System.IO.Compression;

namespace DealTrack.Infrastructure.Services
{
    public class ExcelImportService : IExcelImportService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly IStringLocalizer<SharedResource> _localizer;

        private const long MaxFileSize = 5 * 1024 * 1024;
        private const int MaxRows = 1000;
        private static readonly byte[] XlsxMagicBytes = [0x50, 0x4B, 0x03, 0x04];
        // Only block true formula starters — '+' and '-' are valid phone prefixes
        private static readonly char[] ForbiddenFirstChars = ['=', '@', '\t', '\r'];

        public ExcelImportService(IUnitOfWork uow, ICurrentUserService currentUser, IStringLocalizer<SharedResource> localizer)
        {
            _uow = uow;
            _currentUser = currentUser;
            _localizer = localizer;
        }

        public async Task<ImportResultDto> ImportClientsAsync(IFormFile file, CancellationToken ct = default)
        {
            var result = new ImportResultDto();

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx")
                throw new InvalidOperationException(_localizer["ImportOnlyXlsx"]);

            if (file.Length > MaxFileSize)
                throw new InvalidOperationException(_localizer["ImportFileTooLarge"]);

            using var memStream = new MemoryStream();
            await file.CopyToAsync(memStream, ct);
            memStream.Position = 0;

            ValidateMagicBytes(memStream);
            memStream.Position = 0;
            ValidateZipContents(memStream);
            memStream.Position = 0;

            using var workbook = new XLWorkbook(memStream);
            var sheet = workbook.Worksheets.First();
            var rows  = sheet.RowsUsed().Skip(1).ToList();

            if (rows.Count > MaxRows)
                throw new InvalidOperationException(string.Format(_localizer["ImportTooManyRows"], MaxRows));

            var tenantId      = _currentUser.TenantId;
            var userId        = _currentUser.UserId;
            var userGuid      = Guid.Parse(userId);
            var role          = _currentUser.Role;

            // Pre-load existing phones to detect duplicates
            var existingClients = await _uow.Read<Client>()
                .ListAsync(c => c.TenantId == tenantId, ct);
            var existingPhones = existingClients
                .Select(c => c.Phone)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Plan limit: count clients assigned to this user specifically
            var maxClients   = PlanLimits.GetMaxClients(_currentUser.SubscriptionPlan);
            var currentCount = existingClients.Count(c => c.AssignedToUserId == userId);
            var remaining    = maxClients == int.MaxValue ? int.MaxValue : maxClients - currentCount;

            var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Pre-load team members for TeamLead (email → userId lookup)
            Dictionary<string, string>? teamMembersByEmail = null;
            if (role == UserRole.TeamLead)
            {
                var members = await _uow.Read<ApplicationUser>()
                    .ListAsync(u => u.TeamLeadId == userGuid && u.TenantId == tenantId, ct);
                teamMembersByEmail = members
                    .ToDictionary(u => u.Email!.ToLowerInvariant(), u => u.Id);
            }

            // Pre-load all tenant users for Admin (email → user lookup)
            Dictionary<string, ApplicationUser>? tenantUsersByEmail = null;
            if (role == UserRole.Admin)
            {
                var tenantUsers = await _uow.Read<ApplicationUser>()
                    .ListAsync(u => u.TenantId == tenantId, ct);
                tenantUsersByEmail = tenantUsers
                    .ToDictionary(u => u.Email!.ToLowerInvariant(), u => u);
            }

            foreach (var row in rows)
            {
                var rowNum = row.RowNumber();

                var name  = SanitizeCell(row.Cell(1).GetString(), rowNum, _localizer["FieldName"],  result);
                var phone = SanitizeCell(row.Cell(2).GetString(), rowNum, _localizer["FieldPhone"], result);
                var notes = row.Cell(3).GetString().Trim();

                if (name is null || phone is null) { result.Skipped++; continue; }

                if (string.IsNullOrWhiteSpace(name))
                {
                    result.Errors.Add(new ImportRowError { Row = rowNum, Name = name ?? "", Reason = _localizer["ImportNameRequired"] });
                    result.Skipped++; continue;
                }

                if (string.IsNullOrWhiteSpace(phone))
                {
                    result.Errors.Add(new ImportRowError { Row = rowNum, Name = name, Reason = _localizer["ImportPhoneRequired"] });
                    result.Skipped++; continue;
                }

                if (existingPhones.Contains(phone) || seenInFile.Contains(phone))
                {
                    result.Errors.Add(new ImportRowError { Row = rowNum, Name = name, Reason = string.Format(_localizer["ImportPhoneDuplicate"], phone) });
                    result.Skipped++; continue;
                }

                // Determine who to assign this client to
                string assignedToUserId = userId;

                if (role == UserRole.TeamLead)
                {
                    // Column D (optional): email of a sales member in this team
                    var assignEmail = row.Cell(4).GetString().Trim().ToLowerInvariant();
                    if (!string.IsNullOrWhiteSpace(assignEmail))
                    {
                        if (teamMembersByEmail!.TryGetValue(assignEmail, out var memberId))
                        {
                            assignedToUserId = memberId;
                        }
                        else
                        {
                            result.Errors.Add(new ImportRowError { Row = rowNum, Name = name, Reason = string.Format(_localizer["ImportNotSalesMember"], assignEmail) });
                            result.Skipped++; continue;
                        }
                    }
                }
                else if (role == UserRole.Admin)
                {
                    // Column D (optional): TeamLead email
                    // Column E (optional): Sales email
                    var teamLeadEmail = row.Cell(4).GetString().Trim().ToLowerInvariant();
                    var salesEmail    = row.Cell(5).GetString().Trim().ToLowerInvariant();

                    ApplicationUser? teamLeadUser = null;
                    ApplicationUser? salesUser    = null;

                    if (!string.IsNullOrWhiteSpace(teamLeadEmail))
                    {
                        if (!tenantUsersByEmail!.TryGetValue(teamLeadEmail, out teamLeadUser) || teamLeadUser.Role != UserRole.TeamLead)
                        {
                            result.Errors.Add(new ImportRowError { Row = rowNum, Name = name, Reason = string.Format(_localizer["ImportTeamLeadNotFound"], teamLeadEmail) });
                            result.Skipped++; continue;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(salesEmail))
                    {
                        if (!tenantUsersByEmail!.TryGetValue(salesEmail, out salesUser) || salesUser.Role != UserRole.Sales)
                        {
                            result.Errors.Add(new ImportRowError { Row = rowNum, Name = name, Reason = string.Format(_localizer["ImportSalesNotFound"], salesEmail) });
                            result.Skipped++; continue;
                        }

                        // If both specified, Sales must be under that TeamLead
                        if (teamLeadUser != null && salesUser.TeamLeadId != Guid.Parse(teamLeadUser.Id))
                        {
                            result.Errors.Add(new ImportRowError { Row = rowNum, Name = name, Reason = string.Format(_localizer["ImportSalesNotUnderTeamLead"], salesEmail, teamLeadEmail) });
                            result.Skipped++; continue;
                        }

                        assignedToUserId = salesUser.Id;
                    }
                    else if (teamLeadUser != null)
                    {
                        assignedToUserId = teamLeadUser.Id;
                    }
                }

                // Check plan limit — only applies to the row's assigned user (typically the current user for Sales)
                if (remaining <= 0)
                {
                    result.Errors.Add(new ImportRowError { Row = rowNum, Name = name, Reason = _localizer["ImportPlanLimitReached"] });
                    result.Skipped++;
                    continue;
                }

                seenInFile.Add(phone);
                var client = new Client(tenantId, name, phone, notes, assignedToUserId);
                await _uow.Write<Client>().AddAsync(client, ct);
                result.Imported++;
                remaining--;
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
                throw new InvalidOperationException("File is not a valid Excel (.xlsx) file.");
        }

        private static void ValidateZipContents(Stream stream)
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            var forbidden = new[] { "vbaProject.bin", "activeX", "externalLinks" };
            foreach (var entry in zip.Entries)
                foreach (var f in forbidden)
                    if (entry.FullName.Contains(f, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"File contains forbidden content: {f}.");
        }

        private string? SanitizeCell(string raw, int row, string field, ImportResultDto result)
        {
            var value = raw.Trim();
            if (string.IsNullOrEmpty(value)) return value;

            if (ForbiddenFirstChars.Contains(value[0]))
            {
                result.Errors.Add(new ImportRowError { Row = row, Name = "", Reason = string.Format(_localizer["ImportFormulaInjection"], field) });
                return null;
            }

            if (value.Length > 200)
            {
                result.Errors.Add(new ImportRowError { Row = row, Name = "", Reason = string.Format(_localizer["ImportFieldTooLong"], field) });
                return null;
            }

            return value;
        }
    }
}
