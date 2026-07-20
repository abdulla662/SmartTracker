DROP PROCEDURE IF EXISTS sp_GetDashboardSummary;;
CREATE PROCEDURE sp_GetDashboardSummary(
    IN p_UserId   VARCHAR(450),
    IN p_TenantId CHAR(36),
    IN p_Role     VARCHAR(50)
)
BEGIN
    SELECT 1 AS Legacy;
END;;
