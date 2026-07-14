CREATE OR ALTER PROCEDURE sp_GetSalesDashboard
    @UserId   NVARCHAR(450),
    @TenantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today       DATE      = CAST(GETUTCDATE() AS DATE);
    DECLARE @TodayStart  DATETIME2 = CAST(@Today AS DATETIME2);
    DECLARE @TomStart    DATETIME2 = DATEADD(DAY, 1, @TodayStart);
    DECLARE @SixMonAgo   DATE      = DATEADD(MONTH, -5, DATEFROMPARTS(YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 1));

    -- ── RS1: Summary scalars ───────────────────────────────────────────────
    SELECT
        (SELECT COUNT(*) FROM Clients
         WHERE IsDeleted=0 AND TenantId=@TenantId AND AssignedToUserId=@UserId
        ) AS TotalClients,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND c.AssignedToUserId=@UserId
              AND f.FollowUpDate >= @TodayStart AND f.FollowUpDate < @TomStart
              AND f.Status='Pending'
        ) AS TodayFollowUps,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND c.AssignedToUserId=@UserId AND f.Status='Missed'
        ) AS OverdueFollowUps,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND c.AssignedToUserId=@UserId AND f.Status='Pending'
        ) AS PendingFollowUps,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND c.AssignedToUserId=@UserId
              AND f.FollowUpDate >= @TodayStart AND f.FollowUpDate < @TomStart
              AND f.Status='Done'
        ) AS CompletedToday,

        ISNULL((SELECT SUM(p.Amount) FROM Payments p
            INNER JOIN Clients c ON p.ClientId=c.Id
            WHERE p.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND c.AssignedToUserId=@UserId
        ), 0) AS TotalRevenue,

        (SELECT COUNT(*) FROM Payments p
            INNER JOIN Clients c ON p.ClientId=c.Id
            WHERE p.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND c.AssignedToUserId=@UserId
        ) AS TotalPayments,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND c.AssignedToUserId=@UserId AND f.Status='Done'
        ) AS FollowUpsDone,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND c.AssignedToUserId=@UserId AND f.Status='Missed'
        ) AS FollowUpsMissed;

    -- ── RS2: Monthly Revenue (last 6 months) ──────────────────────────────
    SELECT
        YEAR(p.PaymentDate)             AS [Year],
        MONTH(p.PaymentDate)            AS [Month],
        DATENAME(MONTH, p.PaymentDate)  AS MonthName,
        ISNULL(SUM(p.Amount), 0)        AS Amount,
        COUNT(*)                        AS [Count]
    FROM Payments p
    INNER JOIN Clients c ON p.ClientId=c.Id
    WHERE p.IsDeleted=0 AND c.IsDeleted=0
      AND c.TenantId=@TenantId AND c.AssignedToUserId=@UserId
      AND p.PaymentDate >= @SixMonAgo
    GROUP BY YEAR(p.PaymentDate), MONTH(p.PaymentDate), DATENAME(MONTH, p.PaymentDate)
    ORDER BY [Year], [Month];

    -- ── RS3: Follow-up status breakdown ───────────────────────────────────
    SELECT
        ISNULL(SUM(CASE WHEN f.Status='Pending' THEN 1 ELSE 0 END), 0) AS Pending,
        ISNULL(SUM(CASE WHEN f.Status='Done'    THEN 1 ELSE 0 END), 0) AS Done,
        ISNULL(SUM(CASE WHEN f.Status='Missed'  THEN 1 ELSE 0 END), 0) AS Missed
    FROM FollowUps f
    INNER JOIN Clients c ON f.ClientId=c.Id
    WHERE f.IsDeleted=0 AND c.IsDeleted=0
      AND c.TenantId=@TenantId AND c.AssignedToUserId=@UserId;
END
