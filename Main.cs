using OrbKit_Quest_Completer.CDPENGINE;
using OrbKit_Quest_Completer.CORE;
using OrbKit_Quest_Completer.GUI;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text.Json;

namespace OrbKit_Quest_Completer;

public partial class Main : Form
{
    private readonly string _appDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DiscordQuestCompleter");

    private readonly GameRegistry _registry;
    private readonly DummyExe _runner;
    private readonly CdpScriptProvider _scripts;
    private readonly DiscordCdpBridge _cdp;
    private List<DetectableGame> _allGames = [];
    private List<DiscordQuestInfo> _cdpQuests = [];
    private CancellationTokenSource? _activeCts;

    private int DurationMinutes => Math.Max(1, (int)Math.Round(_sldDuration.Value));
    private int TailMaxMinutes => Math.Max(0, (int)Math.Round(_sldTail.Value));
    private double VideoPaceMinSec => Math.Round((double)_sldVideoMin.Value * 2.0) / 2.0;
    private double VideoPaceMaxSec => Math.Round((double)_sldVideoMax.Value * 2.0) / 2.0;

    public Main()
    {
        InitializeComponent();

        _txtSearch.Base.PlaceholderText = "Search Discord detectable games (e.g. Where Winds Meet, Marvel Rivals, Rust)...";
        EnableDoubleBuffering(_lvGames);
        EnableDoubleBuffering(_lvQuests);

        _registry = new GameRegistry(Path.Combine(_appDataDir, "Cache"));
        string stubPath = Path.Combine(AppContext.BaseDirectory, "STUB", "StubHost.exe");
        _runner = new DummyExe(Path.Combine(_appDataDir, "Staging"), stubPath);
        _scripts = new CdpScriptProvider(Path.Combine(AppContext.BaseDirectory, "Scripts"));
        _cdp = new DiscordCdpBridge(_scripts, 9222);
    }

    private static void EnableDoubleBuffering(Control ctrl)
    {
        typeof(Control).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(ctrl, true, null);
    }

    private async void Form1_Load(object? sender, EventArgs e)
    {
        Version? ver = Assembly.GetExecutingAssembly().GetName().Version;
        string verText = ver is null ? "v1.0.0" : $"v{ver.Major}.{ver.Minor}.{ver.Build}";
        Text = $"OrbKit Quest Completer {verText}";
        themeContainer.SubHeader = $"{verText}";
        LoadSettings();
        UpdateSliderLabels();
        navManual.Selected = true;
        SyncRightRailPanel();
        await InitializeAsync();
    }

    private void btnWinMin_Click(object? sender, EventArgs e)
    {
        WindowState = FormWindowState.Minimized;
    }
    private void btnWinClose_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void sldDuration_ValueChanged(object? sender, EventArgs e)
    {
        UpdateSliderLabels();
    }

    private void sldTail_ValueChanged(object? sender, EventArgs e)
    {
        UpdateSliderLabels();
    }

    private void UpdateSliderLabels()
    {
        _lblDurValue.Text = $"{DurationMinutes} min";
        _lblTailValue.Text = TailMaxMinutes == 0 ? "Off" : $"{TailMaxMinutes} min";
    }
    private void NavTab_Click(object? sender, EventArgs e)
    {
        SyncRightRailPanel();
    }
    private void SyncRightRailPanel()
    {
        bool showGame = navManual.Selected;
        bool showVideo = navQuests.Selected;

        grpControls.Visible = showGame;
        grpVideoControls.Visible = showVideo;
        pnlControlsHost.Visible = showGame || showVideo;
        pnlRailGap.Visible = showGame || showVideo;
    }

    private void sldVideoMin_ValueChanged(object? sender, EventArgs e)
    {
        if (!_loadingSettings && _sldVideoMin.Value > _sldVideoMax.Value)
            _sldVideoMax.Value = _sldVideoMin.Value;

        UpdateVideoSliderLabels();
    }

    private void sldVideoMax_ValueChanged(object? sender, EventArgs e)
    {
        if (!_loadingSettings && _sldVideoMax.Value < _sldVideoMin.Value)
            _sldVideoMin.Value = _sldVideoMax.Value;

        UpdateVideoSliderLabels();
    }

    private void sldVideoPace_MouseUp(object? sender, MouseEventArgs e)
    {
        if (_loadingSettings) return;
        _settings.VideoPaceMinSec = VideoPaceMinSec;
        _settings.VideoPaceMaxSec = VideoPaceMaxSec;
        SaveSettings();
    }

