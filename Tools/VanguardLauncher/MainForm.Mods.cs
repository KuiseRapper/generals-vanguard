using System.Diagnostics;
using VanguardLauncher.Core;

namespace VanguardLauncher;

public sealed partial class MainForm
{
    private static readonly Color ModActive = Color.FromArgb(242, 201, 76);
    private static readonly Color ModInstalled = Color.FromArgb(115, 209, 140);
    private static readonly Color ModPreview = Color.FromArgb(128, 170, 235);
    private static readonly Color ModIdle = Color.FromArgb(143, 162, 191);

    private readonly Panel _mods = new();
    private readonly Label _modsKicker = new();
    private readonly Label _modsTitle = new();
    private readonly Label _modsIntro = new();
    private readonly Label _modsPath = new();
    private readonly ProgressBar _modsProgress = new();
    private readonly Label _modsStatus = new();
    private readonly Button _modsOpenFolder = new();
    private readonly Button _modsInstallFile = new();
    private readonly Button _modsRefresh = new();
    private readonly Button _modsBack = new();
    private readonly List<ModRow> _modRows = new();
    private readonly ModStore _modStore = new();
    private readonly Dictionary<string, InstalledMod> _installedMods = new();
    private string? _activeMod;
    private bool _modsBusy;
    private bool _refreshingMods;

    private sealed class ModRow
    {
        public FeaturedMod Mod { get; init; } = null!;
        public Panel Panel { get; init; } = null!;
        public Label Status { get; init; } = null!;
        public Label Size { get; init; } = null!;
        public CheckBox Active { get; init; } = null!;
        public Button Action { get; init; } = null!;
    }

    private void BuildModsScreen()
    {
        _modsKicker.Text = "MODS";
        _modsKicker.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _modsKicker.ForeColor = Muted;
        _modsKicker.AutoSize = true;
        _modsKicker.Location = new Point(40, 24);

        _modsTitle.Text = "Choose what Play loads";
        _modsTitle.Font = new Font("Segoe UI", 21F, FontStyle.Bold);
        _modsTitle.ForeColor = Ink;
        _modsTitle.AutoSize = true;
        _modsTitle.Location = new Point(40, 42);

        _modsIntro.Text = "Three mods are offered. Nothing is downloaded until you press Download; "
                          + "a downloaded mod stays in your Documents folder, so a game update cannot remove it. "
                          + "Tick one to play it, or tick nothing to play vanilla Vanguard.";
        _modsIntro.Font = new Font("Segoe UI", 9F);
        _modsIntro.ForeColor = Muted;
        _modsIntro.AutoSize = false;
        _modsIntro.Location = new Point(40, 84);
        _modsIntro.Size = new Size(920, 36);

        _modsPath.Font = new Font("Segoe UI", 8F);
        _modsPath.ForeColor = ModIdle;
        _modsPath.AutoSize = false;
        _modsPath.AutoEllipsis = true;
        _modsPath.Location = new Point(40, 122);
        _modsPath.Size = new Size(920, 18);

        var top = 146;
        foreach (var mod in FeaturedMods.All)
        {
            var row = CreateModRow(mod, top);
            _modRows.Add(row);
            _mods.Controls.Add(row.Panel);
            top += 112;
        }

        _modsProgress.Location = new Point(40, 486);
        _modsProgress.Size = new Size(920, 8);
        _modsProgress.Visible = false;

        _modsStatus.Text = "";
        _modsStatus.ForeColor = Muted;
        _modsStatus.Font = new Font("Segoe UI", 8.5F);
        _modsStatus.AutoSize = false;
        _modsStatus.AutoEllipsis = true;
        _modsStatus.Location = new Point(40, 500);
        _modsStatus.Size = new Size(920, 18);

        SetupFlatButton(_modsOpenFolder, "Open mods folder", new Point(40, 520), new Size(150, 34),
            PanelBack, Ink);
        _modsOpenFolder.Click += (_, _) => OpenModsFolder();

        SetupFlatButton(_modsInstallFile, "Install from file\u2026", new Point(200, 520),
            new Size(180, 34), PanelBack, Ink);
        _modsInstallFile.Click += async (_, _) => await InstallModFromFileAsync();

        SetupFlatButton(_modsRefresh, "Refresh", new Point(390, 520), new Size(120, 34),
            PanelBack, Ink);
        _modsRefresh.Click += (_, _) => RefreshMods();

        SetupFlatButton(_modsBack, "Close", new Point(840, 520), new Size(120, 34), Gold,
            Color.FromArgb(16, 18, 22));
        _modsBack.Click += (_, _) => ShowMain();

        _mods.Controls.AddRange(new Control[]
        {
            _modsKicker, _modsTitle, _modsIntro, _modsPath, _modsProgress, _modsStatus,
            _modsOpenFolder, _modsInstallFile, _modsRefresh, _modsBack,
        });
    }

