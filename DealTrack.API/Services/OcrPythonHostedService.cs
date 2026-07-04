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

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
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
    }
}
