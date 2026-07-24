DROP PROCEDURE IF EXISTS sp_GetTeamLeadDashboard;;
CREATE PROCEDURE sp_GetTeamLeadDashboard(
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
        (SELECT COUNT(*) FROM Clients cl
            INNER JOIN ApplicationUsers au ON cl.AssignedToUserId = au.Id
            WHERE cl.IsDeleted=0 AND cl.TenantId=p_TenantId
              AND (au.Id=p_UserId OR au.TeamLeadId=p_UserId)
        ) AS TotalClients,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            INNER JOIN ApplicationUsers au ON f.CreatedByUserId = au.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND (au.Id=p_UserId OR au.TeamLeadId=p_UserId)
              AND f.FollowUpDate >= v_TodayStart AND f.FollowUpDate < v_TomStart
              AND f.Status='Pending'
        ) AS TodayFollowUps,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            INNER JOIN ApplicationUsers au ON f.CreatedByUserId = au.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND (au.Id=p_UserId OR au.TeamLeadId=p_UserId)
              AND f.Status='Missed'
        ) AS OverdueFollowUps,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            INNER JOIN ApplicationUsers au ON f.CreatedByUserId = au.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND (au.Id=p_UserId OR au.TeamLeadId=p_UserId)
              AND f.Status='Pending'
        ) AS PendingFollowUps,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            INNER JOIN ApplicationUsers au ON f.CreatedByUserId = au.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND (au.Id=p_UserId OR au.TeamLeadId=p_UserId)
              AND f.FollowUpDate >= v_TodayStart AND f.FollowUpDate < v_TomStart
              AND f.Status='Done'
        ) AS CompletedToday,

        IFNULL((SELECT SUM(p2.Amount) FROM Payments p2
            INNER JOIN Clients c ON p2.ClientId=c.Id
            INNER JOIN ApplicationUsers au ON c.AssignedToUserId = au.Id
            WHERE p2.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND (au.Id=p_UserId OR au.TeamLeadId=p_UserId)
        ), 0) AS TotalRevenue,

        (SELECT COUNT(*) FROM Payments p2
            INNER JOIN Clients c ON p2.ClientId=c.Id
            INNER JOIN ApplicationUsers au ON c.AssignedToUserId = au.Id
            WHERE p2.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND (au.Id=p_UserId OR au.TeamLeadId=p_UserId)
        ) AS TotalPayments,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            INNER JOIN ApplicationUsers au ON f.CreatedByUserId = au.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND (au.Id=p_UserId OR au.TeamLeadId=p_UserId)
              AND f.Status='Done'
        ) AS FollowUpsDone,

        (SELECT COUNT(*) FROM FollowUps f
            INNER JOIN Clients c ON f.ClientId=c.Id
            INNER JOIN ApplicationUsers au ON f.CreatedByUserId = au.Id
            WHERE f.IsDeleted=0 AND c.IsDeleted=0 AND c.TenantId=p_TenantId
              AND (au.Id=p_UserId OR au.TeamLeadId=p_UserId)
              AND f.Status='Missed'
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
    INNER JOIN ApplicationUsers au ON c.AssignedToUserId = au.Id
    WHERE p2.IsDeleted=0 AND c.IsDeleted=0
      AND c.TenantId=p_TenantId
      AND (au.Id=p_UserId OR au.TeamLeadId=p_UserId)
      AND p2.PaymentDate >= v_SixMonAgo
    GROUP BY YEAR(p2.PaymentDate), MONTH(p2.PaymentDate), MONTHNAME(p2.PaymentDate)
    ORDER BY `Year`, `Month`;

    -- RS3: Follow-up status breakdown
    SELECT
        SUM(CASE WHEN f.Status='Pending' THEN 1 ELSE 0 END) AS Pending,
        SUM(CASE WHEN f.Status='Done'    THEN 1 ELSE 0 END) AS Done,
        SUM(CASE WHEN f.Status='Missed'  THEN 1 ELSE 0 END) AS Missed
    FROM FollowUps f
    INNER JOIN Clients c ON f.ClientId=c.Id
    INNER JOIN ApplicationUsers au ON f.CreatedByUserId = au.Id
    WHERE f.IsDeleted=0 AND c.IsDeleted=0
      AND c.TenantId=p_TenantId
      AND (au.Id=p_UserId OR au.TeamLeadId=p_UserId);

    -- RS4: Team member performance
    SELECT
        u.FullName                                                      AS MemberName,
        u.Role                                                          AS RoleId,
        (SELECT COUNT(*) FROM Clients cl
            WHERE cl.IsDeleted=0 AND cl.TenantId=p_TenantId AND cl.AssignedToUserId=u.Id
        )                                                               AS ClientsCount,
        IFNULL((SELECT SUM(p2.Amount) FROM Payments p2
            INNER JOIN Clients cl2 ON p2.ClientId=cl2.Id
            WHERE p2.IsDeleted=0 AND cl2.IsDeleted=0 AND cl2.TenantId=p_TenantId
              AND cl2.AssignedToUserId=u.Id
        ), 0)                                                           AS Revenue,
        (SELECT COUNT(*) FROM FollowUps f2
            INNER JOIN Clients cl3 ON f2.ClientId=cl3.Id
            WHERE f2.IsDeleted=0 AND cl3.IsDeleted=0 AND cl3.TenantId=p_TenantId
              AND f2.CreatedByUserId=u.Id AND f2.Status='Done'
        )                                                               AS FollowUpsDone,
        (SELECT COUNT(*) FROM FollowUps f3
            INNER JOIN Clients cl4 ON f3.ClientId=cl4.Id
            WHERE f3.IsDeleted=0 AND cl4.IsDeleted=0 AND cl4.TenantId=p_TenantId
              AND f3.CreatedByUserId=u.Id AND f3.Status='Pending'
        )                                                               AS FollowUpsPending,
        (SELECT COUNT(*) FROM FollowUps f4
            INNER JOIN Clients cl5 ON f4.ClientId=cl5.Id
            WHERE f4.IsDeleted=0 AND cl5.IsDeleted=0 AND cl5.TenantId=p_TenantId
              AND f4.CreatedByUserId=u.Id AND f4.Status='Missed'
        )                                                               AS FollowUpsOverdue
    FROM ApplicationUsers u
    WHERE u.TenantId=p_TenantId
      AND (u.Id=p_UserId OR u.TeamLeadId=p_UserId)
    ORDER BY Revenue DESC;

END;;
