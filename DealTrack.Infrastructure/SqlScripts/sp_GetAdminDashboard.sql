DROP PROCEDURE IF EXISTS sp_GetAdminDashboard;;
CREATE PROCEDURE sp_GetAdminDashboard(
    IN p_TenantId CHAR(36)
)
BEGIN
    DECLARE v_Today      DATE     DEFAULT CURDATE();
    DECLARE v_TodayStart DATETIME DEFAULT CAST(v_Today AS DATETIME);
    DECLARE v_TomStart   DATETIME DEFAULT DATE_ADD(v_TodayStart, INTERVAL 1 DAY);
    DECLARE v_SixMonAgo  DATE     DEFAULT DATE_FORMAT(UTC_DATE(), '%Y-%m-01');
    SET v_SixMonAgo = DATE_ADD(v_SixMonAgo, INTERVAL -5 MONTH);

    -- RS1: Summary scalars
    SELECT
        (SELECT COUNT(*) FROM Clients WHERE IsDeleted=0 AND TenantId=p_TenantId
        ) AS TotalClients,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND f.FollowUpDate >= v_TodayStart AND f.FollowUpDate < v_TomStart
              AND f.Status='Pending'
        ) AS TodayFollowUps,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND f.Status='Missed'
        ) AS OverdueFollowUps,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND f.Status='Pending'
        ) AS PendingFollowUps,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND f.FollowUpDate >= v_TodayStart AND f.FollowUpDate < v_TomStart
              AND f.Status='Done'
        ) AS CompletedToday,

        IFNULL((SELECT SUM(Amount) FROM Payments WHERE IsDeleted=0 AND TenantId=p_TenantId), 0) AS TotalRevenue,

        (SELECT COUNT(*) FROM Payments WHERE IsDeleted=0 AND TenantId=p_TenantId) AS TotalPayments,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId AND f.Status='Done'
        ) AS FollowUpsDone,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId AND f.Status='Missed'
        ) AS FollowUpsMissed;

    -- RS2: Monthly Revenue (last 6 months)
    SELECT
        YEAR(PaymentDate)             AS `Year`,
        MONTH(PaymentDate)            AS `Month`,
        MONTHNAME(PaymentDate)        AS MonthName,
        IFNULL(SUM(Amount), 0)        AS Amount,
        COUNT(*)                      AS `Count`
    FROM Payments
    WHERE IsDeleted=0 AND TenantId=p_TenantId AND PaymentDate >= v_SixMonAgo
    GROUP BY YEAR(PaymentDate), MONTH(PaymentDate), MONTHNAME(PaymentDate)
    ORDER BY `Year`, `Month`;

    -- RS3: Follow-up status breakdown
    SELECT
        SUM(CASE WHEN f.Status='Pending' THEN 1 ELSE 0 END) AS Pending,
        SUM(CASE WHEN f.Status='Done'    THEN 1 ELSE 0 END) AS Done,
        SUM(CASE WHEN f.Status='Missed'  THEN 1 ELSE 0 END) AS Missed
    FROM FollowUps f
    INNER JOIN Clients c ON f.ClientId=c.Id
    WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId;

    -- RS4: Per-user performance
    SELECT
        u.FullName                                                     AS MemberName,
        u.Role                                                         AS RoleId,
        (SELECT COUNT(*) FROM Clients cl
            WHERE cl.IsDeleted=0 AND cl.TenantId=p_TenantId AND cl.AssignedToUserId=u.Id
        )                                                              AS ClientsCount,
        IFNULL((SELECT SUM(p2.Amount) FROM Payments p2
            INNER JOIN Clients cl2 ON p2.ClientId=cl2.Id
            WHERE p2.IsDeleted=0 AND cl2.IsDeleted=0 AND cl2.TenantId=p_TenantId
              AND cl2.AssignedToUserId=u.Id
        ), 0)                                                          AS Revenue,
        (SELECT COUNT(*) FROM FollowUps f2
            INNER JOIN Clients cl3 ON f2.ClientId=cl3.Id
            WHERE f2.IsDeleted=0 AND cl3.IsDeleted=0 AND cl3.TenantId=p_TenantId
              AND f2.CreatedByUserId=u.Id AND f2.Status='Done'
        )                                                              AS FollowUpsDone,
        (SELECT COUNT(*) FROM FollowUps f3
            INNER JOIN Clients cl4 ON f3.ClientId=cl4.Id
            WHERE f3.IsDeleted=0 AND cl4.IsDeleted=0 AND cl4.TenantId=p_TenantId
              AND f3.CreatedByUserId=u.Id AND f3.Status='Pending'
        )                                                              AS FollowUpsPending,
        (SELECT COUNT(*) FROM FollowUps f4
            INNER JOIN Clients cl5 ON f4.ClientId=cl5.Id
            WHERE f4.IsDeleted=0 AND cl5.IsDeleted=0 AND cl5.TenantId=p_TenantId
              AND f4.CreatedByUserId=u.Id AND f4.Status='Missed'
        )                                                              AS FollowUpsOverdue
    FROM ApplicationUsers u
    WHERE u.TenantId=p_TenantId AND u.Role IN (2, 3)
    ORDER BY Revenue DESC;
END;;
