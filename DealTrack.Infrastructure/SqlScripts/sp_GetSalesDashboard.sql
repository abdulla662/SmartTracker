DROP PROCEDURE IF EXISTS sp_GetSalesDashboard;;
CREATE PROCEDURE sp_GetSalesDashboard(
    IN p_UserId   VARCHAR(450),
    IN p_TenantId CHAR(36)
)
BEGIN
    DECLARE v_Today      DATE     DEFAULT CURDATE();
    DECLARE v_TodayStart DATETIME DEFAULT CAST(v_Today AS DATETIME);
    DECLARE v_TomStart   DATETIME DEFAULT DATE_ADD(v_TodayStart, INTERVAL 1 DAY);
    DECLARE v_SixMonAgo  DATE     DEFAULT DATE_ADD(DATE_FORMAT(UTC_DATE(), '%Y-%m-01'), INTERVAL -5 MONTH);

    -- RS1: Summary scalars
    SELECT
        (SELECT COUNT(*) FROM Clients
         WHERE IsDeleted=0 AND TenantId=p_TenantId AND AssignedToUserId=p_UserId
        ) AS TotalClients,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND c.AssignedToUserId=p_UserId
              AND f.FollowUpDate >= v_TodayStart AND f.FollowUpDate < v_TomStart
              AND f.Status='Pending'
        ) AS TodayFollowUps,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND c.AssignedToUserId=p_UserId AND f.Status='Missed'
        ) AS OverdueFollowUps,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND c.AssignedToUserId=p_UserId AND f.Status='Pending'
        ) AS PendingFollowUps,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND c.AssignedToUserId=p_UserId
              AND f.FollowUpDate >= v_TodayStart AND f.FollowUpDate < v_TomStart
              AND f.Status='Done'
        ) AS CompletedToday,

        IFNULL((SELECT SUM(p2.Amount) FROM Payments p2
            INNER JOIN Clients c ON p2.ClientId=c.Id
            WHERE p2.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND c.AssignedToUserId=p_UserId
        ), 0) AS TotalRevenue,

        (SELECT COUNT(*) FROM Payments p2
            INNER JOIN Clients c ON p2.ClientId=c.Id
            WHERE p2.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND c.AssignedToUserId=p_UserId
        ) AS TotalPayments,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND c.AssignedToUserId=p_UserId AND f.Status='Done'
        ) AS FollowUpsDone,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND c.AssignedToUserId=p_UserId AND f.Status='Missed'
        ) AS FollowUpsMissed;

    -- RS2: Monthly Revenue (last 6 months)
    SELECT
        YEAR(p2.PaymentDate)             AS `Year`,
        MONTH(p2.PaymentDate)            AS `Month`,
        MONTHNAME(p2.PaymentDate)        AS MonthName,
        IFNULL(SUM(p2.Amount), 0)        AS Amount,
        COUNT(*)                         AS `Count`
    FROM Payments p2
    INNER JOIN Clients c ON p2.ClientId=c.Id
    WHERE p2.IsDeleted=0 AND c.IsDeleted=0
      AND c.TenantId=p_TenantId AND c.AssignedToUserId=p_UserId
      AND p2.PaymentDate >= v_SixMonAgo
    GROUP BY YEAR(p2.PaymentDate), MONTH(p2.PaymentDate), MONTHNAME(p2.PaymentDate)
    ORDER BY `Year`, `Month`;

    -- RS3: Follow-up status breakdown
    SELECT
        IFNULL(SUM(CASE WHEN f.Status='Pending' THEN 1 ELSE 0 END), 0) AS Pending,
        IFNULL(SUM(CASE WHEN f.Status='Done'    THEN 1 ELSE 0 END), 0) AS Done,
        IFNULL(SUM(CASE WHEN f.Status='Missed'  THEN 1 ELSE 0 END), 0) AS Missed
    FROM FollowUps f
    INNER JOIN Clients c ON f.ClientId=c.Id
    WHERE f.IsDeleted=0 AND c.IsDeleted=0
      AND c.TenantId=p_TenantId AND c.AssignedToUserId=p_UserId;
END;;
