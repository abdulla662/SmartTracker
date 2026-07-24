using MySqlConnector;

namespace DealTrack.API.Maintenance
{
    public static class MaintenanceState
    {
        private static string? _connStr;

        public static bool IsActive { get; private set; }
        public static DateTime? Until { get; private set; }

        // Called once on startup — creates table if missing, loads saved state
        public static async Task InitAsync(string connStr)
        {
            _connStr = connStr;
            await using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();

            // Create table if it doesn't exist
            await using (var cmd = new MySqlCommand("""
                CREATE TABLE IF NOT EXISTS SystemSettings (
                    `Key`   VARCHAR(100) NOT NULL PRIMARY KEY,
                    `Value` TEXT
                )
                """, conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Load saved maintenance state
            await using var sel = new MySqlCommand(
                "SELECT `Value` FROM SystemSettings WHERE `Key` = 'MaintenanceUntil'", conn);
            var val = await sel.ExecuteScalarAsync() as string;
            if (!string.IsNullOrEmpty(val) && DateTime.TryParse(val, out var until) && until > DateTime.UtcNow)
            {
                IsActive = true;
                Until = until;
            }
        }

        public static async Task EnableAsync(DateTime until)
        {
            IsActive = true;
            Until = until;
            await SaveAsync("MaintenanceUntil", until.ToString("o"));
        }

        public static async Task DisableAsync()
        {
            IsActive = false;
            Until = null;
            await SaveAsync("MaintenanceUntil", null);
        }

        private static async Task SaveAsync(string key, string? value)
        {
            if (_connStr is null) return;
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(
                "INSERT INTO SystemSettings (`Key`, `Value`) VALUES (@k, @v) " +
                "ON DUPLICATE KEY UPDATE `Value` = @v", conn);
            cmd.Parameters.AddWithValue("@k", key);
            cmd.Parameters.AddWithValue("@v", value ?? (object)DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
