using System.Diagnostics;

namespace OrbKit_Quest_Completer.CORE;

public sealed class DummyExe : IDisposable
{
    private readonly string _stagingRoot;
    private readonly string _stubExePath;
    private Process? _activeProcess;
    private string? _stagedFilePath;

    public DummyExe(string stagingRoot, string stubExePath)
    {
        _stagingRoot = stagingRoot;
        _stubExePath = stubExePath;
    }

    public bool StubExists => File.Exists(_stubExePath);
    public string ExpectedStubPath => _stubExePath;

    public async Task RunQuestSession(
     DetectableGame game,
     TimeSpan questDuration,
     int tailMaxMinutes = 3,
     IProgress<string>? log = null,
     IProgress<SessionProgress>? progress = null,
     CancellationToken ct = default,
     ResolvedExecutable? exeOverride = null)
    {
        if (!File.Exists(_stubExePath))
            throw new FileNotFoundException($"StubHost.exe not found at: {_stubExePath}. Publish Discord_Dummy_Host first.");

        var resolved = exeOverride ?? ExePicker.Pick(game.Executables, game.Name);
        string targetPath = Path.Combine(_stagingRoot, resolved.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        string targetDir = Path.GetDirectoryName(targetPath)!;

        Directory.CreateDirectory(targetDir);
        File.Copy(_stubExePath, targetPath, overwrite: true);
        _stagedFilePath = targetPath;

        int tailSeconds = tailMaxMinutes > 0
            ? Random.Shared.Next((int)(tailMaxMinutes * 60 * 0.4), tailMaxMinutes * 60 + 1)
            : 0;

        double targetSec = Math.Max(1, questDuration.TotalSeconds);
        double totalSec = targetSec + tailSeconds;

        var psi = new ProcessStartInfo
        {
            FileName = targetPath,
            WorkingDirectory = targetDir,
            UseShellExecute = false
        };

        log?.Report($"Launching {resolved.ExeName} for \"{game.Name}\"");
        log?.Report($"Staged path: {targetPath}");
        log?.Report($"Duration: {questDuration:mm\\:ss} + random overshoot tail: {tailSeconds}s");

        _activeProcess = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start {targetPath}");

        var sw = Stopwatch.StartNew();
        try
        {
            while (sw.Elapsed.TotalSeconds < totalSec)
            {
                ct.ThrowIfCancellationRequested();

                if (_activeProcess.HasExited)
                    throw new InvalidOperationException($"Stub process exited unexpectedly (code {_activeProcess.ExitCode}). Make sure Discord_Dummy_Host was published with PublishSingleFile=true.");

                double elapsed = sw.Elapsed.TotalSeconds;
                string phase = elapsed < targetSec ? "Quest Progress" : "Overshoot Tail";
                progress?.Report(new SessionProgress(elapsed, targetSec, tailSeconds, phase));

                await Task.Delay(1000, ct);
            }

            progress?.Report(new SessionProgress(totalSec, targetSec, tailSeconds, "Complete"));
            log?.Report($"Session finished for \"{game.Name}\". Releasing process...");
        }
        finally
        {
            StopAndCleanup(log);
        }
    }

    public void StopAndCleanup(IProgress<string>? log = null)
    {
        if (_activeProcess is { HasExited: false })
        {
            try
            {
                _activeProcess.Kill(entireProcessTree: true);
                _activeProcess.WaitForExit(3000);
                log?.Report("Stopped dummy process.");
            }
            catch (Exception ex)
            {
                log?.Report($"Process cleanup warning: {ex.Message}");
            }
        }

        _activeProcess?.Dispose();
        _activeProcess = null;

        if (!string.IsNullOrEmpty(_stagedFilePath) && File.Exists(_stagedFilePath))
        {
            for (int attempt = 0; attempt < 4; attempt++)
            {
                try
                {
                    File.Delete(_stagedFilePath);
                    break;
                }
                catch
                {
                    Thread.Sleep(150);
                }
            }
            _stagedFilePath = null;
        }
    }

    public void Dispose() => StopAndCleanup();
}