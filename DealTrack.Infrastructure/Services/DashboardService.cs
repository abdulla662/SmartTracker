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
        private static void FillScalars(DashboardSummaryDto dto, SqlDataReader r)
        {
            dto.TotalClientsCount     = r.GetInt32(r.GetOrdinal("TotalClients"));
            dto.TodayFollowUpsCount   = r.GetInt32(r.GetOrdinal("TodayFollowUps"));
            dto.OverdueFollowUpsCount = r.GetInt32(r.GetOrdinal("OverdueFollowUps"));
            dto.PendingFollowUpsCount = r.GetInt32(r.GetOrdinal("PendingFollowUps"));
            dto.CompletedTodayCount   = r.GetInt32(r.GetOrdinal("CompletedToday"));
            dto.TotalPaidAmount       = r.GetDecimal(r.GetOrdinal("TotalRevenue"));
            dto.TotalPaymentsCount    = r.GetInt32(r.GetOrdinal("TotalPayments"));
            dto.FollowUpsDone         = r.GetInt32(r.GetOrdinal("FollowUpsDone"));
            dto.FollowUpsMissed       = r.GetInt32(r.GetOrdinal("FollowUpsMissed"));
        }

        private static void FillStatusBreakdown(DashboardSummaryDto dto, SqlDataReader r)
        {
            dto.PendingFollowUpsCount = r.GetInt32(r.GetOrdinal("Pending"));
            dto.FollowUpsDone         = r.GetInt32(r.GetOrdinal("Done"));
            dto.FollowUpsMissed       = r.GetInt32(r.GetOrdinal("Missed"));
        }

        private static MonthlyRevenueDto ReadMonthlyRevenue(SqlDataReader r) => new()
        {
            Year      = r.GetInt32(r.GetOrdinal("Year")),
            Month     = r.GetInt32(r.GetOrdinal("Month")),
            MonthName = r.GetString(r.GetOrdinal("MonthName")),
            Amount    = r.GetDecimal(r.GetOrdinal("Amount")),
            Count     = r.GetInt32(r.GetOrdinal("Count")),
        };

        private static TeamMemberStatDto ReadTeamMember(SqlDataReader r) => new()
        {
            MemberName       = r.GetString(r.GetOrdinal("MemberName")),
            RoleId           = r.IsDBNull(r.GetOrdinal("RoleId")) ? 0 : r.GetInt32(r.GetOrdinal("RoleId")),
            ClientsCount     = r.GetInt32(r.GetOrdinal("ClientsCount")),
            Revenue          = r.GetDecimal(r.GetOrdinal("Revenue")),
            FollowUpsDone    = r.GetInt32(r.GetOrdinal("FollowUpsDone")),
            FollowUpsPending = r.GetInt32(r.GetOrdinal("FollowUpsPending")),
            FollowUpsOverdue = r.GetInt32(r.GetOrdinal("FollowUpsOverdue")),
        };
    }
}
