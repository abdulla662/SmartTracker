CREATE OR ALTER PROCEDURE sp_GetTeamLeadDashboard
    @UserId   NVARCHAR(450),
    @TenantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserGuid    UNIQUEIDENTIFIER = TRY_CAST(@UserId AS UNIQUEIDENTIFIER);
    DECLARE @Today       DATE             = CAST(GETUTCDATE() AS DATE);
    DECLARE @TodayStart  DATETIME2        = CAST(@Today AS DATETIME2);
    DECLARE @TomStart    DATETIME2        = DATEADD(DAY, 1, @TodayStart);
    DECLARE @SixMonAgo   DATE             = DATEFROMPARTS(YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 1);
    SET @SixMonAgo = DATEADD(MONTH, -5, @SixMonAgo);

    -- Collect all user IDs in this team (TeamLead + their Sales reps)
    CREATE TABLE #TeamUserIds (UserId NVARCHAR(450), UserGuid UNIQUEIDENTIFIER, FullName NVARCHAR(256));
    INSERT INTO #TeamUserIds
    SELECT Id, TRY_CAST(Id AS UNIQUEIDENTIFIER), FullName
    FROM ApplicationUsers
    WHERE TenantId=@TenantId
      AND (Id=@UserId OR TeamLeadId=@UserGuid);

    -- ── RS1: Summary scalars ───────────────────────────────────────────────
    SELECT
        (SELECT COUNT(*) FROM Clients cl
            WHERE cl.IsDeleted=0 AND cl.TenantId=@TenantId
              AND EXISTS(SELECT 1 FROM #TeamUserIds t WHERE t.UserId=cl.AssignedToUserId)
        ) AS TotalClients,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND EXISTS(SELECT 1 FROM #TeamUserIds t WHERE t.UserGuid=f.CreatedByUserId)
              AND f.FollowUpDate >= @TodayStart AND f.FollowUpDate < @TomStart
              AND f.Status='Pending'
        ) AS TodayFollowUps,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND EXISTS(SELECT 1 FROM #TeamUserIds t WHERE t.UserGuid=f.CreatedByUserId)
              AND f.Status='Missed'
        ) AS OverdueFollowUps,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND EXISTS(SELECT 1 FROM #TeamUserIds t WHERE t.UserGuid=f.CreatedByUserId)
              AND f.Status='Pending'
        ) AS PendingFollowUps,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND EXISTS(SELECT 1 FROM #TeamUserIds t WHERE t.UserGuid=f.CreatedByUserId)
              AND f.FollowUpDate >= @TodayStart AND f.FollowUpDate < @TomStart
              AND f.Status='Done'
        ) AS CompletedToday,

        ISNULL((SELECT SUM(p.Amount) FROM Payments p
            INNER JOIN Clients c ON p.ClientId=c.Id
            WHERE p.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND EXISTS(SELECT 1 FROM #TeamUserIds t WHERE t.UserId=c.AssignedToUserId)
        ), 0) AS TotalRevenue,

        (SELECT COUNT(*) FROM Payments p
            INNER JOIN Clients c ON p.ClientId=c.Id
            WHERE p.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND EXISTS(SELECT 1 FROM #TeamUserIds t WHERE t.UserId=c.AssignedToUserId)
        ) AS TotalPayments,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND EXISTS(SELECT 1 FROM #TeamUserIds t WHERE t.UserGuid=f.CreatedByUserId)
              AND f.Status='Done'
        ) AS FollowUpsDone,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=@TenantId
              AND EXISTS(SELECT 1 FROM #TeamUserIds t WHERE t.UserGuid=f.CreatedByUserId)
              AND f.Status='Missed'
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
      AND c.TenantId=@TenantId
      AND EXISTS(SELECT 1 FROM #TeamUserIds t WHERE t.UserId=c.AssignedToUserId)
      AND p.PaymentDate >= @SixMonAgo
    GROUP BY YEAR(p.PaymentDate), MONTH(p.PaymentDate), DATENAME(MONTH, p.PaymentDate)
    ORDER BY [Year], [Month];

    -- ── RS3: Follow-up status breakdown ───────────────────────────────────
    SELECT
        SUM(CASE WHEN f.Status='Pending' THEN 1 ELSE 0 END) AS Pending,
        SUM(CASE WHEN f.Status='Done'    THEN 1 ELSE 0 END) AS Done,
        SUM(CASE WHEN f.Status='Missed'  THEN 1 ELSE 0 END) AS Missed
    FROM FollowUps f
    INNER JOIN Clients c ON f.ClientId=c.Id
    WHERE f.IsDeleted=0 AND c.IsDeleted=0
      AND c.TenantId=@TenantId
      AND EXISTS(SELECT 1 FROM #TeamUserIds t WHERE t.UserGuid=f.CreatedByUserId);

    -- ── RS4: Team member performance ──────────────────────────────────────
    SELECT
        t.FullName                                                      AS MemberName,
        ISNULL((SELECT Role FROM ApplicationUsers WHERE Id=t.UserId), 3) AS RoleId,
        (SELECT COUNT(*) FROM Clients cl
            WHERE cl.IsDeleted=0 AND cl.TenantId=@TenantId AND cl.AssignedToUserId=t.UserId
        )                                                               AS ClientsCount,
        ISNULL((SELECT SUM(p2.Amount) FROM Payments p2
            INNER JOIN Clients cl2 ON p2.ClientId=cl2.Id
            WHERE p2.IsDeleted=0 AND cl2.IsDeleted=0 AND cl2.TenantId=@TenantId
              AND cl2.AssignedToUserId=t.UserId
        ), 0)                                                           AS Revenue,
        (SELECT COUNT(*) FROM FollowUps f2
            INNER JOIN Clients cl3 ON f2.ClientId=cl3.Id
            WHERE f2.IsDeleted=0 AND cl3.IsDeleted=0 AND cl3.TenantId=@TenantId
              AND f2.CreatedByUserId=t.UserGuid AND f2.Status='Done'
        )                                                               AS FollowUpsDone,
        (SELECT COUNT(*) FROM FollowUps f3
            INNER JOIN Clients cl4 ON f3.ClientId=cl4.Id
            WHERE f3.IsDeleted=0 AND cl4.IsDeleted=0 AND cl4.TenantId=@TenantId
              AND f3.CreatedByUserId=t.UserGuid AND f3.Status='Pending'
        )                                                               AS FollowUpsPending,
        (SELECT COUNT(*) FROM FollowUps f4
            INNER JOIN Clients cl5 ON f4.ClientId=cl5.Id
            WHERE f4.IsDeleted=0 AND cl5.IsDeleted=0 AND cl5.TenantId=@TenantId
              AND f4.CreatedByUserId=t.UserGuid AND f4.Status='Missed'
        )                                                               AS FollowUpsOverdue
    FROM #TeamUserIds t
    ORDER BY Revenue DESC;

    DROP TABLE #TeamUserIds;
END
