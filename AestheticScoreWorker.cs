using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaPictureViewer
{
    public sealed class AestheticScoreWorker : IDisposable
    {
        private readonly string _workingDirectory;
        private readonly string _uvExecutable;
        private readonly string _processPath;
        private readonly SemaphoreSlim _workerLock = new SemaphoreSlim(1, 1);
        private Process _process;
        private Task _stderrPump;
        private readonly StringBuilder _stderr = new StringBuilder();
        private bool _disposed;

        public AestheticScoreWorker()
        {
            _workingDirectory = Path.Combine(AppContext.BaseDirectory, "experimental", "scoring-tool");
            if (!Directory.Exists(_workingDirectory))
            {
                _workingDirectory = Path.GetFullPath(
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "experimental", "scoring-tool"));
            }

            _uvExecutable = ResolveUvExecutable();
            _processPath = BuildProcessPath();
        }

        public async Task<double> ScoreAsync(string imagePath, string requestId)
        {
            await _workerLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await EnsureStartedAsync().ConfigureAwait(false);

                var request = JsonSerializer.Serialize(new
                {
                    id = requestId,
                    type = "score",
                    image = imagePath,
                });

                await _process.StandardInput.WriteLineAsync(request).ConfigureAwait(false);
                var line = await _process.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                if (line == null)
                {
                    throw new InvalidOperationException(FormatWorkerExitMessage("Score worker stopped unexpectedly."));
                }

                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;
                if (root.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True)
                {
                    return root.GetProperty("score").GetDouble();
                }

                var error = root.TryGetProperty("error", out var errorElement)
                    ? errorElement.GetString()
                    : "Score worker returned an error.";
                throw new InvalidOperationException(error);
            }
            finally
            {
                _workerLock.Release();
            }
        }

        private async Task EnsureStartedAsync()
        {
            if (_process != null && !_process.HasExited)
            {
                return;
            }

            if (!Directory.Exists(_workingDirectory))
            {
                throw new DirectoryNotFoundException($"Scoring tool directory not found: {_workingDirectory}");
            }

            _stderr.Clear();
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _uvExecutable,
                    Arguments = "run score-photo --worker",
                    WorkingDirectory = _workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true,
            };
            _process.StartInfo.Environment["PATH"] = _processPath;

            if (!_process.Start())
            {
                throw new InvalidOperationException("Failed to start score worker.");
            }

            _stderrPump = Task.Run(async () =>
            {
                while (!_process.HasExited)
                {
                    var line = await _process.StandardError.ReadLineAsync().ConfigureAwait(false);
                    if (line == null)
                    {
                        break;
                    }

                    lock (_stderr)
                    {
                        if (_stderr.Length > 0)
                        {
                            _stderr.AppendLine();
                        }
                        _stderr.Append(line);
                    }
                }
            });

            var readyLineTask = _process.StandardOutput.ReadLineAsync();
            var exitTask = _process.WaitForExitAsync();
            var completed = await Task.WhenAny(readyLineTask, exitTask).ConfigureAwait(false);
            if (completed == exitTask)
            {
                throw new InvalidOperationException(FormatWorkerExitMessage("Score worker exited during startup."));
            }

            var readyLine = await readyLineTask.ConfigureAwait(false);
            if (readyLine == null)
            {
                throw new InvalidOperationException(FormatWorkerExitMessage("Score worker did not report readiness."));
            }

            using var document = JsonDocument.Parse(readyLine);
            var root = document.RootElement;
            if (!root.TryGetProperty("type", out var type) ||
                type.GetString() != "ready" ||
                !root.TryGetProperty("ok", out var ok) ||
                ok.ValueKind != JsonValueKind.True)
            {
                var error = root.TryGetProperty("error", out var errorElement)
                    ? errorElement.GetString()
                    : "Score worker returned an invalid ready message.";
                throw new InvalidOperationException(error);
            }
        }

        private string FormatWorkerExitMessage(string message)
        {
            lock (_stderr)
            {
                return _stderr.Length == 0 ? message : $"{message} {_stderr}";
            }
        }

        private static string ResolveUvExecutable()
        {
            var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var pathCandidates = path
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(directory => Path.Combine(directory, "uv"));

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var knownCandidates = new[]
            {
                Path.Combine(home, ".local", "bin", "uv"),
                "/opt/homebrew/bin/uv",
                "/usr/local/bin/uv",
            };

            return pathCandidates
                .Concat(knownCandidates)
                .FirstOrDefault(File.Exists)
                ?? "uv";
        }

        private static string BuildProcessPath()
        {
            var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var extraPaths = new[]
            {
                Path.Combine(home, ".local", "bin"),
                "/opt/homebrew/bin",
                "/usr/local/bin",
            };

            return string.Join(
                Path.PathSeparator,
                extraPaths.Concat(path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)).Distinct());
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _process.StandardInput.WriteLine("{\"type\":\"shutdown\"}");
                    if (!_process.WaitForExit(2000))
                    {
                        _process.Kill(entireProcessTree: true);
                    }
                }
            }
            catch
            {
                try
                {
                    if (_process != null && !_process.HasExited)
                    {
                        _process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Best-effort shutdown only.
                }
            }
            finally
            {
                _process?.Dispose();
                _workerLock.Dispose();
            }
        }
    }
}
