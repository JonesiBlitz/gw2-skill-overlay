using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;

namespace SkillFocus
{
    [Export(typeof(Module))]
    public class SkillFocusModule : Module
    {
        private static readonly Logger Logger = Logger.GetLogger<SkillFocusModule>();

        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;

        [ImportingConstructor]
        public SkillFocusModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

        private static readonly Slot[] AllSlots = ((Slot[])Enum.GetValues(typeof(Slot)))
            .Where(s => s != Slot.Unknown).ToArray();

        // --- settings ---
        private SettingEntry<int> _boxSize;
        private SettingEntry<string> _selectedSongPath;
        private SettingEntry<string> _layoutsJson;
        private SettingEntry<string> _lastActiveLayoutKey;

        // --- keybinds ---
        private Dictionary<Slot, HashSet<Keys>> _keybinds = new Dictionary<Slot, HashSet<Keys>>();
        private HashSet<Slot> _unsupportedSlots = new HashSet<Slot>();

        // --- rotation songs ---
        private List<string> _songFiles = new List<string>();
        private RotationSong _song;
        private int _currentIndex;

        // --- layouts (per profession+spec calibration) ---
        private Dictionary<string, Layout> _layouts = new Dictionary<string, Layout>();
        private string _activeLayoutKey;
        private string _lastDetectedKey;

        // --- overlay ---
        private HighlightControl _highlight;
        private Texture2D _iconTexture;

        private bool _calibrating;
        private string _calibratingKey;
        private readonly List<CalibrationMarker> _markers = new List<CalibrationMarker>();

        private CornerIcon _cornerIcon;
        private ContextMenuStrip _menu;
        private ContextMenuStripItem _calibrateMenuItem;
        private ContextMenuStripItem _layoutsMenuItem;
        private SongPickerPanel _songPicker;

        protected override void DefineSettings(SettingCollection settings)
        {
            _boxSize = settings.DefineSetting(
                "BoxSize", 46,
                () => "Highlight Box Size",
                () => "Size in pixels of the highlight box drawn over each skill icon.");

            _selectedSongPath = settings.DefineSetting(
                "SelectedSong", "",
                () => "Selected Rotation",
                () => "Path to the rotation file currently loaded. Change via the corner icon menu.");

            var internalSettings = settings.AddSubCollection("internal (not shown in UI)");
            _layoutsJson = internalSettings.DefineSetting("LayoutsJson", "");
            _lastActiveLayoutKey = internalSettings.DefineSetting("LastActiveLayoutKey", "");
        }

        protected override async Task LoadAsync()
        {
            LoadLayoutsFromSettings();
            RebuildKeybinds();
            LoadSongsAndPickDefault();

            _iconTexture = MakeCrosshairTexture(32);

            _highlight = new HighlightControl
            {
                Parent = GameService.Graphics.SpriteScreen,
                Size = new Point(_boxSize.Value, _boxSize.Value),
                Visible = false,
            };

            GameService.Input.Keyboard.KeyPressed += Keyboard_KeyPressed;

            BuildCornerIcon();

            // Restore whichever layout was active last session as a starting point;
            // the Update loop will immediately correct this once Mumble data is available.
            if (!string.IsNullOrEmpty(_lastActiveLayoutKey.Value) && _layouts.ContainsKey(_lastActiveLayoutKey.Value))
            {
                _activeLayoutKey = _lastActiveLayoutKey.Value;
            }

            UpdateHighlightPosition();

            await Task.CompletedTask;
        }

        protected override void Update(GameTime gameTime)
        {
            if (_highlight != null && _highlight.Size.X != _boxSize.Value)
            {
                _highlight.Size = new Point(_boxSize.Value, _boxSize.Value);
            }

            // Cheap poll: switch layouts automatically when the player's profession/spec changes
            // (e.g. swapping build templates, logging onto a different character).
            if (!_calibrating && GameService.Gw2Mumble.IsAvailable)
            {
                string detectedKey = BuildLayoutKey(
                    GameService.Gw2Mumble.PlayerCharacter.Profession.ToString(),
                    GameService.Gw2Mumble.PlayerCharacter.Specialization);

                if (detectedKey != _lastDetectedKey)
                {
                    _lastDetectedKey = detectedKey;
                    TryAutoSelectLayout(detectedKey, notifyIfMissing: true);
                }
            }
        }

