using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Dashboard;
using DealTrack.Application.Interfaces;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Enums;
using DealTrack.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DealTrack.Infrastructure.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly string _connStr;

        public DashboardService(ICurrentUserService currentUser, AppDbContext context)
        {
            _currentUser = currentUser;
            _connStr = context.Database.GetConnectionString()!;
        }

        public async Task<ApiResponseT<DashboardSummaryDto>> GetSummaryAsync(CancellationToken ct = default)
        {
            var userId   = _currentUser.UserId;
            var tenantId = _currentUser.TenantId;
            var role     = _currentUser.Role;

            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync(ct);

            DashboardSummaryDto summary;

            if (role == UserRole.Admin)
                summary = await CallAdminSp(conn, tenantId, ct);
            else if (role == UserRole.TeamLead)
                summary = await CallTeamLeadSp(conn, userId, tenantId, ct);
            else
                summary = await CallSalesSp(conn, userId, tenantId, ct);

            return ApiResponseT<DashboardSummaryDto>.SuccessResponse(summary);
        }

        // ── Sales SP ─────────────────────────────────────────────────────────
        private static async Task<DashboardSummaryDto> CallSalesSp(
            SqlConnection conn, string userId, Guid tenantId, CancellationToken ct)
        {
            await using var cmd = new SqlCommand("sp_GetSalesDashboard", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@UserId",   userId);
            cmd.Parameters.AddWithValue("@TenantId", tenantId);

            await using var rdr = await cmd.ExecuteReaderAsync(ct);

            var dto = new DashboardSummaryDto();

            // RS1: scalars
            if (await rdr.ReadAsync(ct))
                FillScalars(dto, rdr);

            // RS2: monthly revenue
            await rdr.NextResultAsync(ct);
            while (await rdr.ReadAsync(ct))
                dto.MonthlyRevenue.Add(ReadMonthlyRevenue(rdr));

            // RS3: status breakdown
            await rdr.NextResultAsync(ct);
            if (await rdr.ReadAsync(ct))
                FillStatusBreakdown(dto, rdr);

            return dto;
        }

        // ── TeamLead SP ───────────────────────────────────────────────────────
        private static async Task<DashboardSummaryDto> CallTeamLeadSp(
            SqlConnection conn, string userId, Guid tenantId, CancellationToken ct)
        {
            await using var cmd = new SqlCommand("sp_GetTeamLeadDashboard", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@UserId",   userId);
            cmd.Parameters.AddWithValue("@TenantId", tenantId);

            await using var rdr = await cmd.ExecuteReaderAsync(ct);

            var dto = new DashboardSummaryDto();

            if (await rdr.ReadAsync(ct)) FillScalars(dto, rdr);

            await rdr.NextResultAsync(ct);
            while (await rdr.ReadAsync(ct)) dto.MonthlyRevenue.Add(ReadMonthlyRevenue(rdr));

            await rdr.NextResultAsync(ct);
            if (await rdr.ReadAsync(ct)) FillStatusBreakdown(dto, rdr);

            await rdr.NextResultAsync(ct);
            while (await rdr.ReadAsync(ct)) dto.TeamMemberStats.Add(ReadTeamMember(rdr));

            return dto;
        }

        // ── Admin SP ──────────────────────────────────────────────────────────
        private static async Task<DashboardSummaryDto> CallAdminSp(
            SqlConnection conn, Guid tenantId, CancellationToken ct)
        {
            await using var cmd = new SqlCommand("sp_GetAdminDashboard", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@TenantId", tenantId);

            await using var rdr = await cmd.ExecuteReaderAsync(ct);

            var dto = new DashboardSummaryDto();

            if (await rdr.ReadAsync(ct)) FillScalars(dto, rdr);

            await rdr.NextResultAsync(ct);
            while (await rdr.ReadAsync(ct)) dto.MonthlyRevenue.Add(ReadMonthlyRevenue(rdr));

            await rdr.NextResultAsync(ct);
            if (await rdr.ReadAsync(ct)) FillStatusBreakdown(dto, rdr);

            await rdr.NextResultAsync(ct);
            while (await rdr.ReadAsync(ct)) dto.TeamMemberStats.Add(ReadTeamMember(rdr));

            return dto;
        }

        // ── Mapping helpers ───────────────────────────────────────────────────
        private static int SafeInt(SqlDataReader r, string col)
        {
            var i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? 0 : r.GetInt32(i);
        }

        private static decimal SafeDec(SqlDataReader r, string col)
        {
            var i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? 0m : r.GetDecimal(i);
        }

        private static string SafeStr(SqlDataReader r, string col)
        {
            var i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? string.Empty : r.GetString(i);
        }

        private static void FillScalars(DashboardSummaryDto dto, SqlDataReader r)
        {
            dto.TotalClientsCount     = SafeInt(r, "TotalClients");
            dto.TodayFollowUpsCount   = SafeInt(r, "TodayFollowUps");
            dto.OverdueFollowUpsCount = SafeInt(r, "OverdueFollowUps");
            dto.PendingFollowUpsCount = SafeInt(r, "PendingFollowUps");
            dto.CompletedTodayCount   = SafeInt(r, "CompletedToday");
            dto.TotalPaidAmount       = SafeDec(r, "TotalRevenue");
            dto.TotalPaymentsCount    = SafeInt(r, "TotalPayments");
            dto.FollowUpsDone         = SafeInt(r, "FollowUpsDone");
            dto.FollowUpsMissed       = SafeInt(r, "FollowUpsMissed");
        }

        private static void FillStatusBreakdown(DashboardSummaryDto dto, SqlDataReader r)
        {
            dto.PendingFollowUpsCount = SafeInt(r, "Pending");
            dto.FollowUpsDone         = SafeInt(r, "Done");
            dto.FollowUpsMissed       = SafeInt(r, "Missed");
        }

        private static MonthlyRevenueDto ReadMonthlyRevenue(SqlDataReader r) => new()
        {
            Year      = SafeInt(r, "Year"),
            Month     = SafeInt(r, "Month"),
            MonthName = SafeStr(r, "MonthName"),
            Amount    = SafeDec(r, "Amount"),
            Count     = SafeInt(r, "Count"),
        };

        private static TeamMemberStatDto ReadTeamMember(SqlDataReader r) => new()
        {
            MemberName       = SafeStr(r, "MemberName"),
            RoleId           = SafeInt(r, "RoleId"),
            ClientsCount     = SafeInt(r, "ClientsCount"),
            Revenue          = SafeDec(r, "Revenue"),
            FollowUpsDone    = SafeInt(r, "FollowUpsDone"),
            FollowUpsPending = SafeInt(r, "FollowUpsPending"),
            FollowUpsOverdue = SafeInt(r, "FollowUpsOverdue"),
        };
    }
}
