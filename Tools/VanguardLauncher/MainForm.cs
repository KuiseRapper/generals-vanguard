using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using VanguardLauncher.Core;

namespace VanguardLauncher;

public sealed partial class MainForm : Form
{
    private const int FormWidth = 1000;
    private const int FormHeight = 640;
    private const int TopHeight = 42;
    private const int BannerHeight = 32;

    private static readonly Color Back = Color.FromArgb(8, 11, 18);
    private static readonly Color SideBack = Color.FromArgb(12, 17, 27);
    private static readonly Color PanelBack = Color.FromArgb(20, 27, 42);
    private static readonly Color Gold = Color.FromArgb(242, 201, 76);
    private static readonly Color Red = Color.FromArgb(176, 30, 36);
    private static readonly Color Ink = Color.FromArgb(233, 239, 249);
    private static readonly Color Muted = Color.FromArgb(143, 162, 191);

    private readonly Panel _topBar = new();
    private readonly Label _brand = new();
    private readonly Label _banner = new();
    private readonly Panel _content = new();
    private readonly Panel _main = new();
    private readonly Panel _update = new();
    private readonly PictureBox _hero = new();
    private readonly Panel _side = new();
    private readonly Label _installKicker = new();
    private readonly Label _ready = new();
    private readonly Label _gameCaption = new();
    private readonly Label _gameVersion = new();
    private readonly Label _launcherCaption = new();
    private readonly Label _launcherVersion = new();
    private readonly Label _status = new();
    private readonly Label _zeroHour = new();
    private readonly Label _baseInfo = new();
    private readonly Label _installSummary = new();
    private readonly Label _modInfo = new();
    private readonly Button _play = new();
    private readonly Button _check = new();
    private readonly Button _modsButton = new();
    private readonly Button _setFolder = new();
    private readonly Button _openFolder = new();
    private readonly LinkLabel _locateZeroHour = new();
    private readonly LinkLabel _releases = new();

    private readonly Label _updateKicker = new();
    private readonly Label _updateTitle = new();
    private readonly TextBox _notes = new();
    private readonly Label _updateSummary = new();
    private readonly ProgressBar _updateProgress = new();
    private readonly Label _updateStatus = new();
    private readonly Button _updateNow = new();
    private readonly Button _cancel = new();

    private LauncherSettings _settings = new();
    private LauncherManifest? _manifest;
    private string? _gameDir;
    private bool _busy;
    private readonly bool _installOnStart;

    private static string LauncherVersion
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version is null ? "0.6.0" : $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    public MainForm() : this(Array.Empty<string>())
    {
    }

    public MainForm(string[] args)
    {
        _installOnStart = args.Any(argument =>
            argument.Equals("--install", StringComparison.OrdinalIgnoreCase));

        Text = "Generals Vanguard";
        ClientSize = new Size(FormWidth, FormHeight);
        MinimumSize = MaximumSize = Size;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Back;
        ForeColor = Ink;
        Font = new Font("Segoe UI", 9F);
        KeyPreview = true;
        try
        {
            Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? Application.ExecutablePath);
        }
        catch
        {
            // The exe icon is a nicety, not a start requirement.
        }