        protected override void Unload()
        {
            GameService.Input.Keyboard.KeyPressed -= Keyboard_KeyPressed;

            foreach (var marker in _markers) marker.Dispose();
            _markers.Clear();

            _highlight?.Dispose();
            _cornerIcon?.Dispose();
            _menu?.Dispose();
            _songPicker?.Dispose();
            _iconTexture?.Dispose();
        }

        // ------------------------------------------------------------------
        // Keybind detection
        // ------------------------------------------------------------------

        private void RebuildKeybinds()
        {
            string inputBindsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Guild Wars 2", "InputBinds");

            var (map, unsupported) = KeybindMap.Build(inputBindsDir);
            _keybinds = map;
            _unsupportedSlots = new HashSet<Slot>(unsupported);

            if (unsupported.Count > 0)
            {
                string list = string.Join(", ", unsupported.Select(s => s.ShortLabel()));
                ScreenNotification.ShowNotification(
                    $"Skill Focus: {list} bound to a mouse/other device - can't auto-detect that key. Skipping it in the rotation instead of blocking on it.",
                    ScreenNotification.NotificationType.Warning);
            }

            SkipUnsupportedSteps();
        }

        // ------------------------------------------------------------------
        // Rotation songs (reuses Dance Dance Rotation's song file format/folders)
        // ------------------------------------------------------------------

        private static string DdrCustomSongsDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Guild Wars 2", "addons", "blishhud", "danceDanceRotation", "customSongs");

        private static string DdrDefaultSongsDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Guild Wars 2", "addons", "blishhud", "danceDanceRotation", "defaultSongs");

        /// <summary>
        /// Skill Focus's own rotation folder, used regardless of whether Dance Dance
        /// Rotation is installed - so players without DDR still have somewhere to drop
        /// (or paste, via "Add Rotation from Clipboard") song files.
        /// </summary>
        private static string OwnSongsDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Guild Wars 2", "addons", "blishhud", "skillfocus-data", "customSongs");

        private void LoadSongsAndPickDefault()
        {
            Directory.CreateDirectory(OwnSongsDir);

            _songFiles = RotationSong.DiscoverSongFiles(OwnSongsDir, DdrCustomSongsDir, DdrDefaultSongsDir);

            string pick = _selectedSongPath.Value;
            if (string.IsNullOrEmpty(pick) || !File.Exists(pick))
            {
                pick = _songFiles.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).IndexOf("Ritualist", StringComparison.OrdinalIgnoreCase) >= 0
                                                        && Path.GetFileNameWithoutExtension(f).IndexOf("Quickness", StringComparison.OrdinalIgnoreCase) < 0)
                       ?? _songFiles.FirstOrDefault();
            }

