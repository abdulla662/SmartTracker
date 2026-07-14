using System.Diagnostics;

namespace DealTrack.API.Services
{
    public class OcrPythonHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<OcrPythonHostedService> _logger;
        private readonly IConfiguration _configuration;
        private Process? _process;

        public OcrPythonHostedService(ILogger<OcrPythonHostedService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var scriptPath = _configuration["OcrService:ScriptPath"]
                ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "ocr-service", "main.py");

            scriptPath = Path.GetFullPath(scriptPath);

            if (!File.Exists(scriptPath))
            {
                _logger.LogWarning("OCR Python script not found at: {Path}", scriptPath);
                return Task.CompletedTask;
            }

            // Kill any stale python main.py processes from previous runs
            if (OperatingSystem.IsWindows())
                KillStaleOcrProcesses(scriptPath);

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\"",
                    WorkingDirectory = Path.GetDirectoryName(scriptPath)!, // needed so .env is found
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                }
            };

            _process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    _logger.LogInformation("[OCR Python] {Message}", e.Data);
            };

            _process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    _logger.LogError("[OCR Python] {Message}", e.Data);
            };

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            _logger.LogInformation("OCR Python service started (PID: {Pid})", _process.Id);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
                _logger.LogInformation("OCR Python service stopped.");
            }
            return Task.CompletedTask;
        }

        public void Dispose() => _process?.Dispose();

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private void KillStaleOcrProcesses(string scriptPath)
        {
            try
            {
                var scriptName = Path.GetFileName(scriptPath); // "main.py"
                var stale = System.Diagnostics.Process.GetProcessesByName("python")
                    .Where(p =>
                    {
                        try
                        {
                            var wmi = new System.Management.ManagementObjectSearcher(
                                $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {p.Id}");
                            foreach (System.Management.ManagementObject obj in wmi.Get())
                                if (obj["CommandLine"]?.ToString()?.Contains(scriptName) == true)
                                    return true;
                        }
                        catch { }
                        return false;
                    })
                    .ToList();

                foreach (var p in stale)
                {
                    try { p.Kill(entireProcessTree: true); } catch { }
                    _logger.LogInformation("Killed stale OCR process (PID {Pid})", p.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not enumerate stale OCR processes: {Msg}", ex.Message);
            }
        }
    }
}