    private void tglVideoAutoEnroll_CheckedChanged(object? sender, EventArgs e)
    {
        if (_loadingSettings) return;
        _settings.AutoEnrollVideo = _tglVideoAutoEnroll.Checked;
        SaveSettings();
    }

    private void btnResetVideoPace_Click(object? sender, EventArgs e)
    {
        _loadingSettings = true;
        _sldVideoMin.Value = 7.0m;
        _sldVideoMax.Value = 9.5m;
        _tglVideoAutoEnroll.Checked = true;
        _loadingSettings = false;

        UpdateVideoSliderLabels();
        _settings.VideoPaceMinSec = 7.0;
        _settings.VideoPaceMaxSec = 9.5;
        _settings.AutoEnrollVideo = true;
        SaveSettings();

        Log("Video quest heartbeat reset to Discord safe defaults (7.0–9.5s).", FDPalette.HudGreen);
    }

    private void btnFooterSite_Click(object? sender, EventArgs e)
    {
        OpenExternalUrl("https://voidtechstudios.us/");
    }

    private void btnFooterCoffee_Click(object? sender, EventArgs e)
    {
        OpenExternalUrl("https://buymeacoffee.com/rustforgedev");
    }

    private void OpenExternalUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log($"Could not open link: {ex.Message}", FDPalette.AccentOrange);
        }
    }

    private void UpdateVideoSliderLabels()
    {
        double min = VideoPaceMinSec;
        double max = Math.Max(min, VideoPaceMaxSec);

        _lblVideoMinValue.Text = $"{min:F1} s";
        _lblVideoMaxValue.Text = $"{max:F1} s";

        if (min < 6.5)
        {
            _lblVideoPaceHint.ForeColor = FDPalette.AccentOrange;
            _lblVideoPaceHint.Text = "Warning: Faster pings don't finish sooner & create extra API traffic.";
        }
        else if (min <= 8.0 && max >= 8.5 && max <= 11.0)
        {
            _lblVideoPaceHint.ForeColor = FDPalette.HudGreen;
            _lblVideoPaceHint.Text = "Matches Discord's video player heartbeat (recommended).";
        }
        else
        {
            _lblVideoPaceHint.ForeColor = FDPalette.TextSecondary;
            _lblVideoPaceHint.Text = "Slower interval: fewer API calls, slightly delayed completion tick. (SAFEST)";
        }
    }
    private async void btnConnectCdp_Click(object? sender, EventArgs e) => await RefreshCdpQuestsAsync();

    private void btnLaunchCdp_Click(object? sender, EventArgs e)
    {
        if (MessageBox.Show("Restart Discord with local CDP debugging enabled (127.0.0.1:9222)?",
            "Relaunch Discord", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            DiscordCdpBridge.TryRelaunchDiscordWithCdp(out string msg, 9222);
            Log(msg, FDPalette.AccentOrange);
        }
    }

    private void btnStop_Click(object? sender, EventArgs e)
    {
        StopActiveTask();
    }
    private void txtSearch_TextChanged(object? sender, EventArgs e)
    {
        FilterGamesList();
    }
    private async void btnRefreshReg_Click(object? sender, EventArgs e)
    {
        await LoadRegistryAsync(force: true);
    }
    private void lvGames_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateResolvedPreview();
    }
    private async void btnRunManual_Click(object? sender, EventArgs e)
    {
        await StartManualGameAsync();
    }
    private async void btnRefreshQuests_Click(object? sender, EventArgs e)
    {
        await RefreshCdpQuestsAsync();
    }
    private async void btnRunQuest_Click(object? sender, EventArgs e)
    {
        await StartSelectedCdpQuestAsync();
    }

    private void ListView_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        using var bg = new SolidBrush(FDPalette.SurfaceCard);
        using var border = new Pen(FDPalette.BorderSubtle, 1f);
        e.Graphics.FillRectangle(bg, e.Bounds);
        e.Graphics.DrawLine(border, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        e.Graphics.DrawLine(border, e.Bounds.Right - 1, e.Bounds.Top + 4, e.Bounds.Right - 1, e.Bounds.Bottom - 4);

        using var hdrFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        var textRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, Math.Max(1, e.Bounds.Width - 12), e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? "", hdrFont, textRect, FDPalette.AccentBrurple,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }

    private void ListView_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (e.Item is null) return;

        bool selected = e.Item.Selected;
        Color rowBg = selected
            ? FDPalette.SurfaceHover
            : (e.ItemIndex % 2 == 0 ? FDPalette.SurfaceInput : Color.FromArgb(21, 26, 33));

        using var bg = new SolidBrush(rowBg);
        e.Graphics.FillRectangle(bg, e.Bounds);

        if (selected && e.ColumnIndex == 0)
        {
            using var accentBar = new SolidBrush(FDPalette.AccentBrurple);
            e.Graphics.FillRectangle(accentBar, new Rectangle(e.Bounds.X, e.Bounds.Y, 3, e.Bounds.Height));
        }

        Color textCol = selected
            ? FDPalette.TextPrimary
            : (e.ColumnIndex == 0 ? FDPalette.TextPrimary : FDPalette.TextSecondary);

        var textRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, Math.Max(1, e.Bounds.Width - 12), e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, e.SubItem?.Text ?? "", e.Item.Font, textRect, textCol,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }

    private async Task InitializeAsync()
    {
        if (!_runner.StubExists)
        {
            _pillStub.Text = "Missing";
            Log($"WARNING: StubHost.exe missing at {_runner.ExpectedStubPath}", FDPalette.AccentOrange);
            Log("Make sure the STUB folder exists and contains StubHost.exe.", FDPalette.AccentOrange);
        }
        else
        {
            _pillStub.Text = "Ready";
            Log("StubHost.exe is verified and ready.", FDPalette.HudGreen);
        }

        _ = _scripts.GetActiveScript(out string? scriptWarn);
        UpdateScriptStatusUi();
        if (!string.IsNullOrEmpty(scriptWarn))
            Log(scriptWarn, FDPalette.AccentCrimson);
        else if (_scripts.IsUsingValidCustomScript)
            Log($"Loaded custom patch script from Scripts\\cdp_patch.js (v{_scripts.ActiveVersion}, SHA-256: {_scripts.ActiveShortHash}...).", FDPalette.AccentOrange);

        await LoadRegistryAsync(force: false);

        if (_settings.AutoConnectCdp)
        {
            if (await _cdp.FindDiscordWebSocketUrl() != null)
                await RefreshCdpQuestsAsync();
        }
        else
        {
            Log("Auto-CDP is OFF (safe mode). Click Connect CDP anytime to sync quests, orbs, and complete video quests.", FDPalette.TextMuted);
        }
    }

    private async Task LoadRegistryAsync(bool force)
    {
        try
        {
            Log(force ? "Refreshing Discord detectable games list..." : "Loading Discord detectable games list...", FDPalette.TextSecondary);
            _allGames = await _registry.GetGames(force);
            Log($"Loaded {_allGames.Count:N0} detectable games.", FDPalette.HudGreen);
            FilterGamesList();
        }
        catch (Exception ex)
        {
            Log($"Failed to load detectable games: {ex.Message}", FDPalette.AccentOrange);
        }
    }

    private void FilterGamesList()
    {
        string q = _txtSearch.Text.Trim();
        var matches = string.IsNullOrEmpty(q)
            ? _allGames.Take(150)
            : _allGames.Where(g => g.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || g.Id.Contains(q)).Take(150);

        _lvGames.BeginUpdate();
        _lvGames.Items.Clear();
        foreach (var g in matches)
        {
            var resolved = ExePicker.Pick(g.Executables, g.Name);
            var item = new ListViewItem(g.Name) { Tag = g };
            item.SubItems.Add(g.Id);
            item.SubItems.Add(resolved.RelativePath);
            _lvGames.Items.Add(item);
        }
        _lvGames.EndUpdate();
    }

    private void UpdateResolvedPreview()
    {
        _cmbExecutables.Items.Clear();
        if (_lvGames.SelectedItems.Count == 0 || _lvGames.SelectedItems[0].Tag is not DetectableGame g)
            return;

        foreach (var exe in ExePicker.GetAllWin32(g.Executables, g.Name))
            _cmbExecutables.Items.Add(exe);

        if (_cmbExecutables.Items.Count > 0)
            _cmbExecutables.SelectedIndex = 0;
    }

    private async Task RefreshCdpQuestsAsync()
    {
        try
        {
            var snap = await _cdp.FetchQuestSnapshot(new Progress<string>(m => Log(m, FDPalette.AccentOrange)));
            UpdateScriptStatusUi();
            _pillCdp.Text = "Connected";
            _pillOrbs.Text = snap.OrbBalance.HasValue ? $"{snap.OrbBalance.Value:N0}" : "N/A";

            bool flagged = IsFutureTimestamp(snap.SuspendedUntil) || IsFutureTimestamp(snap.EnrollmentBlockedUntil);
            if (flagged)
            {
                string until = snap.SuspendedUntil ?? snap.EnrollmentBlockedUntil ?? "unknown";
                _lblWarning.Text = $"STOPPED: Discord flagged quest access/enrollment until {until}. Temporarily disabled for account safety.";
                _lblWarning.Visible = true;
                _btnRunQuest.Enabled = false;
                _btnRunManual.Enabled = false;
                Log(_lblWarning.Text, FDPalette.AccentOrange);
            }
            else
            {
                _lblWarning.Visible = false;
            }

            _cdpQuests = snap.Quests;
            _lvQuests.BeginUpdate();
            _lvQuests.Items.Clear();
            foreach (var q in _cdpQuests)
            {
                var item = new ListViewItem(q.Name) { Tag = q };
                item.SubItems.Add(q.TaskType);
                item.SubItems.Add($"{Math.Floor(q.Progress)}s / {Math.Floor(q.Target)}s");
                item.SubItems.Add(q.RewardLabel);
                item.SubItems.Add(q.Enrolled ? "Yes" : "No");
                item.SubItems.Add(q.AppId ?? "N/A");
                _lvQuests.Items.Add(item);
            }
            _lvQuests.EndUpdate();

            Log($"CDP synced: {_cdpQuests.Count} active eligible quest(s) found.", FDPalette.HudGreen);
        }
        catch (Exception ex)
        {
            _pillCdp.Text = "Disconnected";
            Log($"CDP sync failed: {ex.Message}", FDPalette.AccentOrange);
        }
    }

    private async Task StartManualGameAsync()
    {
        if (_activeCts != null) { Log("A task is already running.", FDPalette.AccentOrange); return; }
        if (_lvGames.SelectedItems.Count == 0 || _lvGames.SelectedItems[0].Tag is not DetectableGame game)
        {
            Log("Select a game from the list first.", FDPalette.AccentOrange);
            return;
        }

        var selectedExe = _cmbExecutables.SelectedItem as ResolvedExecutable;

        await RunTrackedAsync(async (log, prog, ct) =>
        {
            await _runner.RunQuestSession(
                game,
                TimeSpan.FromMinutes(DurationMinutes),
                TailMaxMinutes,
                log,
                prog,
                ct,
                selectedExe);
        }, $"Playing {game.Name}");
    }

    private async Task StartSelectedCdpQuestAsync()
    {
        if (_activeCts != null) { Log("A task is already running.", FDPalette.AccentOrange); return; }
        if (_lvQuests.SelectedItems.Count == 0 || _lvQuests.SelectedItems[0].Tag is not DiscordQuestInfo quest)
        {
            Log("Select a quest from the Live Quests list first.", FDPalette.AccentOrange);
            return;
        }

        if (quest.TaskType == "VIDEO")
        {
            double minPace = VideoPaceMinSec;
            double maxPace = Math.Max(minPace, VideoPaceMaxSec);
            bool autoEnroll = _tglVideoAutoEnroll.Checked;

            await RunTrackedAsync(
                (log, prog, ct) => _cdp.RunVideoQuest(quest, log, prog, ct, minPace, maxPace, autoEnroll),
                $"Video: {quest.Name}");
        }
        else if (quest.TaskType == "GAME")
        {
            if (string.IsNullOrEmpty(quest.AppId))
            {
                Log($"Quest \"{quest.Name}\" has no application ID.", FDPalette.AccentOrange);
                return;
            }

            var game = _allGames.FirstOrDefault(g => g.Id == quest.AppId)
                ?? await _registry.FetchSingleApp(quest.AppId)
                ?? new DetectableGame { Id = quest.AppId, Name = quest.Name };

            double remainingSec = Math.Max(30, quest.Target - quest.Progress);

            await RunTrackedAsync(async (log, prog, ct) =>
            {
                if (!quest.Enrolled)
                {
                    log.Report($"Enrolling in \"{quest.Name}\"...");
                    await _cdp.EnrollQuestAsync(quest, log, ct);
                    await Task.Delay(Random.Shared.Next(1000, 1800), ct);
                }

                await _runner.RunQuestSession(
                    game,
                    TimeSpan.FromSeconds(remainingSec),
                    TailMaxMinutes,
                    log,
                    prog,
                    ct);
            }, $"Playing {game.Name}");
        }
        else
        {
            Log($"Task type \"{quest.TaskType}\" ({quest.TaskKey}) is intentionally skipped for account safety.", FDPalette.AccentOrange);
        }
    }

    private async Task RunTrackedAsync(
        Func<IProgress<string>, IProgress<SessionProgress>, CancellationToken, Task> work,
        string label)
    {
        _activeCts = new CancellationTokenSource();
        SetRunningState(true, label);

        var logProg = new Progress<string>(msg => Log(msg, FDPalette.TextPrimary));
        var sessionProg = new Progress<SessionProgress>(p =>
        {
            double total = Math.Max(1, p.TargetSeconds + p.TailSeconds);
            int pct = (int)Math.Clamp((p.ElapsedSeconds / total) * 100.0, 0, 100);
            _progressBar.Value = pct;
            _lblActiveStatus.Text = $"{label} - {p.Phase}: {TimeSpan.FromSeconds(p.ElapsedSeconds):mm\\:ss} / {TimeSpan.FromSeconds(p.TargetSeconds):mm\\:ss}" +
                                    (p.TailSeconds > 0 ? $" (+{p.TailSeconds:F0}s tail)" : "");
        });

        try
        {
            await work(logProg, sessionProg, _activeCts.Token);
            Log($"Finished: {label}. Claim your reward manually in Discord.", FDPalette.HudGreen);
        }
        catch (OperationCanceledException)
        {
            Log("Task stopped by user.", FDPalette.AccentOrange);
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}", FDPalette.AccentOrange);
        }
        finally
        {
            _activeCts?.Dispose();
            _activeCts = null;
            SetRunningState(false, "Idle | Ready");
        }
    }

    private void SetRunningState(bool running, string status)
    {
        _btnStop.Enabled = running;
        _btnRunManual.Enabled = !running && !_lblWarning.Visible;
        _btnRunQuest.Enabled = !running && !_lblWarning.Visible;
        _lblActiveStatus.Text = status;
        if (!running) _progressBar.Value = 0;
    }

    private void StopActiveTask()
    {
        _activeCts?.Cancel();
        _runner.StopAndCleanup(new Progress<string>(m => Log(m, FDPalette.AccentOrange)));
    }

    private static bool IsFutureTimestamp(string? raw) =>
        DateTimeOffset.TryParse(raw, out var dto) && dto > DateTimeOffset.UtcNow;

    private void Log(string message, Color color)
    {
        if (InvokeRequired) { BeginInvoke(() => Log(message, color)); return; }
        _rtbLog.AppendLog($"[{DateTime.Now:HH:mm:ss}] ", message, color);
    }

    private sealed class AppSettings
    {
        public bool AutoConnectCdp { get; set; } = false;
        public double VideoPaceMinSec { get; set; } = 7.0;
        public double VideoPaceMaxSec { get; set; } = 9.5;
        public bool AutoEnrollVideo { get; set; } = true;
    }

    private string SettingsPath => Path.Combine(_appDataDir, "settings.json");
    private AppSettings _settings = new();
    private bool _loadingSettings;

    private void LoadSettings()
    {
        _loadingSettings = true;
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            _settings = new AppSettings();
        }

        _tglAutoCdp.Checked = _settings.AutoConnectCdp;
        _sldVideoMin.Value = (decimal)Math.Clamp(_settings.VideoPaceMinSec, 3.0, 15.0);
        _sldVideoMax.Value = (decimal)Math.Clamp(Math.Max(_settings.VideoPaceMinSec, _settings.VideoPaceMaxSec), 4.0, 20.0);
        _tglVideoAutoEnroll.Checked = _settings.AutoEnrollVideo;
        UpdateVideoSliderLabels();
        _loadingSettings = false;
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(_appDataDir);
            string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            Log($"Could not save settings: {ex.Message}", FDPalette.AccentOrange);
        }
    }

    private async void tglAutoCdp_CheckedChanged(object? sender, EventArgs e)
    {
        if (_loadingSettings) return;

        _settings.AutoConnectCdp = _tglAutoCdp.Checked;
        SaveSettings();

        Log($"Auto-connect CDP on startup: {(_settings.AutoConnectCdp ? "Enabled" : "Disabled")}.", FDPalette.TextSecondary);

        if (_settings.AutoConnectCdp && _pillCdp.Text != "Connected")
        {
            if (await _cdp.FindDiscordWebSocketUrl() != null)
                await RefreshCdpQuestsAsync();
        }
    }

    private void UpdateScriptStatusUi()
    {
        _lblScriptStatus.Text = _scripts.StatusSummary;
        _lblScriptStatus.ForeColor = _scripts.IsUsingValidCustomScript
            ? FDPalette.AccentOrange
            : FDPalette.TextSecondary;
        btnResetScript.Enabled = _scripts.HasCustomFile;
    }

    private async void btnImportScript_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Import CDP Patch Script",
            Filter = "JavaScript Files (*.js)|*.js|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        string content;
        try
        {
            content = File.ReadAllText(dlg.FileName);
        }
        catch (Exception ex)
        {
            Log($"Could not read patch script file: {ex.Message}", FDPalette.AccentCrimson);
            return;
        }

        var staticCheck = CdpScriptValidator.ValidateStatic(content);
        if (!staticCheck.IsValid)
        {
            string report = string.Join(Environment.NewLine + "• ", staticCheck.Errors);
            Log($"Import rejected ({staticCheck.Errors.Count} issue(s)):\n• {report}", FDPalette.AccentCrimson);
            MessageBox.Show($"Patch script failed validation and was NOT imported:\n\n• {report}",
                "Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        bool v8Checked = false;
        if (await _cdp.FindDiscordWebSocketUrl() != null)
        {
            var (ok, v8Error) = await _cdp.CompileCheck(content);
            if (!ok)
            {
                Log($"V8 compilation check failed: {v8Error}", FDPalette.AccentCrimson);
                MessageBox.Show($"Discord's V8 engine rejected the patch script syntax:\n\n{v8Error}",
                    "JavaScript Syntax Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            v8Checked = true;
        }

        string confirmMsg =
            $"File: {Path.GetFileName(dlg.FileName)}\n" +
            $"Patch Script Version: v{staticCheck.Version}\n" +
            $"Security Scan: PASSED\n" +
            $"Syntax Check: {(v8Checked ? "PASSED (security check success)" : "Skipped (CDP not connected)")}\n\n" +
            $"SHA-256:\n{staticCheck.Sha256}\n\n" +
            $"This patch script will be saved to:\n{_scripts.CustomScriptPath}\n\n" +
            "Only import scripts from trusted sources! Activate this script?";

        if (MessageBox.Show(confirmMsg, "Confirm Patch Script Import", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            _scripts.ImportValidatedContent(content);
            UpdateScriptStatusUi();
            Log($"Activated custom CDP patch script v{staticCheck.Version} (SHA-256: {staticCheck.Sha256[..12]}...).", FDPalette.HudGreen);

            if (_pillCdp.Text == "Connected")
                await RefreshCdpQuestsAsync();
        }
        catch (Exception ex)
        {
            Log($"Failed to save custom patch script: {ex.Message}", FDPalette.AccentCrimson);
            MessageBox.Show(ex.Message, "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnExportScript_Click(object? sender, EventArgs e)
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Export CDP Patch Script",
            Filter = "JavaScript Files (*.js)|*.js",
            InitialDirectory = Directory.Exists(_scripts.ScriptsDirectory) ? _scripts.ScriptsDirectory : AppContext.BaseDirectory,
            FileName = "cdp_patch.js"
        };

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            _scripts.ExportTemplateTo(dlg.FileName, builtInOnly: false);
            Log($"Exported CDP patch script to {dlg.FileName}.", FDPalette.HudGreen);
        }
        catch (Exception ex)
        {
            Log($"Failed to export patch script: {ex.Message}", FDPalette.AccentCrimson);
        }
    }

    private void btnScriptsFolder_Click(object? sender, EventArgs e)
    {
        try
        {
            Directory.CreateDirectory(_scripts.ScriptsDirectory);
            Process.Start(new ProcessStartInfo("explorer.exe", _scripts.ScriptsDirectory) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log($"Could not open Scripts folder: {ex.Message}", FDPalette.AccentOrange);
        }
    }

    private async void btnResetScript_Click(object? sender, EventArgs e)
    {
        _scripts.ResetToDefault();
        UpdateScriptStatusUi();
        Log("Removed custom Scripts\\cdp_patch.js and reverted to built-in patch script.", FDPalette.TextSecondary);

        if (_pillCdp.Text == "Connected")
            await RefreshCdpQuestsAsync();
    }
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        StopActiveTask();
        _runner.Dispose();
        base.OnFormClosing(e);
    }
}