    private ModRow CreateModRow(FeaturedMod mod, int top)
    {
        var panel = new Panel
        {
            Location = new Point(40, top),
            Size = new Size(920, 104),
            BackColor = SideBack,
        };

        var active = new CheckBox
        {
            Location = new Point(16, 40),
            Size = new Size(18, 18),
            BackColor = SideBack,
            ForeColor = Ink,
            FlatStyle = FlatStyle.Flat,
            TabStop = true,
        };

        var title = new Label
        {
            Text = mod.Title,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Ink,
            AutoSize = true,
            Location = new Point(44, 12),
        };

        var status = new Label
        {
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            ForeColor = ModIdle,
            AutoSize = true,
            Location = new Point(44, 38),
        };

        var size = new Label
        {
            Text = mod.SizeText,
            Font = new Font("Segoe UI", 8F),
            ForeColor = Muted,
            AutoSize = true,
            Location = new Point(660, 15),
        };

        var detail = new Label
        {
            Text = mod.Detail,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Muted,
            AutoSize = false,
            Location = new Point(44, 58),
            Size = new Size(590, 36),
        };

        var action = new Button
        {
            Text = "Download",
            Location = new Point(772, 54),
            Size = new Size(130, 34),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            BackColor = Gold,
            ForeColor = Color.FromArgb(16, 18, 22),
        };
        action.FlatAppearance.BorderSize = 0;
        action.FlatAppearance.MouseOverBackColor = Color.FromArgb(250, 214, 108);

        var tooltip = new ToolTip();
        tooltip.SetToolTip(panel, string.Join("\n", mod.Includes));
        tooltip.SetToolTip(title, string.Join("\n", mod.Includes));

        panel.Controls.AddRange(new Control[] { active, title, status, size, detail, action });

        var row = new ModRow
        {
            Mod = mod,
            Panel = panel,
            Status = status,
            Size = size,
            Active = active,
            Action = action,
        };

        active.CheckedChanged += (_, _) =>
        {
            if (_refreshingMods)
                return;
            if (active.Checked)
                SetActiveMod(mod.Id);
            else if (string.Equals(_activeMod, mod.Id, StringComparison.OrdinalIgnoreCase))
                SetActiveMod(null);
        };

        action.Click += async (_, _) =>
        {
            if (_installedMods.ContainsKey(mod.Id))
                RemoveMod(mod);
            else
                await DownloadModAsync(mod);
        };

        return row;
    }

