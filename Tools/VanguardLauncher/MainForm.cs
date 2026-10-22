using System.Diagnostics;
using System.Reflection;
using VanguardLauncher.Core;

namespace VanguardLauncher;

public sealed class MainForm : Form
{
    private static readonly Color Background = Color.FromArgb(22, 24, 30);
    private static readonly Color Panel = Color.FromArgb(31, 34, 42);
    private static readonly Color Accent = Color.FromArgb(190, 42, 44);
    private static readonly Color Muted = Color.FromArgb(160, 166, 178);

    private readonly Label _status = new();
    private readonly Label _detail = new();
    private readonly Label _versions = new();
    private readonly Button _play = new();
    private readonly Button _update = new();
    private readonly Button _browse = new();
    private readonly Button _folder = new();
    private readonly ProgressBar _progress = new();
    private readonly LinkLabel _link = new();

    private LauncherSettings _settings = new();
    private string? _gameDir;
    private bool _busy;

    private static string LauncherVersion
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version is null ? "0.3.0" : $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    public MainForm()
    {
        Text = "Generals Vanguard";
        ClientSize = new Size(640, 400);
        MinimumSize = new Size(640, 400);
        MaximumSize = new Size(640, 400);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        var title = new Label
        {
            Text = "GENERALS VANGUARD",
            Font = new Font("Segoe UI", 23F, FontStyle.Bold),
            ForeColor = Accent,
            AutoSize = true,
            Location = new Point(26, 22),
        };
        var subtitle = new Label
        {
            Text = "A fourth side for Zero Hour \u2014 launcher and updates",
            ForeColor = Muted,
            AutoSize = true,
            Location = new Point(29, 64),
        };

        _status.Text = "Starting\u2026";
        _status.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _status.AutoSize = false;
        _status.Location = new Point(29, 110);
        _status.Size = new Size(580, 26);

        _detail.Text = "";
        _detail.ForeColor = Muted;
        _detail.AutoSize = false;
        _detail.Location = new Point(29, 138);
        _detail.Size = new Size(580, 42);

        _progress.Location = new Point(29, 188);
        _progress.Size = new Size(580, 10);
        _progress.Style = ProgressBarStyle.Continuous;
        _progress.Visible = false;

        ConfigureButton(_play, "Play", 29, 216, 184, Accent, Color.White);
        ConfigureButton(_update, "Check for updates", 223, 216, 184, Panel, Color.White);
        ConfigureButton(_browse, "Set game folder", 417, 216, 192, Panel, Color.White);
        ConfigureButton(_folder, "Open game folder", 29, 264, 184, Panel, Color.White);

        _versions.Text = $"Launcher {LauncherVersion}";
        _versions.ForeColor = Muted;
        _versions.AutoSize = true;
        _versions.Location = new Point(29, 330);

        _link.Text = "github.com/KuiseRapper/generals-vanguard/releases";
        _link.LinkColor = Muted;
        _link.ActiveLinkColor = Color.White;
        _link.VisitedLinkColor = Muted;
        _link.AutoSize = true;
        _link.Location = new Point(29, 352);
        _link.LinkClicked += (_, _) => OpenUrl(UpdateService.ReleasePage);

        Controls.AddRange(new Control[]
        {
            title, subtitle, _status, _detail, _progress, _play, _update, _browse, _folder,
            _versions, _link,
        });

        AcceptButton = _play;
        _play.Click += async (_, _) => await PlayAsync();
        _update.Click += async (_, _) => await UpdateAsync();
        _browse.Click += (_, _) => BrowseGame();
        _folder.Click += (_, _) => OpenGameFolder();
        Shown += async (_, _) => await OnShownAsync();
    }

    private static void ConfigureButton(
        Button button, string text, int x, int y, int width, Color back, Color fore)
    {
        button.Text = text;
        button.Location = new Point(x, y);
        button.Size = new Size(width, 40);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(back, 0.18f);
        button.BackColor = back;
        button.ForeColor = fore;
        button.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
    }

    private async Task OnShownAsync()
    {
        _settings = SettingsStore.Load();
        _gameDir = GameLocator.Find(_settings.GamePath);
        RefreshState();
        if (_settings.CheckOnStartup)
            await CheckAsync(silent: true);
    }

    private void RefreshState()
    {
        _gameDir = GameLocator.Find(_settings.GamePath);
        var installed = GameLocator.InstalledVersion(_gameDir);
        if (!_busy)
        {
            if (_gameDir is null)
            {
                _status.Text = "Game not installed";
                _detail.Text = "Check for updates to download the latest Vanguard build.";
            }
            else
            {
                _status.Text = "Ready to play";
                _detail.Text = installed is null
                    ? $"Local build at {_gameDir}"
                    : $"Game {installed} at {_gameDir}";
            }
        }
        _versions.Text = $"Launcher {LauncherVersion}  \u2022  Game {installed ?? "not installed"}";
        _play.Enabled = !_busy;
    }

