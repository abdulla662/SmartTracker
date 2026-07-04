using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Dashboard;
using DealTrack.Application.Interfaces;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DealTrack.Infrastructure.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public DashboardService(AppDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<ApiResponseT<DashboardSummaryDto>> GetSummaryAsync(CancellationToken ct = default)
        {
            var userId   = _currentUser.UserId;
            var tenantId = _currentUser.TenantId;
            var role     = _currentUser.Role;

            var result = new DashboardSummaryDto();

            await using var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_GetDashboardSummary";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@UserId",   userId));
            cmd.Parameters.Add(new SqlParameter("@TenantId", tenantId));
            cmd.Parameters.Add(new SqlParameter("@Role",     role));

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            // Result set 1: Clients
            if (await reader.ReadAsync(ct))
            {
                result.TotalClientsCount = reader.GetInt32(0);
                result.TodayFollowUpsCount = reader.GetInt32(1); 
            }

            // Result set 2: Follow-ups
            if (await reader.NextResultAsync(ct) && await reader.ReadAsync(ct))
            {
                result.TodayFollowUpsCount  = reader.GetInt32(0);
                result.OverdueFollowUpsCount = reader.GetInt32(1);
            }

            // Result set 3: Payments
            if (await reader.NextResultAsync(ct) && await reader.ReadAsync(ct))
            {
                result.TotalPaymentsCount = reader.GetInt32(0);
                result.TotalPaidAmount    = reader.GetDecimal(1);
            }

            return ApiResponseT<DashboardSummaryDto>.SuccessResponse(result);
        }
    }
}
