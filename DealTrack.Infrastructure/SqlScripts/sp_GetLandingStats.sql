DROP PROCEDURE IF EXISTS sp_GetLandingStats;;
CREATE PROCEDURE sp_GetLandingStats()
BEGIN
    DECLARE v_MonthStart DATETIME DEFAULT DATE_FORMAT(UTC_DATE(), '%Y-%m-01');
    DECLARE v_TodayStart DATETIME DEFAULT CAST(CURDATE() AS DATETIME);
    DECLARE v_TomStart   DATETIME DEFAULT DATE_ADD(v_TodayStart, INTERVAL 1 DAY);

    SELECT
        -- Total active clients across all tenants
        (SELECT COUNT(*) FROM Clients WHERE IsDeleted = 0) AS ActiveClients,

        -- Follow-ups scheduled for today (Pending) across all tenants
        (SELECT COUNT(*) FROM FollowUps
         WHERE IsDeleted = 0
           AND Status = 'Pending'
           AND FollowUpDate >= v_TodayStart
           AND FollowUpDate < v_TomStart
        ) AS TodaysTasks,

        -- Total collected this month across all tenants
        IFNULL(
            (SELECT SUM(Amount) FROM Payments
             WHERE IsDeleted = 0 AND PaymentDate >= v_MonthStart),
            0
        ) AS CollectedThisMonth,

        -- Collection rate: Done / (Done + Missed) * 100, across all tenants
        IFNULL(
            ROUND(
                (SELECT COUNT(*) FROM FollowUps WHERE IsDeleted = 0 AND Status = 'Done') * 100.0 /
                NULLIF(
                    (SELECT COUNT(*) FROM FollowUps WHERE IsDeleted = 0 AND Status IN ('Done', 'Missed')),
                    0
                ),
                1
            ),
            0
        ) AS CollectionRate;
END;;
