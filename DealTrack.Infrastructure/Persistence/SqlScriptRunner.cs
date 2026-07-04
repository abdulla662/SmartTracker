using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace DealTrack.Infrastructure.Persistence
{
    public static class SqlScriptRunner
    {
        public static async Task RunStoredProceduresAsync(AppDbContext db)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(r => r.EndsWith(".sql", StringComparison.OrdinalIgnoreCase));

            var connectionString = db.Database.GetConnectionString()!;

            foreach (var resourceName in resourceNames)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream is null) continue;

                using var reader = new StreamReader(stream);
                var fullScript = await reader.ReadToEndAsync();

                // Split on GO statements
                var batches = fullScript
                    .Split(["\nGO", "\r\nGO"], StringSplitOptions.RemoveEmptyEntries)
                    .Select(b => b.Trim())
                    .Where(b => !string.IsNullOrWhiteSpace(b));

                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                foreach (var batch in batches)
                {
                    await using var cmd = new SqlCommand(batch, conn);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
