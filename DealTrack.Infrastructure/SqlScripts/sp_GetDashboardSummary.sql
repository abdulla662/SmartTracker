IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_GetDashboardSummary')
    DROP PROCEDURE sp_GetDashboardSummary;
GO

CREATE PROCEDURE sp_GetDashboardSummary
    @UserId NVARCHAR(450),
    @TenantId UNIQUEIDENTIFIER,
    @Role NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(GETDATE() AS DATE);

    SELECT
        COUNT(*) AS TotalClients,
        ISNULL(SUM(CASE WHEN CAST(CreatedAt AS DATE) = @Today THEN 1 ELSE 0 END), 0) AS NewClientsToday
    FROM Clients
    WHERE IsDeleted = 0
      AND (
          @Role = 'Admin'
          OR (@Role = 'TeamLead' AND TenantId = @TenantId)
          OR (@Role = 'Sales'    AND AssignedToUserId = @UserId)
      );

    SELECT
        ISNULL(COUNT(CASE WHEN CAST(FollowUpDate AS DATE) = @Today AND Status != 2 THEN 1 END), 0) AS TodayFollowUps,
        ISNULL(COUNT(CASE WHEN FollowUpDate < GETDATE() AND Status = 3 THEN 1 END), 0) AS OverdueFollowUps
    FROM FollowUps f
    INNER JOIN Clients c ON f.ClientId = c.Id
    WHERE f.IsDeleted = 0 AND c.IsDeleted = 0
      AND (
          @Role = 'Admin'
          OR (@Role = 'TeamLead' AND c.TenantId = @TenantId)
          OR (@Role = 'Sales'    AND c.AssignedToUserId = @UserId)
      );

    SELECT
        ISNULL(COUNT(*), 0) AS TotalPayments,
        ISNULL(SUM(Amount), 0) AS TotalRevenue
    FROM Payments p
    INNER JOIN Clients c ON p.ClientId = c.Id
    WHERE p.IsDeleted = 0 AND c.IsDeleted = 0
      AND (
          @Role = 'Admin'
          OR (@Role = 'TeamLead' AND c.TenantId = @TenantId)
          OR (@Role = 'Sales'    AND c.AssignedToUserId = @UserId)
      );
END