        BuildTopBar();
        BuildBanner();
        BuildContent();
        BuildUpdateScreen();
        BuildModsScreen();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Escape || _busy)
                return;
            if (_mods.Visible)
                ShowMain();
            else
                Close();
        };
        Shown += async (_, _) => await OnShownAsync();
    }

    private void BuildTopBar()
    {
        _topBar.Location = new Point(0, 0);
        _topBar.Size = new Size(FormWidth, TopHeight);
        _topBar.BackColor = Color.FromArgb(9, 13, 21);
        AttachDrag(_topBar);

        _brand.Text = "GENERALS VANGUARD";
        _brand.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        _brand.ForeColor = Ink;
        _brand.AutoSize = true;
        _brand.Location = new Point(18, 11);
        AttachDrag(_brand);

        var launcher = new Label
        {
            Text = "LAUNCHER",
            Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
            ForeColor = Red,
            AutoSize = true,
            Location = new Point(190, 16),
        };
        AttachDrag(launcher);

        var minimize = WindowButton("\u2014", FormWidth - 96);
        minimize.Click += (_, _) => WindowState = FormWindowState.Minimized;
        var close = WindowButton("\u00d7", FormWidth - 46);
        close.Click += (_, _) => Close();

        _topBar.Controls.AddRange(new Control[] { _brand, launcher, minimize, close });
        Controls.Add(_topBar);
    }

    private Button WindowButton(string text, int x)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, 6),
            Size = new Size(40, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(9, 13, 21),
            ForeColor = Muted,
            Font = new Font("Segoe UI", 11F),
            TabStop = false,
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 38, 55);
        return button;
    }

    private void BuildBanner()
    {
        _banner.Location = new Point(0, TopHeight);
        _banner.Size = new Size(FormWidth, BannerHeight);
        _banner.BackColor = Color.FromArgb(52, 41, 15);
        _banner.ForeColor = Gold;
        _banner.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _banner.TextAlign = ContentAlignment.MiddleLeft;
        _banner.Padding = new Padding(18, 0, 0, 0);
        _banner.Visible = false;
        _banner.Cursor = Cursors.Hand;
        _banner.Click += (_, _) =>
        {
            if (_manifest is not null && (_gameDir is null
                || UpdateService.IsNewer(_manifest.Game!.Version,
                    GameLocator.InstalledVersion(_gameDir) ?? "0")))
                ShowUpdateScreen(_manifest);
            else
                _ = CheckAsync(silent: false);
        };
        Controls.Add(_banner);
    }

    private void BuildContent()
    {
        _content.Location = new Point(0, TopHeight + BannerHeight);
        _content.Size = new Size(FormWidth, FormHeight - TopHeight - BannerHeight);
        _content.BackColor = Back;

        _main.Dock = DockStyle.Fill;
        _main.BackColor = Back;
        _update.Dock = DockStyle.Fill;
        _update.BackColor = Back;
        _update.Visible = false;
        _mods.Dock = DockStyle.Fill;
        _mods.BackColor = Back;
        _mods.Visible = false;

        _hero.Location = new Point(0, 0);
        _hero.Size = new Size(600, _content.Height);
        _hero.SizeMode = PictureBoxSizeMode.Zoom;
        _hero.BackColor = Color.FromArgb(6, 9, 15);
        LoadHero();

        _side.Location = new Point(600, 0);
        _side.Size = new Size(400, _content.Height);
        _side.BackColor = SideBack;

        _installKicker.Text = "INSTALLATION";
        _installKicker.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        _installKicker.ForeColor = Muted;
        _installKicker.AutoSize = true;
        _installKicker.Location = new Point(28, 30);

        _ready.Text = "Not installed";
        _ready.Font = new Font("Segoe UI", 21F, FontStyle.Bold);
        _ready.ForeColor = Ink;
        _ready.AutoSize = true;
        _ready.Location = new Point(28, 50);

        _gameCaption.Text = "GAME";
        _gameCaption.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
        _gameCaption.ForeColor = Muted;
        _gameCaption.AutoSize = true;
        _gameCaption.Location = new Point(28, 120);

        _gameVersion.Text = "\u2014";
        _gameVersion.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        _gameVersion.ForeColor = Ink;
        _gameVersion.AutoSize = true;
        _gameVersion.Location = new Point(28, 138);

        _launcherCaption.Text = "LAUNCHER";
        _launcherCaption.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
        _launcherCaption.ForeColor = Muted;
        _launcherCaption.AutoSize = true;
        _launcherCaption.Location = new Point(210, 120);

        _launcherVersion.Text = LauncherVersion;
        _launcherVersion.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        _launcherVersion.ForeColor = Ink;
        _launcherVersion.AutoSize = true;
        _launcherVersion.Location = new Point(210, 138);

        _play.Text = "Play";
        _play.Location = new Point(28, 210);
        _play.Size = new Size(344, 52);
        _play.BackColor = Gold;
        _play.ForeColor = Color.FromArgb(16, 18, 22);
        _play.FlatStyle = FlatStyle.Flat;
        _play.FlatAppearance.BorderSize = 0;
        _play.FlatAppearance.MouseOverBackColor = Color.FromArgb(250, 214, 108);
        _play.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _play.Cursor = Cursors.Hand;
        _play.Click += async (_, _) => await PlayAsync();

        _check.Text = "Check for updates";
        _check.Location = new Point(28, 276);
        _check.Size = new Size(344, 38);
        _check.BackColor = PanelBack;
        _check.ForeColor = Ink;
        _check.FlatStyle = FlatStyle.Flat;
        _check.FlatAppearance.BorderSize = 0;
        _check.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 39, 58);
        _check.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _check.Cursor = Cursors.Hand;
        _check.Click += async (_, _) => await CheckAsync(silent: false);

        _modsButton.Text = "Mods";
        _modsButton.Location = new Point(28, 322);
        _modsButton.Size = new Size(344, 38);
        _modsButton.BackColor = PanelBack;
        _modsButton.ForeColor = Gold;
        _modsButton.FlatStyle = FlatStyle.Flat;
        _modsButton.FlatAppearance.BorderSize = 0;
        _modsButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 39, 58);
        _modsButton.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _modsButton.Cursor = Cursors.Hand;
        _modsButton.Click += (_, _) => ShowModsScreen();

        _setFolder.Text = "Set game folder";
        _setFolder.Location = new Point(28, 370);
        _setFolder.Size = new Size(166, 30);
        _setFolder.BackColor = Back;
        _setFolder.ForeColor = Muted;
        _setFolder.FlatStyle = FlatStyle.Flat;
        _setFolder.FlatAppearance.BorderSize = 0;
        _setFolder.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 27, 42);
        _setFolder.Font = new Font("Segoe UI", 8.5F);
        _setFolder.Cursor = Cursors.Hand;
        _setFolder.Click += (_, _) => SetGameFolder();

        _openFolder.Text = "Open game folder";
        _openFolder.Location = new Point(206, 370);
        _openFolder.Size = new Size(166, 30);
        _openFolder.BackColor = Back;
        _openFolder.ForeColor = Muted;
        _openFolder.FlatStyle = FlatStyle.Flat;
        _openFolder.FlatAppearance.BorderSize = 0;
        _openFolder.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 27, 42);
        _openFolder.Font = new Font("Segoe UI", 8.5F);
        _openFolder.Cursor = Cursors.Hand;
        _openFolder.Click += (_, _) => OpenGameFolder();

        _status.Text = "";
        _status.ForeColor = Muted;
        _status.Font = new Font("Segoe UI", 9F);
        _status.AutoSize = false;
        _status.AutoEllipsis = true;
        _status.Location = new Point(28, 408);
        _status.Size = new Size(344, 20);

        _zeroHour.Text = "Zero Hour: looking\u2026";
        _zeroHour.ForeColor = Muted;
        _zeroHour.Font = new Font("Segoe UI", 8F);
        _zeroHour.AutoSize = false;
        _zeroHour.AutoEllipsis = true;
        _zeroHour.Location = new Point(28, 430);
        _zeroHour.Size = new Size(344, 18);

        _baseInfo.Text = "";
        _baseInfo.ForeColor = Muted;
        _baseInfo.Font = new Font("Segoe UI", 8F);
        _baseInfo.AutoSize = false;
        _baseInfo.AutoEllipsis = true;
        _baseInfo.Location = new Point(28, 450);
        _baseInfo.Size = new Size(344, 18);

        _installSummary.Text = "";
        _installSummary.ForeColor = Muted;
        _installSummary.Font = new Font("Segoe UI", 8F);
        _installSummary.AutoSize = false;
        _installSummary.AutoEllipsis = true;
        _installSummary.Location = new Point(28, 470);
        _installSummary.Size = new Size(344, 18);

        _modInfo.Text = "Mod: vanilla Vanguard";
        _modInfo.ForeColor = Muted;
        _modInfo.Font = new Font("Segoe UI", 8F);
        _modInfo.AutoSize = false;
        _modInfo.AutoEllipsis = true;
        _modInfo.Location = new Point(28, 490);
        _modInfo.Size = new Size(344, 18);

        _locateZeroHour.Text = "Locate Zero Hour";
        _locateZeroHour.LinkColor = Muted;
        _locateZeroHour.ActiveLinkColor = Ink;
        _locateZeroHour.VisitedLinkColor = Muted;
        _locateZeroHour.Font = new Font("Segoe UI", 8F);
        _locateZeroHour.AutoSize = true;
        _locateZeroHour.Location = new Point(28, 512);
        _locateZeroHour.LinkClicked += (_, _) => LocateZeroHour();

        _releases.Text = "GitHub releases";
        _releases.LinkColor = Muted;
        _releases.ActiveLinkColor = Ink;
        _releases.VisitedLinkColor = Muted;
        _releases.Font = new Font("Segoe UI", 8F);
        _releases.AutoSize = true;
        _releases.Location = new Point(145, 512);
        _releases.LinkClicked += (_, _) => OpenUrl(UpdateService.ReleasePage);

        _side.Controls.AddRange(new Control[]
        {
            _installKicker, _ready, _gameCaption, _gameVersion, _launcherCaption, _launcherVersion,
            _play, _check, _modsButton, _setFolder, _openFolder, _status, _zeroHour, _baseInfo,
            _installSummary, _modInfo, _locateZeroHour, _releases,
        });

        _main.Controls.Add(_hero);
        _main.Controls.Add(_side);
        _content.Controls.Add(_main);
        _content.Controls.Add(_update);
        _content.Controls.Add(_mods);
        Controls.Add(_content);
    }

    private void BuildUpdateScreen()
    {
        _updateKicker.Text = "REVIEW / UPDATE";
        _updateKicker.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _updateKicker.ForeColor = Muted;
        _updateKicker.AutoSize = true;
        _updateKicker.Location = new Point(40, 34);

        _updateTitle.Text = "Up to date";
        _updateTitle.Font = new Font("Segoe UI", 21F, FontStyle.Bold);
        _updateTitle.ForeColor = Ink;
        _updateTitle.AutoSize = true;
        _updateTitle.Location = new Point(40, 56);

        _notes.Location = new Point(40, 112);
        _notes.Size = new Size(920, 286);
        _notes.Multiline = true;
        _notes.ReadOnly = true;
        _notes.ScrollBars = ScrollBars.Vertical;
        _notes.BorderStyle = BorderStyle.None;
        _notes.BackColor = SideBack;
        _notes.ForeColor = Color.FromArgb(201, 210, 227);
        _notes.Font = new Font("Segoe UI", 9.5F);
        _notes.TabStop = false;

        _updateSummary.Text = "";
        _updateSummary.ForeColor = Muted;
        _updateSummary.Font = new Font("Segoe UI", 9F);
        _updateSummary.AutoSize = false;
        _updateSummary.Location = new Point(40, 414);
        _updateSummary.Size = new Size(920, 20);

        _updateProgress.Location = new Point(40, 444);
        _updateProgress.Size = new Size(400, 8);
        _updateProgress.Visible = false;

        _updateStatus.Text = "";
        _updateStatus.ForeColor = Muted;
        _updateStatus.Font = new Font("Segoe UI", 8.5F);
        _updateStatus.AutoSize = false;
        _updateStatus.AutoEllipsis = true;
        _updateStatus.Location = new Point(40, 460);
        _updateStatus.Size = new Size(920, 20);

        _updateNow.Text = "Update";
        _updateNow.Location = new Point(40, 492);
        _updateNow.Size = new Size(160, 46);
        _updateNow.BackColor = Gold;
        _updateNow.ForeColor = Color.FromArgb(16, 18, 22);
        _updateNow.FlatStyle = FlatStyle.Flat;
        _updateNow.FlatAppearance.BorderSize = 0;
        _updateNow.FlatAppearance.MouseOverBackColor = Color.FromArgb(250, 214, 108);
        _updateNow.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _updateNow.Cursor = Cursors.Hand;
        _updateNow.Click += async (_, _) => await InstallUpdateAsync();

        _cancel.Text = "Cancel";
        _cancel.Location = new Point(214, 492);
        _cancel.Size = new Size(120, 46);
        _cancel.BackColor = PanelBack;
        _cancel.ForeColor = Ink;
        _cancel.FlatStyle = FlatStyle.Flat;
        _cancel.FlatAppearance.BorderSize = 0;
        _cancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 39, 58);
        _cancel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _cancel.Cursor = Cursors.Hand;
        _cancel.Click += (_, _) => ShowMain();

        _update.Controls.AddRange(new Control[]
        {
            _updateKicker, _updateTitle, _notes, _updateSummary, _updateProgress, _updateStatus,
            _updateNow, _cancel,
        });
    }

    private void LoadHero()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var name = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("Background.png", StringComparison.OrdinalIgnoreCase));
            if (name is null)
                return;
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream is not null)
                _hero.Image = Image.FromStream(stream);
        }
        catch
        {
            // The window still works without the hero image.
        }
    }

    private async Task OnShownAsync()
    {
        _settings = SettingsStore.Load();
        _activeMod = _settings.ActiveMod;
        if (!_settings.ShortcutCreated)
        {
            _settings.ShortcutCreated = TryCreateShortcut();
            SettingsStore.Save(_settings);
        }
        _gameDir = GameLocator.Find(_settings.GamePath);
        RefreshMods();
        RefreshState();
        if (_installOnStart)
            await InstallLatestAsync();
        else if (_settings.CheckOnStartup)
            await CheckAsync(silent: true);
    }

    private void RefreshState()
    {
        _gameDir = GameLocator.Find(_settings.GamePath);
        var installed = GameLocator.InstalledVersion(_gameDir);
        var zeroHour = ZeroHourLocator.Detect(_settings.ZeroHourPath, _gameDir);

        _ready.Text = _gameDir is null ? "Not installed" : "Ready to play";
        _gameVersion.Text = installed ?? (_gameDir is null ? "\u2014" : "local");
        _launcherVersion.Text = LauncherVersion;

        if (_gameDir is null)
            _status.Text = "The game build has not been installed yet.";
        else if (installed is null)
            _status.Text = "A local game build is selected.";
        else
            _status.Text = $"Game {installed} installed.";

        _zeroHour.Text = zeroHour.Found
            ? "Zero Hour: " + zeroHour.Path
            : zeroHour.BaseGeneralsOnly
                ? "Zero Hour: only the base game was found"
                : "Zero Hour: not found - install Zero Hour";
        _zeroHour.ForeColor = zeroHour.Found ? Muted : Red;

        if (zeroHour.BaseReady)
        {
            _baseInfo.Text = "Base game files: linked";
            _baseInfo.ForeColor = Muted;
        }
        else if (zeroHour.BaseSource is not null)
        {
            _baseInfo.Text = "Base game files: found in your Zero Hour install";
            _baseInfo.ForeColor = Muted;
        }
        else
        {
            _baseInfo.Text = "Base game files: not found";
            _baseInfo.ForeColor = Red;
        }

        _installSummary.Text = zeroHour.Repaired ? "Zero Hour registry path repaired." : "";
        RefreshModSummary();
        _play.Enabled = !_busy;
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        _play.Enabled = !busy;
        _check.Enabled = !busy;
        _modsButton.Enabled = !busy;
        _setFolder.Enabled = !busy;
        _openFolder.Enabled = !busy;
        _locateZeroHour.Enabled = !busy;
        _updateNow.Enabled = !busy;
        _cancel.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private async Task CheckAsync(bool silent)
    {
        if (_busy)
            return;
        SetBusy(true);
        RefreshState();
        if (!silent)
            _status.Text = "Checking for updates\u2026";
        try
        {
            _manifest = await UpdateService.FetchManifestAsync(_settings.ManifestUrl);
            var installed = GameLocator.InstalledVersion(_gameDir);
            var gameNewer = _gameDir is null || installed is null
                || UpdateService.IsNewer(_manifest.Game!.Version, installed);
            var launcherNewer = _manifest.Launcher is not null
                && UpdateService.IsNewer(_manifest.Launcher.Version, LauncherVersion);

            if (gameNewer || launcherNewer)
            {
                _banner.Text = gameNewer
                    ? $"Update available: game {_manifest.Game!.Version} \u2014 click to review"
                    : $"Launcher update {_manifest.Launcher!.Version} available \u2014 click to review";
                _banner.Visible = true;
                _status.Text = gameNewer
                    ? $"Game update {_manifest.Game!.Version} available."
                    : $"Launcher update {_manifest.Launcher!.Version} available.";
                if (!silent)
                    ShowUpdateScreen(_manifest);
            }
            else
            {
                _banner.Visible = false;
                _status.Text = "Everything is up to date.";
                if (!silent)
                    MessageBox.Show(this, "The game and launcher are up to date.", "Up to date",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            _status.Text = "Could not check for updates.";
            if (!silent)
                MessageBox.Show(this, ex.Message, "Update check failed", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ShowUpdateScreen(LauncherManifest manifest)
    {
        _manifest = manifest;
        var installed = GameLocator.InstalledVersion(_gameDir);
        _updateTitle.Text = _gameDir is null || installed is null
            ? $"Install v{manifest.Game!.Version}"
            : $"v{installed} \u2192 v{manifest.Game!.Version}";
        _notes.Text = string.IsNullOrWhiteSpace(manifest.Notes)
            ? "No release notes were attached to this package."
            : manifest.Notes;
        _notes.SelectionStart = 0;
        _notes.SelectionLength = 0;
        var size = manifest.Game!.Size > 0
            ? $"{manifest.Game.Size / (1024.0 * 1024.0):0} MB"
            : "size not reported";
        _updateSummary.Text =
            $"Game package: {size}   \u2022   Launcher {LauncherVersion}"
            + (manifest.Launcher is not null ? $" \u2192 {manifest.Launcher.Version}" : "");
        _updateStatus.Text = "The game package is downloaded and hash-checked before the old build is replaced.";
        _updateNow.Text = _gameDir is null || installed is null ? "Install" : "Update";
        _updateNow.Enabled = true;
        _cancel.Enabled = true;
        _updateProgress.Visible = false;
        _main.Visible = false;
        _mods.Visible = false;
        _update.Visible = true;
        _update.BringToFront();
        _banner.Visible = false;
    }

    private void ShowMain()
    {
        _update.Visible = false;
        _mods.Visible = false;
        _main.Visible = true;
        _main.BringToFront();
    }

    private async Task InstallUpdateAsync()
    {
        if (_busy || _manifest?.Game is null)
            return;
        if (Process.GetProcessesByName("generals").Length > 0)
        {
            MessageBox.Show(this, "Close the game before updating.", "The game is running",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        SetBusy(true);
        _updateNow.Enabled = false;
        _cancel.Enabled = false;
        _updateProgress.Visible = true;
        _updateProgress.Value = 0;
        _updateStatus.Text = "Downloading the game package\u2026";
        try
        {
            var progress = new Progress<double>(value =>
            {
                _updateProgress.Value = Math.Clamp((int)Math.Round(value * 100), 0, 100);
                _updateStatus.Text = $"Downloading the game package: {value:P0}";
            });
            await UpdateService.InstallGameAsync(
                _manifest.Game, GameLocator.DefaultGameDirectory, progress);
            _settings.GamePath = GameLocator.DefaultGameDirectory;
            SettingsStore.Save(_settings);
            _gameDir = GameLocator.DefaultGameDirectory;

            _updateStatus.Text = "Connecting the base game files Zero Hour shares\u2026";
            var prep = await Task.Run(() =>
            {
                var result = ZeroHourLocator.PrepareBaseAssets(
                    _gameDir, _settings.ZeroHourPath, out var message);
                return (Result: result, Message: message);
            });
            _baseInfo.Text = prep.Message;
            await Task.Run(() => ZeroHourLocator.PrepareCursorAssets(
                _gameDir, _settings.ZeroHourPath));

            if (_manifest.Launcher is not null
                && UpdateService.IsNewer(_manifest.Launcher.Version, LauncherVersion))
            {
                _updateStatus.Text = $"Downloading launcher {_manifest.Launcher.Version}\u2026";
                _updateProgress.Value = 0;
                var launcherProgress = new Progress<double>(value =>
                {
                    _updateProgress.Value = Math.Clamp((int)Math.Round(value * 100), 0, 100);
                    _updateStatus.Text = $"Downloading launcher: {value:P0}";
                });
                var newExe = await UpdateService.DownloadLauncherAsync(
                    _manifest.Launcher, launcherProgress);
                SelfUpdater.Launch(newExe, Environment.ProcessPath
                    ?? throw new InvalidOperationException("The launcher path is unknown."));
                Application.Exit();
                return;
            }

            _updateStatus.Text = $"Game {_manifest.Game.Version} installed.";
            ShowMain();
            RefreshMods();
            RefreshState();
            _status.Text = $"Game {_manifest.Game.Version} installed.";
        }
        catch (Exception ex)
        {
            _updateStatus.Text = "Update failed.";
            MessageBox.Show(this, ex.Message, "Update failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (!IsDisposed)
            {
                SetBusy(false);
                _updateNow.Enabled = true;
                _cancel.Enabled = true;
            }
        }
    }

    private async Task PlayAsync()
    {
        if (_busy)
            return;
        if (_gameDir is null)
        {
            await CheckAsync(silent: false);
            return;
        }

        var zeroHour = ZeroHourLocator.Detect(_settings.ZeroHourPath, _gameDir);
        if (!zeroHour.Found)
        {
            var answer = MessageBox.Show(this,
                zeroHour.BaseGeneralsOnly
                    ? "Only the base game was found. Vanguard is a Zero Hour mod and needs the Zero Hour expansion.\n\nPlay anyway?"
                    : "Zero Hour was not found. Install Zero Hour from Steam or the EA app.\n\nPlay anyway?",
                "Zero Hour not found", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes)
                return;
        }

        SetBusy(true);
        try
        {
            var hasWeather = await Task.Run(() => ZeroHourLocator.HasInizhFile(zeroHour.Path, "Weather.ini"));
            if (!hasWeather)
            {
                var answer = MessageBox.Show(this,
                    "Your Zero Hour install is incomplete: its INIZH.big has no "
                    + "Data\\INI\\Default\\Weather.ini.\n\n"
                    + "Install or verify the Zero Hour expansion in Steam/EA (not just base Generals), "
                    + "then use Locate Zero Hour if the folder is not detected.\n\nPlay anyway?",
                    "Zero Hour data incomplete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (answer != DialogResult.Yes)
                    return;
            }
            var duplicate = await Task.Run(() => ZeroHourLocator.MoveDuplicateInizh(zeroHour.Path));
            var archives = await Task.Run(() => ZeroHourLocator.PrepareZeroHourArchives(_gameDir, zeroHour.Path));
            var pointers = await Task.Run(() => ZeroHourLocator.PrepareCursorAssets(_gameDir, zeroHour.Path));
            _baseInfo.Text = archives.Message + " " + pointers.Message;
            var gameWeather = await Task.Run(() => ZeroHourLocator.HasInizhFile(_gameDir, "Weather.ini"));
            if (!gameWeather)
            {
                var answer = MessageBox.Show(this,
                    "The game folder still has no INIZH.big with Data\\INI\\Default\\Weather.ini. "
                    + "The launcher could not link the Zero Hour archives (different drive, permission, "
                    + "or the install is incomplete).\n\nPlay anyway?",
                    "Zero Hour archives missing", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (answer != DialogResult.Yes)
                    return;
            }
            if (duplicate is not null)
                _baseInfo.Text = "Moved the duplicate Data\\INI\\INIZH.big out of Zero Hour.";
            var prep = await Task.Run(() =>
            {
                var result = ZeroHourLocator.PrepareBaseAssets(
                    _gameDir, _settings.ZeroHourPath, out var message);
                return (Result: result, Message: message);
            });
            _baseInfo.Text = prep.Message;
            if (prep.Result is BaseAssetResult.Missing or BaseAssetResult.Failed)
            {
                var answer = MessageBox.Show(this,
                    prep.Message + "\n\nThe game will start with missing artwork and audio.\n\nPlay anyway?",
                    "Base game files missing", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (answer != DialogResult.Yes)
                    return;
            }
            var mod = ActiveModDirectory();
            GameLocator.Start(_gameDir, mod);
            _status.Text = mod is null
                ? "Game started."
                : $"Game started with {ActiveModName()}.";
        }
        catch (Exception ex)
        {
            _status.Text = "Could not start the game.";
            MessageBox.Show(this, ex.Message, "Could not start the game", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static bool TryCreateShortcut()
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
                return false;
            dynamic shell = Activator.CreateInstance(shellType)!;
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "Generals Vanguard.lnk");
            dynamic shortcut = shell.CreateShortcut(path);
            shortcut.TargetPath = Environment.ProcessPath ?? Application.ExecutablePath;
            shortcut.WorkingDirectory = AppContext.BaseDirectory;
            shortcut.IconLocation = Environment.ProcessPath ?? Application.ExecutablePath;
            shortcut.Save();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SetGameFolder()
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

    private void LocateZeroHour()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the Zero Hour folder that holds INIZH.big",
            SelectedPath = _settings.ZeroHourPath ?? "",
            ShowNewFolderButton = false,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        if (!File.Exists(Path.Combine(dialog.SelectedPath, "INIZH.big")))
        {
            MessageBox.Show(this, "That folder has no INIZH.big.", "Not a Zero Hour folder",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _settings.ZeroHourPath = dialog.SelectedPath;
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

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch
        {
            // A browser that will not open is not worth a dialog over.
        }
    }

    private void AttachDrag(Control control)
    {
        control.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left)
                return;
            ReleaseCapture();
            SendMessage(Handle, WmNcLButtonDown, HtCaption, 0);
        };
    }

    private const int WmNcLButtonDown = 0xA1;
    private const int HtCaption = 0x2;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
}
