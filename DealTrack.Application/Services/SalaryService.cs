using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Salary;
using DealTrack.Application.Helpers;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using Microsoft.Extensions.Localization;
using System.Net;

namespace DealTrack.Application.Services
{
    public class SalaryService : ISalaryService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly INotificationService _notifications;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public SalaryService(IUnitOfWork uow, ICurrentUserService currentUser, INotificationService notifications, IStringLocalizer<SharedResource> localizer)
        {
            _uow = uow;
            _currentUser = currentUser;
            _notifications = notifications;
            _localizer = localizer;
        }

        private async Task NotifyAccountantsAsync(Guid tenantId, string titleKey, string msgKey, string[] args, string link, CancellationToken ct)
        {
            var accountants = await _uow.Read<ApplicationUser>()
                .ListAsync(u => u.TenantId == tenantId && u.Role == UserRole.Accountant && u.IsApproved, ct);
            foreach (var acc in accountants)
                await _notifications.CreateAsync(
                    Guid.Parse(acc.Id), tenantId,
                    NotifKey.Build(titleKey),
                    NotifKey.Build(msgKey, args, link),
                    NotificationType.SystemNotification, ct);
        }

        public async Task<ApiResponse> SetBaseSalaryAsync(SetBaseSalaryDto dto, CancellationToken ct = default)
        {
            if (_currentUser.Role != UserRole.Admin)
                return ApiResponse.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Forbidden);

            if (dto.BaseSalary < 0)
                return ApiResponse.FailureResponse(_localizer["SalaryMustBePositive"]);

            var targetUser = await _uow.Read<ApplicationUser>().GetSingleAsync(
                u => u.Id == dto.UserId && u.TenantId == _currentUser.TenantId, ct);
            if (targetUser == null)
                return ApiResponse.FailureResponse(_localizer["UserNotFound"], HttpStatusCode.NotFound);

            var existing = await _uow.Read<MonthlySalary>().GetSingleAsync(
                s => s.UserId == dto.UserId && s.Month == dto.Month && s.Year == dto.Year && s.TenantId == _currentUser.TenantId, ct);

            if (existing != null)
            {
                existing.Update(dto.BaseSalary, _currentUser.UserId);
                await _uow.Write<MonthlySalary>().UpdateAsync(existing, ct);
            }
            else
            {
                var salary = new MonthlySalary(dto.UserId, _currentUser.TenantId, dto.BaseSalary, dto.Month, dto.Year, _currentUser.UserId);
                await _uow.Write<MonthlySalary>().AddAsync(salary, ct);
            }

            await _uow.SaveChangesAsync();

            var adminUser = await _uow.Read<ApplicationUser>().GetSingleAsync(u => u.Id == _currentUser.UserId, ct);
            var adminName = adminUser?.FullName ?? "";
            var period = $"{dto.Month}/{dto.Year}";

            await _notifications.CreateAsync(
                Guid.Parse(dto.UserId), _currentUser.TenantId,
                NotifKey.Build("notif.title.salaryUpdated"),
                NotifKey.Build("notif.msg.salaryUpdated", new[] { adminName, dto.BaseSalary.ToString("F0"), period }, "/salary"),
                NotificationType.SystemNotification, ct);

            await NotifyAccountantsAsync(_currentUser.TenantId,
                "notif.title.salaryUpdated",
                "notif.msg.salaryUpdated", new[] { adminName, dto.BaseSalary.ToString("F0"), period }, "/salary", ct);

