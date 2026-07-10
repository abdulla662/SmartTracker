CREATE OR ALTER PROCEDURE sp_GetAdminDashboard
    @TenantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today       DATE      = CAST(GETUTCDATE() AS DATE);
    DECLARE @TodayStart  DATETIME2 = CAST(@Today AS DATETIME2);
    DECLARE @TomStart    DATETIME2 = DATEADD(DAY, 1, @TodayStart);
    DECLARE @SixMonAgo   DATE      = DATEFROMPARTS(YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 1);
    SET @SixMonAgo = DATEADD(MONTH, -5, @SixMonAgo);

    -- ── RS1: Summary scalars ───────────────────────────────────────────────
    SELECT
        (SELECT COUNT(*) FROM Clients WHERE IsDeleted=0 AND TenantId=@TenantId
        ) AS TotalClients,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND f.FollowUpDate >= @TodayStart AND f.FollowUpDate < @TomStart
              AND f.Status='Pending'
        ) AS TodayFollowUps,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND f.Status='Missed'
        ) AS OverdueFollowUps,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND f.Status='Pending'
        ) AS PendingFollowUps,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND f.FollowUpDate >= @TodayStart AND f.FollowUpDate < @TomStart
              AND f.Status='Done'
        ) AS CompletedToday,

        ISNULL((SELECT SUM(Amount) FROM Payments WHERE IsDeleted=0 AND TenantId=@TenantId), 0) AS TotalRevenue,

        (SELECT COUNT(*) FROM Payments WHERE IsDeleted=0 AND TenantId=@TenantId) AS TotalPayments,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId AND f.Status='Done'
        ) AS FollowUpsDone,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId AND f.Status='Missed'
        ) AS FollowUpsMissed;

    -- ── RS2: Monthly Revenue (last 6 months) ──────────────────────────────
    SELECT
        YEAR(PaymentDate)             AS [Year],
        MONTH(PaymentDate)            AS [Month],
        DATENAME(MONTH, PaymentDate)  AS MonthName,
        ISNULL(SUM(Amount), 0)        AS Amount,
        COUNT(*)                      AS [Count]
    FROM Payments
    WHERE IsDeleted=0 AND TenantId=@TenantId AND PaymentDate >= @SixMonAgo
    GROUP BY YEAR(PaymentDate), MONTH(PaymentDate), DATENAME(MONTH, PaymentDate)
    ORDER BY [Year], [Month];

    -- ── RS3: Follow-up status breakdown ───────────────────────────────────
    SELECT
        SUM(CASE WHEN f.Status='Pending' THEN 1 ELSE 0 END) AS Pending,
        SUM(CASE WHEN f.Status='Done'    THEN 1 ELSE 0 END) AS Done,
        SUM(CASE WHEN f.Status='Missed'  THEN 1 ELSE 0 END) AS Missed
    FROM FollowUps f
    INNER JOIN Clients c ON f.ClientId=c.Id
    WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId;

    -- ── RS4: Per-user performance (Sales reps + TeamLeads) ────────────────
    SELECT
        u.FullName                                                     AS MemberName,
        u.Role                                                         AS RoleId,
        (SELECT COUNT(*) FROM Clients cl
            WHERE cl.IsDeleted=0 AND cl.TenantId=@TenantId AND cl.AssignedToUserId=u.Id
        )                                                              AS ClientsCount,
        ISNULL((SELECT SUM(p.Amount) FROM Payments p
            INNER JOIN Clients cl2 ON p.ClientId=cl2.Id
            WHERE p.IsDeleted=0 AND cl2.IsDeleted=0 AND cl2.TenantId=@TenantId
              AND cl2.AssignedToUserId=u.Id
        ), 0)                                                          AS Revenue,
        (SELECT COUNT(*) FROM FollowUps f2
            INNER JOIN Clients cl3 ON f2.ClientId=cl3.Id
            WHERE f2.IsDeleted=0 AND cl3.IsDeleted=0 AND cl3.TenantId=@TenantId
              AND f2.CreatedByUserId=TRY_CAST(u.Id AS UNIQUEIDENTIFIER) AND f2.Status='Done'
        )                                                              AS FollowUpsDone,
        (SELECT COUNT(*) FROM FollowUps f3
            INNER JOIN Clients cl4 ON f3.ClientId=cl4.Id
            WHERE f3.IsDeleted=0 AND cl4.IsDeleted=0 AND cl4.TenantId=@TenantId
              AND f3.CreatedByUserId=TRY_CAST(u.Id AS UNIQUEIDENTIFIER) AND f3.Status='Pending'
        )                                                              AS FollowUpsPending,
        (SELECT COUNT(*) FROM FollowUps f4
            INNER JOIN Clients cl5 ON f4.ClientId=cl5.Id
            WHERE f4.IsDeleted=0 AND cl5.IsDeleted=0 AND cl5.TenantId=@TenantId
              AND f4.CreatedByUserId=TRY_CAST(u.Id AS UNIQUEIDENTIFIER) AND f4.Status='Missed'
        )                                                              AS FollowUpsOverdue
    FROM ApplicationUsers u
    WHERE u.TenantId=@TenantId AND u.Role IN (2, 3)
    ORDER BY Revenue DESC;
END