    private static void SetupFlatButton(Button button, string text, Point location, Size size,
        Color back, Color fore)
    {
        button.Text = text;
        button.Location = location;
        button.Size = size;
        button.BackColor = back;
        button.ForeColor = fore;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 39, 58);
        button.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
    }

    private void ShowModsScreen()
    {
        if (_busy)
            return;
        RefreshMods();
        _main.Visible = false;
        _update.Visible = false;
        _mods.Visible = true;
        _mods.BringToFront();
    }

    private void RefreshMods()
    {
        _installedMods.Clear();
        try
        {
            foreach (var mod in _modStore.Scan())
                _installedMods[mod.Id] = mod;
        }
        catch (Exception ex)
        {
            _modsStatus.Text = "Could not read the mods folder: " + ex.Message;
        }

        if (_activeMod is not null && !_installedMods.ContainsKey(_activeMod))
        {
            _activeMod = null;
            _settings.ActiveMod = null;
            SettingsStore.Save(_settings);
        }

        _modsPath.Text = "Mods folder: " + _modStore.Root;
        RefreshModRows();
        RefreshModSummary();
        if (string.IsNullOrWhiteSpace(_modsStatus.Text))
            _modsStatus.Text = "Only a mod you download is installed; the three packages above are not included.";
    }

    private void RefreshModRows()
    {
        _refreshingMods = true;
        try
        {
            foreach (var row in _modRows)
            {
                var installed = _installedMods.TryGetValue(row.Mod.Id, out var entry);
                var active = installed && string.Equals(_activeMod, row.Mod.Id, StringComparison.OrdinalIgnoreCase);
                row.Active.Enabled = installed && !_modsBusy;
                row.Active.Checked = active;
                row.Status.Text = active
                    ? "ACTIVE"
                    : installed
                        ? "INSTALLED"
                        : row.Mod.Preview
                            ? "PREVIEW \u00b7 NOT INSTALLED"
                            : "NOT INSTALLED";
                row.Status.ForeColor = active
                    ? ModActive
                    : installed
                        ? ModInstalled
                        : row.Mod.Preview
                            ? ModPreview
                            : ModIdle;
                row.Size.Text = installed
                    ? $"{entry!.Name} {entry.Version}"
                    : row.Mod.SizeText;
                row.Size.ForeColor = installed ? ModInstalled : Muted;
                row.Action.Text = installed ? "Remove" : "Download";
                row.Action.Enabled = !_modsBusy;
                row.Action.BackColor = installed ? PanelBack : Gold;
                row.Action.ForeColor = installed ? Ink : Color.FromArgb(16, 18, 22);
            }
        }
        finally
        {
            _refreshingMods = false;
        }
    }

    private void RefreshModSummary()
    {
        if (_activeMod is not null && _installedMods.TryGetValue(_activeMod, out var mod))
        {
            _modInfo.Text = $"Mod: {mod.Name} {mod.Version}";
            _modInfo.ForeColor = ModInstalled;
            return;
        }
        _modInfo.Text = "Mod: vanilla Vanguard";
        _modInfo.ForeColor = Muted;
    }

    private string? ActiveModDirectory()
    {
        if (_activeMod is null)
            return null;
        if (_installedMods.TryGetValue(_activeMod, out var mod) && Directory.Exists(mod.Directory))
            return mod.Directory;
        // The folder was moved or deleted while the launcher was open: drop the stale selection.
        SetActiveMod(null);
        return null;
    }

    private string ActiveModName() =>
        _activeMod is not null && _installedMods.TryGetValue(_activeMod, out var mod)
            ? $"{mod.Name} {mod.Version}"
            : "vanilla Vanguard";

    private void SetActiveMod(string? id)
    {
        _activeMod = id;
        _settings.ActiveMod = id;
        SettingsStore.Save(_settings);
        RefreshModRows();
        RefreshModSummary();
    }

    private async Task DownloadModAsync(FeaturedMod mod)
    {
        if (_modsBusy)
            return;
        if (Process.GetProcessesByName("generals").Length > 0)
        {
            MessageBox.Show(this, "Close the game before installing a mod.", "The game is running",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _modsBusy = true;
        _modsProgress.Visible = true;
        _modsProgress.Value = 0;
        _modsStatus.ForeColor = Muted;
        _modsStatus.Text = $"Downloading {mod.Title}\u2026";
        RefreshModRows();

        var zip = Path.Combine(Path.GetTempPath(),
            $"vanguard-{mod.Id}-{Guid.NewGuid():N}.zip");
        try
        {
            var progress = new Progress<double>(value =>
            {
                _modsProgress.Value = Math.Clamp((int)Math.Round(value * 100), 0, 100);
                _modsStatus.Text = $"Downloading {mod.Title}: {value:P0}";
            });
            await UpdateService.DownloadFileAsync(mod.DownloadUrl, zip, progress);

            _modsStatus.Text = $"Unpacking {mod.Title}\u2026";
            await Task.Run(() => _modStore.Install(zip, mod.Id, mod.Title, mod.Version));

            _activeMod = mod.Id;
            _settings.ActiveMod = mod.Id;
            SettingsStore.Save(_settings);
            _modsStatus.ForeColor = ModInstalled;
            _modsStatus.Text = $"{mod.Title} {mod.Version} installed and selected. Press Play.";
        }
        catch (Exception ex)
        {
            _modsStatus.ForeColor = Red;
            _modsStatus.Text = "Could not install " + mod.Title + ".";
            MessageBox.Show(this, ex.Message, "Could not install the mod", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            UpdateService.TryDelete(zip);
            _modsBusy = false;
            _modsProgress.Visible = false;
            RefreshMods();
        }
    }

    private void RemoveMod(FeaturedMod mod)
    {
        if (_modsBusy)
            return;
        if (Process.GetProcessesByName("generals").Length > 0)
        {
            MessageBox.Show(this, "Close the game before removing a mod.", "The game is running",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (MessageBox.Show(this, $"Remove {mod.Title} from your mods folder?", "Remove mod",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            _modStore.Remove(mod.Id);
            if (string.Equals(_activeMod, mod.Id, StringComparison.OrdinalIgnoreCase))
                SetActiveMod(null);
            _modsStatus.ForeColor = Muted;
            _modsStatus.Text = $"{mod.Title} removed.";
            RefreshMods();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not remove the mod", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private async Task InstallModFromFileAsync()
    {
        if (_modsBusy)
            return;
        using var dialog = new OpenFileDialog
        {
            Title = "Choose a mod package",
            Filter = "Mod packages (*.zip;*.big)|*.zip;*.big|Zip archives (*.zip)|*.zip|BIG archives (*.big)|*.big",
            CheckFileExists = true,
            Multiselect = false,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var name = Path.GetFileNameWithoutExtension(dialog.FileName);
        var id = ModStore.SafeId(name);
        if (_installedMods.ContainsKey(id)
            && MessageBox.Show(this, $"Replace the installed {name}?", "Replace mod",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        _modsBusy = true;
        RefreshModRows();
        try
        {
            await Task.Run(() => _modStore.Install(dialog.FileName, id, name, "1.0"));
            _activeMod = id;
            _settings.ActiveMod = id;
            SettingsStore.Save(_settings);
            _modsStatus.ForeColor = ModInstalled;
            _modsStatus.Text = $"{name} installed and selected.";
        }
        catch (Exception ex)
        {
            _modsStatus.ForeColor = Red;
            _modsStatus.Text = "Could not install " + name + ".";
            MessageBox.Show(this, ex.Message, "Could not install the mod", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _modsBusy = false;
            RefreshMods();
        }
    }

    private void OpenModsFolder()
    {
        try
        {
            _modStore.Ensure();
            Process.Start(new ProcessStartInfo { FileName = _modStore.Root, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open the mods folder", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    /// <summary>The installer runs the launcher with --install, so installing the game is one step.</summary>
    private async Task InstallLatestAsync()
    {
        if (_busy)
            return;
        await CheckAsync(silent: true);
        if (_manifest?.Game is null)
        {
            _status.Text = "Could not reach the release. Use Check for updates when you are online.";
            MessageBox.Show(this,
                "The launcher could not reach GitHub to download the game.\n\n"
                + "Check your connection, then press Check for updates.",
                "Could not download the game", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var installed = GameLocator.InstalledVersion(_gameDir);
        if (_gameDir is not null && installed is not null
            && !UpdateService.IsNewer(_manifest.Game.Version, installed))
        {
            _status.Text = $"Game {installed} is already installed.";
            return;
        }

        ShowUpdateScreen(_manifest);
        await InstallUpdateAsync();
    }
}