            if (pick != null)
            {
                LoadSong(pick);
            }
            else
            {
                ScreenNotification.ShowNotification(
                    "Skill Focus: no rotation files found. Use the corner icon to open your rotations folder or paste one from the clipboard.",
                    ScreenNotification.NotificationType.Warning);
            }
        }

        /// <summary>
        /// Parses the clipboard as a Dance Dance Rotation-format song and saves it into
        /// our own songs folder, mirroring DDR's own "Add from Clipboard" feature - lets
        /// players without DDR installed still add rotations without hand-editing files.
        /// </summary>
        private void AddSongFromClipboard()
        {
            string json;
            try
            {
                json = System.Windows.Forms.Clipboard.GetText();
            }
            catch (Exception e)
            {
                Logger.Info(e, "Failed to read clipboard.");
                ScreenNotification.ShowNotification("Skill Focus: couldn't read the clipboard.", ScreenNotification.NotificationType.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                ScreenNotification.ShowNotification("Skill Focus: clipboard is empty.", ScreenNotification.NotificationType.Warning);
                return;
            }

            RotationSong parsed;
            try
            {
                parsed = RotationSong.LoadFromJson(json, sourcePath: null, fallbackName: "Pasted Rotation");
            }
            catch (Exception e)
            {
                Logger.Info(e, "Failed to parse clipboard as a rotation song.");
                ScreenNotification.ShowNotification("Skill Focus: clipboard doesn't look like a valid rotation song.", ScreenNotification.NotificationType.Error);
                return;
            }

            if (parsed.Steps.Count == 0)
            {
                ScreenNotification.ShowNotification("Skill Focus: that rotation has no recognizable skill steps.", ScreenNotification.NotificationType.Warning);
                return;
            }

            string safeName = string.Join("_", parsed.Name.Split(Path.GetInvalidFileNameChars()));
            string destPath = Path.Combine(OwnSongsDir, $"{safeName}.json");

            Directory.CreateDirectory(OwnSongsDir);
            File.WriteAllText(destPath, json);

            _songFiles = RotationSong.DiscoverSongFiles(OwnSongsDir, DdrCustomSongsDir, DdrDefaultSongsDir);
            _songPicker?.UpdateFiles(_songFiles);

            ScreenNotification.ShowNotification($"Skill Focus: added '{parsed.Name}'.", ScreenNotification.NotificationType.Info);
            LoadSong(destPath);
        }

        private void LoadSong(string path)
        {
            try
            {
                _song = RotationSong.LoadFromFile(path);
                _selectedSongPath.Value = path;
                _currentIndex = 0;
                SkipUnsupportedSteps();
                UpdateHighlightPosition();
                _songPicker?.UpdateSelectedPath(path);
            }
            catch (Exception e)
            {
                Logger.Info(e, "Failed to load rotation file {path}", path);
                ScreenNotification.ShowNotification(
                    $"Skill Focus: failed to load '{Path.GetFileName(path)}'.",
                    ScreenNotification.NotificationType.Error);
            }
        }

        // ------------------------------------------------------------------
        // Rotation tracking
        // ------------------------------------------------------------------

        private void Keyboard_KeyPressed(object sender, KeyboardEventArgs e)
        {
            if (_calibrating) return;
            if (_song == null || _song.Steps.Count == 0) return;

            var expected = _song.Steps[_currentIndex];
            _keybinds.TryGetValue(expected.Slot, out HashSet<Keys> expectedKeys);
            bool matched = expectedKeys != null && expectedKeys.Contains(e.Key);

            if (matched)
            {
                _currentIndex = (_currentIndex + 1) % _song.Steps.Count;
                SkipUnsupportedSteps();
                UpdateHighlightPosition();
            }
        }

        /// <summary>
        /// Steps whose slot can't be detected (bound to a mouse button etc.) can never receive
        /// a matching keyboard event, which would otherwise stall the trainer on that step forever.
        /// Skip them automatically instead.
        /// </summary>
        private void SkipUnsupportedSteps()
        {
            if (_song == null || _song.Steps.Count == 0) return;

            int guard = 0;
            while (_unsupportedSlots.Contains(_song.Steps[_currentIndex].Slot) && guard < _song.Steps.Count)
            {
                _currentIndex = (_currentIndex + 1) % _song.Steps.Count;
                guard++;
            }
        }

        private void UpdateHighlightPosition()
        {
            if (_highlight == null) return;

            if (_calibrating || _song == null || _song.Steps.Count == 0 || _activeLayoutKey == null
                || !_layouts.TryGetValue(_activeLayoutKey, out var layout))
            {
                _highlight.Visible = false;
                return;
            }

            var slot = _song.Steps[_currentIndex].Slot;

            if (!layout.Positions.TryGetValue(slot, out var pos))
            {
                _highlight.Visible = false;
                return;
            }

            _highlight.Location = new Point(pos.X, pos.Y);
            _highlight.Size = new Point(_boxSize.Value, _boxSize.Value);
            _highlight.Visible = true;
        }

        // ------------------------------------------------------------------
        // Layouts (per profession+spec calibration profiles)
        // ------------------------------------------------------------------

        private static string BuildLayoutKey(string profession, int specialization) => $"{profession}_{specialization}";

        private void LoadLayoutsFromSettings()
        {
            if (string.IsNullOrEmpty(_layoutsJson.Value)) return;

            try
            {
                _layouts = JsonConvert.DeserializeObject<Dictionary<string, Layout>>(_layoutsJson.Value)
                           ?? new Dictionary<string, Layout>();
            }
            catch (Exception e)
            {
                Logger.Info(e, "Failed to parse saved layouts, starting fresh.");
                _layouts = new Dictionary<string, Layout>();
            }
        }

        private void SaveLayoutsToSettings()
        {
            _layoutsJson.Value = JsonConvert.SerializeObject(_layouts);
        }

        private void TryAutoSelectLayout(string detectedKey, bool notifyIfMissing)
        {
            if (_layouts.TryGetValue(detectedKey, out var layout))
            {
                _activeLayoutKey = layout.Key;
                _lastActiveLayoutKey.Value = layout.Key;
                RebuildLayoutsSubmenu();
                UpdateHighlightPosition();
            }
            else if (notifyIfMissing)
            {
                ScreenNotification.ShowNotification(
                    "Skill Focus: no skill bar calibration saved for your current build yet. Use the corner icon -> Calibrate Skill Bar.",
                    ScreenNotification.NotificationType.Info);
            }
        }

        // ------------------------------------------------------------------
        // Calibration
        // ------------------------------------------------------------------

        private void ToggleCalibration()
        {
            if (_calibrating) FinishCalibration();
            else StartCalibration();
        }

        private void StartCalibration()
        {
            _calibrating = true;
            if (_highlight != null) _highlight.Visible = false;
            if (_calibrateMenuItem != null) _calibrateMenuItem.Text = "Finish Calibration (Save Positions)";

            // Calibrate for whichever build is currently detected; fall back to the active
            // layout's key (or a generic "manual" key) if Mumble data isn't available right now.
            if (GameService.Gw2Mumble.IsAvailable)
            {
                _calibratingKey = BuildLayoutKey(
                    GameService.Gw2Mumble.PlayerCharacter.Profession.ToString(),
                    GameService.Gw2Mumble.PlayerCharacter.Specialization);
            }
            else
            {
                _calibratingKey = _activeLayoutKey ?? "manual";
            }

            string displayName = GameService.Gw2Mumble.IsAvailable
                ? $"{GameService.Gw2Mumble.PlayerCharacter.Profession} (spec {GameService.Gw2Mumble.PlayerCharacter.Specialization})"
                : _calibratingKey;

            // Seed starting positions: prefer an existing save for this exact build, otherwise
            // borrow positions from any other saved layout as a rough starting guess, otherwise
            // stagger fresh markers near the bottom-center of the screen.
            _layouts.TryGetValue(_calibratingKey, out var existingLayout);
            Layout referenceLayout = existingLayout ?? _layouts.Values.FirstOrDefault();

            var screen = GameService.Graphics.SpriteScreen;
            int baseX = Math.Max(20, screen.Width / 2 - (AllSlots.Length * 40) / 2);
            int baseY = screen.Height - 160;

            int i = 0;
            foreach (var slot in AllSlots)
            {
                Point startPos;
                if (referenceLayout != null && referenceLayout.Positions.TryGetValue(slot, out var saved))
                {
                    startPos = new Point(saved.X, saved.Y);
                }
                else
                {
                    startPos = new Point(baseX + i * 40, baseY);
                }

                var marker = new CalibrationMarker(slot)
                {
                    Parent = screen,
                    Size = new Point(_boxSize.Value, _boxSize.Value),
                    Location = startPos,
                };
                _markers.Add(marker);
                i++;
            }

            ScreenNotification.ShowNotification(
                $"Skill Focus: calibrating for {displayName}. Drag each labeled box onto the matching skill icon, then finish from the corner icon menu.",
                ScreenNotification.NotificationType.Info, duration: 8);
        }

        private void FinishCalibration()
        {
            var layout = new Layout
            {
                Key = _calibratingKey,
                DisplayName = GameService.Gw2Mumble.IsAvailable
                    ? $"{GameService.Gw2Mumble.PlayerCharacter.Profession} (spec {GameService.Gw2Mumble.PlayerCharacter.Specialization})"
                    : _calibratingKey,
            };

            foreach (var marker in _markers)
            {
                layout.Positions[marker.Slot] = new PointData { X = marker.Location.X, Y = marker.Location.Y };
                marker.Dispose();
            }
            _markers.Clear();

            _layouts[layout.Key] = layout;
            SaveLayoutsToSettings();

            _activeLayoutKey = layout.Key;
            _lastActiveLayoutKey.Value = layout.Key;

            _calibrating = false;
            _calibratingKey = null;
            if (_calibrateMenuItem != null) _calibrateMenuItem.Text = "Calibrate Skill Bar";

            RebuildLayoutsSubmenu();
            UpdateHighlightPosition();
        }

        // ------------------------------------------------------------------
        // UI: corner icon + menu
        // ------------------------------------------------------------------

        private void BuildCornerIcon()
        {
            _cornerIcon = new CornerIcon
            {
                Icon = _iconTexture,
                BasicTooltipText = "Skill Focus",
                Priority = 875219341,
                Parent = GameService.Graphics.SpriteScreen,
            };

            _menu = new ContextMenuStrip();

            _calibrateMenuItem = _menu.AddMenuItem(_calibrating ? "Finish Calibration (Save Positions)" : "Calibrate Skill Bar");
            _calibrateMenuItem.Click += (s, e) => ToggleCalibration();

            var restartItem = _menu.AddMenuItem("Restart Rotation From Beginning");
            restartItem.Click += (s, e) => { _currentIndex = 0; SkipUnsupportedSteps(); UpdateHighlightPosition(); };

            _songPicker = new SongPickerPanel(GameService.Graphics.SpriteScreen, _songFiles, _selectedSongPath.Value, LoadSong);

            var rotationItem = _menu.AddMenuItem("Choose Rotation...");
            rotationItem.Click += (s, e) => _songPicker.Toggle();

            var openFolderItem = _menu.AddMenuItem("Open Rotations Folder");
            openFolderItem.Click += (s, e) =>
            {
                Directory.CreateDirectory(OwnSongsDir);
                Process.Start(new ProcessStartInfo { FileName = OwnSongsDir, UseShellExecute = true });
            };

            var pasteItem = _menu.AddMenuItem("Add Rotation from Clipboard");
            pasteItem.Click += (s, e) => AddSongFromClipboard();

            _layoutsMenuItem = _menu.AddMenuItem("Layouts");
            RebuildLayoutsSubmenu();

            var rescanItem = _menu.AddMenuItem("Rescan Keybinds");
            rescanItem.Click += (s, e) => RebuildKeybinds();

            _cornerIcon.Menu = _menu;
            _cornerIcon.Click += (s, e) => { _menu.Show(_cornerIcon); };
        }

        private void RebuildLayoutsSubmenu()
        {
            if (_layoutsMenuItem == null) return;

            var oldSubmenu = _layoutsMenuItem.Submenu;

            var submenu = new ContextMenuStrip();

            if (_layouts.Count == 0)
            {
                var noneItem = submenu.AddMenuItem("(none calibrated yet)");
                noneItem.Enabled = false;
            }

            foreach (var layout in _layouts.Values.OrderBy(l => l.DisplayName))
            {
                var item = submenu.AddMenuItem(layout.DisplayName);
                item.CanCheck = true;
                item.Checked = layout.Key == _activeLayoutKey;
                item.Click += (s, e) =>
                {
                    _activeLayoutKey = layout.Key;
                    _lastActiveLayoutKey.Value = layout.Key;
                    RebuildLayoutsSubmenu();
                    UpdateHighlightPosition();
                };
            }

            _layoutsMenuItem.Submenu = submenu;
            oldSubmenu?.Dispose();
        }

        /// <summary>
        /// Draws a simple gold crosshair/reticle icon (a ring, center dot, and four tick
        /// marks) on a transparent background - matches the highlight box's gold pulse,
        /// and avoids depending on a guessed GW2 asset id that might not exist.
        /// </summary>
        private static Texture2D MakeCrosshairTexture(int size)
        {
            using var ctx = GameService.Graphics.LendGraphicsDeviceContext();

            var texture = new Texture2D(ctx.GraphicsDevice, size, size);
            var data = new Color[size * size];

            var gold = new Color(255, 215, 0);
            float center = (size - 1) / 2f;
            float ringOuter = size * 0.40f;
            float ringInner = size * 0.34f;
            float dotRadius = size * 0.07f;
            int tickThickness = Math.Max(2, size / 16);
            int tickStart = 1;
            int tickEnd = (int)(size * 0.20f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                    bool ring = dist >= ringInner && dist <= ringOuter;
                    bool dot = dist <= dotRadius;

                    bool halfway = Math.Abs(x - center) <= tickThickness / 2f;
                    bool halfwayV = Math.Abs(y - center) <= tickThickness / 2f;

                    bool tickTop = halfway && y >= tickStart && y <= tickEnd;
                    bool tickBottom = halfway && y >= size - 1 - tickEnd && y <= size - 1 - tickStart;
                    bool tickLeft = halfwayV && x >= tickStart && x <= tickEnd;
                    bool tickRight = halfwayV && x >= size - 1 - tickEnd && x <= size - 1 - tickStart;

                    bool lit = ring || dot || tickTop || tickBottom || tickLeft || tickRight;

                    data[y * size + x] = lit ? gold : Color.Transparent;
                }
            }

            texture.SetData(data);
            return texture;
        }
    }
}