    private void SetBusy(bool busy, string? status = null, bool marquee = false)
    {
        _busy = busy;
        _play.Enabled = !busy;
        _update.Enabled = !busy;
        _browse.Enabled = !busy;
        _folder.Enabled = !busy;
        _progress.Visible = busy;
        _progress.Style = marquee ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
        if (status is not null)
            _status.Text = status;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private async Task CheckAsync(bool silent)
    {
        if (_busy)
            return;
        SetBusy(true, "Checking for updates\u2026", marquee: true);
        try
        {
            var manifest = await UpdateService.FetchManifestAsync(_settings.ManifestUrl);
            var installed = GameLocator.InstalledVersion(_gameDir);
            var gameNewer = _gameDir is null || installed is null
                || UpdateService.IsNewer(manifest.Game!.Version, installed);
            var launcherNewer = manifest.Launcher is not null
                && UpdateService.IsNewer(manifest.Launcher.Version, LauncherVersion);

            if (gameNewer)
            {
                _status.Text = $"Game update {manifest.Game!.Version} available";
                _detail.Text = _gameDir is null
                    ? "Download it with Check for updates."
                    : $"Installed {installed ?? "local build"} \u2192 {manifest.Game.Version}.";
            }
            else if (launcherNewer)
            {
                _status.Text = $"Launcher update {manifest.Launcher!.Version} available";
                _detail.Text = "Use Check for updates to install it.";
            }
            else
            {
                _status.Text = "Everything is up to date";
                _detail.Text = $"Game {installed ?? manifest.Game!.Version} \u2022 launcher {LauncherVersion}.";
            }
        }
        catch (Exception ex)
        {
            _status.Text = silent ? "Could not check for updates" : "Update check failed";
            _detail.Text = ex.Message;
            if (!silent)
                MessageBox.Show(this, ex.Message, "Update check failed", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
        }
        finally
        {
            SetBusy(false);
            RefreshState();
        }
    }

    private async Task UpdateAsync()
    {
        if (_busy)
            return;
        if (Process.GetProcessesByName("generals").Length > 0)
        {
            MessageBox.Show(this, "Close the game before updating.", "The game is running",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        SetBusy(true, "Checking for updates\u2026", marquee: true);
        try
        {
            var manifest = await UpdateService.FetchManifestAsync(_settings.ManifestUrl);
            var installed = GameLocator.InstalledVersion(_gameDir);
            var gameNewer = _gameDir is null || installed is null
                || UpdateService.IsNewer(manifest.Game!.Version, installed);
            var launcherNewer = manifest.Launcher is not null
                && UpdateService.IsNewer(manifest.Launcher.Version, LauncherVersion);

            if (gameNewer)
            {
                SetBusy(true, $"Downloading game {manifest.Game!.Version}\u2026");
                _progress.Style = ProgressBarStyle.Continuous;
                _progress.Value = 0;
                var progress = new Progress<double>(p =>
                {
                    _progress.Value = Math.Clamp((int)Math.Round(p * 100), 0, 100);
                    _detail.Text = $"Downloading game {manifest.Game.Version}: {p:P0}";
                });
                await UpdateService.InstallGameAsync(
                    manifest.Game, GameLocator.DefaultGameDirectory, progress);
                _settings.GamePath = GameLocator.DefaultGameDirectory;
                SettingsStore.Save(_settings);
                _gameDir = GameLocator.DefaultGameDirectory;
                _status.Text = $"Game {manifest.Game.Version} installed";
                _detail.Text = _gameDir;
            }
            else
            {
                _status.Text = "Game is up to date";
            }

            if (launcherNewer)
            {
                var answer = MessageBox.Show(this,
                    $"Launcher {manifest.Launcher!.Version} is available. Update and restart now?",
                    "Launcher update", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer == DialogResult.Yes)
                {
                    SetBusy(true, $"Downloading launcher {manifest.Launcher.Version}\u2026");
                    _progress.Value = 0;
                    var progress = new Progress<double>(p =>
                    {
                        _progress.Value = Math.Clamp((int)Math.Round(p * 100), 0, 100);
                        _detail.Text = $"Downloading launcher: {p:P0}";
                    });
                    var newExe = await UpdateService.DownloadLauncherAsync(
                        manifest.Launcher, progress);
                    SelfUpdater.Launch(newExe, Environment.ProcessPath
                        ?? throw new InvalidOperationException("The launcher path is unknown."));
                    Application.Exit();
                    return;
                }
            }

            if (!gameNewer && !launcherNewer)
                _status.Text = "Everything is up to date";
        }
        catch (Exception ex)
        {
            _status.Text = "Update failed";
            _detail.Text = ex.Message;
            MessageBox.Show(this, ex.Message, "Update failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (!IsDisposed)
            {
                SetBusy(false);
                RefreshState();
            }
        }
    }

    private async Task PlayAsync()
    {
        if (_busy)
            return;
        if (_gameDir is null)
        {
            await UpdateAsync();
            if (_gameDir is null)
                return;
        }
        try
        {
            GameLocator.Start(_gameDir);
            _status.Text = "Game started";
            _detail.Text = "Have fun. Close this launcher whenever you like.";
        }
        catch (Exception ex)
        {
            _status.Text = "Could not start the game";
            _detail.Text = ex.Message;
            MessageBox.Show(this, ex.Message, "Could not start the game", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void BrowseGame()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the folder that holds generals.exe",
            SelectedPath = _gameDir ?? GameLocator.DefaultGameDirectory,
            ShowNewFolderButton = false,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        if (!File.Exists(Path.Combine(dialog.SelectedPath, "generals.exe")))
        {
            MessageBox.Show(this, "That folder has no generals.exe.", "Not a game folder",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _settings.GamePath = dialog.SelectedPath;
        SettingsStore.Save(_settings);
        RefreshState();
    }

    private void OpenGameFolder()
    {
        var directory = _gameDir ?? GameLocator.LauncherDirectory;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = directory, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open the folder", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open the page", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
