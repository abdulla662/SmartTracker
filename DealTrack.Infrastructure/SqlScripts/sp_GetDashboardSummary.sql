-- Legacy dashboard SP kept for reference; use role-specific SPs instead.
CREATE OR ALTER PROCEDURE sp_GetDashboardSummary
    @UserId   NVARCHAR(450),
    @TenantId UNIQUEIDENTIFIER,
    @Role     NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Today DATE = CAST(GETUTCDATE() AS DATE);
    SELECT 1 AS Legacy;
END