            return ApiResponse.SuccessResponse(message: _localizer["SalarySaved"]);
        }

        public async Task<ApiResponse> AddAdjustmentAsync(AddAdjustmentDto dto, CancellationToken ct = default)
        {
            if (_currentUser.Role != UserRole.Admin)
                return ApiResponse.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Forbidden);

            if (dto.Amount <= 0)
                return ApiResponse.FailureResponse(_localizer["SalaryMustBePositive"]);

            var targetUser = await _uow.Read<ApplicationUser>().GetSingleAsync(
                u => u.Id == dto.UserId && u.TenantId == _currentUser.TenantId, ct);
            if (targetUser == null)
                return ApiResponse.FailureResponse(_localizer["UserNotFound"], HttpStatusCode.NotFound);

            var adj = new SalaryAdjustment(dto.UserId, _currentUser.TenantId, dto.Month, dto.Year, dto.Amount, dto.Type, dto.Description, _currentUser.UserId);
            await _uow.Write<SalaryAdjustment>().AddAsync(adj, ct);
            await _uow.SaveChangesAsync();

            var adminUser = await _uow.Read<ApplicationUser>().GetSingleAsync(u => u.Id == _currentUser.UserId, ct);
            var adminName = adminUser?.FullName ?? "";
            var typeKey = dto.Type == AdjustmentType.Bonus ? "notif.msg.salaryBonus" : "notif.msg.salaryDeduction";

            await _notifications.CreateAsync(
                Guid.Parse(dto.UserId), _currentUser.TenantId,
                NotifKey.Build("notif.title.salaryAdjusted"),
                NotifKey.Build(typeKey, new[] { adminName, dto.Amount.ToString("F0"), dto.Description }, "/salary"),
                NotificationType.SystemNotification, ct);

            await NotifyAccountantsAsync(_currentUser.TenantId,
                "notif.title.salaryAdjusted",
                typeKey, new[] { adminName, dto.Amount.ToString("F0"), dto.Description }, "/salary", ct);

            return ApiResponse.SuccessResponse(message: _localizer["AdjustmentAdded"]);
        }

        public async Task<ApiResponse> DeleteAdjustmentAsync(Guid id, CancellationToken ct = default)
        {
            if (_currentUser.Role != UserRole.Admin)
                return ApiResponse.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Forbidden);

            var adj = await _uow.Read<SalaryAdjustment>().GetByIdAsync(id, ct);
            if (adj == null || adj.TenantId != _currentUser.TenantId)
                return ApiResponse.FailureResponse(_localizer["NotFound"], HttpStatusCode.NotFound);

            await _uow.SoftDelete<SalaryAdjustment>().SoftDeleteAsync(adj, ct);
            await _uow.SaveChangesAsync();
            return ApiResponse.SuccessResponse(message: _localizer["AdjustmentDeleted"]);
        }

        public async Task<ApiResponseT<List<SalaryMemberDto>>> GetSalariesAsync(int month, int year, CancellationToken ct = default)
        {
            var role = _currentUser.Role;
            var tenantId = _currentUser.TenantId;

            if (role != UserRole.Admin && role != UserRole.Accountant)
                return ApiResponseT<List<SalaryMemberDto>>.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Forbidden);

            var members = await _uow.Read<ApplicationUser>().ListAsync(
                u => u.TenantId == tenantId && (u.Role == UserRole.TeamLead || u.Role == UserRole.Sales), ct);

            var currentSalaries = await _uow.Read<MonthlySalary>().ListAsync(
                s => s.TenantId == tenantId && s.Month == month && s.Year == year, ct);

            // For members without a record this month, carry forward the most recent base salary
            var memberIds = members.Select(m => m.Id).ToList();
            var idsWithSalary = currentSalaries.Select(s => s.UserId).ToHashSet();
            var idsWithoutSalary = memberIds.Where(id => !idsWithSalary.Contains(id)).ToList();
            List<MonthlySalary> previousSalaries = new();
            if (idsWithoutSalary.Count > 0)
            {
                previousSalaries = await _uow.Read<MonthlySalary>().ListAsync(
                    s => s.TenantId == tenantId
                         && idsWithoutSalary.Contains(s.UserId)
                         && (s.Year < year || (s.Year == year && s.Month < month)), ct);
            }
            var salaryMap = new Dictionary<string, decimal>();
            foreach (var s in currentSalaries) salaryMap[s.UserId] = s.BaseSalary;
            foreach (var id in idsWithoutSalary)
            {
                var mostRecent = previousSalaries
                    .Where(s => s.UserId == id)
                    .OrderByDescending(s => s.Year).ThenByDescending(s => s.Month)
                    .FirstOrDefault();
                salaryMap[id] = mostRecent?.BaseSalary ?? 0;
            }

            var adjustments = await _uow.Read<SalaryAdjustment>().ListAsync(
                a => a.TenantId == tenantId && a.Month == month && a.Year == year, ct);

            var adjCreatorIds = adjustments.Select(a => a.CreatedByUserId).Distinct().ToList();
            var adjCreators = await _uow.Read<ApplicationUser>().ListAsync(u => adjCreatorIds.Contains(u.Id), ct);
            var creatorMap = adjCreators.ToDictionary(u => u.Id, u => u.FullName ?? u.UserName ?? "");

            var result = members.Select(member =>
            {
                var memberAdj = adjustments.Where(a => a.UserId == member.Id).ToList();
                var bonuses = memberAdj.Where(a => a.Type == AdjustmentType.Bonus).Sum(a => a.Amount);
                var deductions = memberAdj.Where(a => a.Type == AdjustmentType.Deduction).Sum(a => a.Amount);
                var baseSalary = salaryMap.GetValueOrDefault(member.Id, 0);

                return new SalaryMemberDto
                {
                    UserId = member.Id,
                    UserName = member.FullName ?? member.UserName ?? "",
                    UserRole = member.Role.ToString(),
                    Month = month,
                    Year = year,
                    BaseSalary = baseSalary,
                    TotalBonuses = bonuses,
                    TotalDeductions = deductions,
                    NetSalary = baseSalary + bonuses - deductions,
                    Adjustments = memberAdj.Select(a => new SalaryAdjustmentDto
                    {
                        Id = a.Id,
                        Amount = a.Amount,
                        Type = a.Type,
                        Description = a.Description,
                        CreatedByUserName = creatorMap.GetValueOrDefault(a.CreatedByUserId, ""),
                        CreatedAt = a.CreatedAt
                    }).OrderByDescending(a => a.CreatedAt).ToList()
                };
            }).OrderBy(m => m.UserName).ToList();

            return ApiResponseT<List<SalaryMemberDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponseT<SalaryMemberDto>> GetMySalaryAsync(int month, int year, CancellationToken ct = default)
        {
            var userId = _currentUser.UserId;
            var tenantId = _currentUser.TenantId;
            var user = await _uow.Read<ApplicationUser>().GetSingleAsync(u => u.Id == userId, ct);

            var salary = await _uow.Read<MonthlySalary>().GetSingleAsync(
                s => s.UserId == userId && s.Month == month && s.Year == year && s.TenantId == tenantId, ct);

            // Carry forward base salary from most recent previous month if not set this month
            decimal baseSalary = salary?.BaseSalary ?? 0;
            if (salary == null)
            {
                var previous = await _uow.Read<MonthlySalary>().ListAsync(
                    s => s.UserId == userId && s.TenantId == tenantId
                         && (s.Year < year || (s.Year == year && s.Month < month)), ct);
                var mostRecent = previous.OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).FirstOrDefault();
                if (mostRecent != null) baseSalary = mostRecent.BaseSalary;
            }

            var adjustments = await _uow.Read<SalaryAdjustment>().ListAsync(
                a => a.UserId == userId && a.Month == month && a.Year == year && a.TenantId == tenantId, ct);

            var adjCreatorIds = adjustments.Select(a => a.CreatedByUserId).Distinct().ToList();
            var adjCreators = await _uow.Read<ApplicationUser>().ListAsync(u => adjCreatorIds.Contains(u.Id), ct);
            var creatorMap = adjCreators.ToDictionary(u => u.Id, u => u.FullName ?? u.UserName ?? "");

            var bonuses = adjustments.Where(a => a.Type == AdjustmentType.Bonus).Sum(a => a.Amount);
            var deductions = adjustments.Where(a => a.Type == AdjustmentType.Deduction).Sum(a => a.Amount);

            return ApiResponseT<SalaryMemberDto>.SuccessResponse(new SalaryMemberDto
            {
                UserId = userId,
                UserName = user?.FullName ?? user?.UserName ?? "",
                UserRole = user?.Role.ToString() ?? "",
                Month = month,
                Year = year,
                BaseSalary = baseSalary,
                TotalBonuses = bonuses,
                TotalDeductions = deductions,
                NetSalary = baseSalary + bonuses - deductions,
                Adjustments = adjustments.Select(a => new SalaryAdjustmentDto
                {
                    Id = a.Id,
                    Amount = a.Amount,
                    Type = a.Type,
                    Description = a.Description,
                    CreatedByUserName = creatorMap.GetValueOrDefault(a.CreatedByUserId, ""),
                    CreatedAt = a.CreatedAt
                }).OrderByDescending(a => a.CreatedAt).ToList()
            });
        }
    }
}
