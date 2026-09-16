using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;

namespace SkillFocus
{
    /// <summary>
    /// A searchable, scrollable rotation picker. The DDR song folders can easily hold 100+
    /// files, which overflowed a plain context menu off the screen with no way to scroll -
    /// this is a small floating panel instead, sized to fit and with a text filter.
    /// </summary>
    public class SongPickerPanel
    {
        /// <summary>
        /// A plain Image captures mouse input by default, which would sit in front of (or
        /// compete for hit-testing with) the search box/list drawn on top of it. This
        /// variant never captures input, so it's safe to use purely as a background fill.
        /// </summary>
        private class BackdropImage : Image
        {
            protected override CaptureType CapturesInput() => CaptureType.None;
        }

        private const int TitleBarHeight = 36;
        private const int WindowWidth = 360;
        private const int WindowHeight = 460;

        private readonly Container _parent;
        private readonly Panel _window;
        private readonly TextBox _search;
        private readonly FlowPanel _list;
        private readonly List<string> _allFiles;
        private readonly Action<string> _onSelected;
        private string _selectedPath;

        private bool _hasBeenPositioned;
        private bool _dragging;
        private Point _dragOffset;
        private readonly EventHandler<Blish_HUD.Input.MouseEventArgs> _onGlobalMouseRelease;

        public SongPickerPanel(Container parent, List<string> songFiles, string selectedPath, Action<string> onSelected)
        {
            _parent = parent;
            _allFiles = songFiles;
            _selectedPath = selectedPath;
            _onSelected = onSelected;

            _window = new Panel
            {
                Title = "Choose Rotation",
                ShowBorder = true,
                Size = new Point(WindowWidth, WindowHeight),
                Parent = parent,
                Visible = false,
                ZIndex = Screen.CONTEXTMENU_BASEINDEX + 1,
            };

            // Panel has no built-in dragging (that's a WindowBase2/StandardWindow feature).
            // Only start a drag from a click in the title bar strip, so it doesn't fight
            // with clicking the search box or list underneath it. The actual movement is
            // driven from UpdateDrag() (called every frame from the module's Update loop)
            // rather than the MouseMoved event, so a fast drag doesn't break when the
            // cursor momentarily leaves the window's bounds.
            _window.LeftMouseButtonPressed += (s, e) =>
            {
                if (_window.RelativeMousePosition.Y <= TitleBarHeight)
                {
                    _dragging = true;
                    _dragOffset = GameService.Input.Mouse.Position - _window.Location;
                }
            };

            _onGlobalMouseRelease = (s, e) => { _dragging = false; };
            GameService.Input.Mouse.LeftMouseButtonReleased += _onGlobalMouseRelease;

            // ShowBorder (set below) already draws Panel's own faint corner/edge accent
            // textures plus a very light tint - that's the "game looking" panel texture
            // other modules have. It's just too faint on its own for text legibility, so
            // darken it further with a translucent (not fully solid) overlay on top,
            // letting those accents still show through instead of blocking them outright.
            new BackdropImage
            {
                Texture = ContentService.Textures.Pixel,
                Tint = Color.Black * 0.55f,
                Size = new Point(WindowWidth, WindowHeight),
                Location = Point.Zero,
                Parent = _window,
            };

            _search = new TextBox
            {
                PlaceholderText = "Search...",
                Width = WindowWidth - 40,
                Location = new Point(10, 40),
                Parent = _window,
            };
            _search.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(TextBox.Text)) RefreshList(); };

            var closeLabel = new Label
            {
                Text = "Close",
                TextColor = Color.LightGray,
                AutoSizeWidth = true,
                Location = new Point(WindowWidth - 60, 10),
                Parent = _window,
            };
            closeLabel.Click += (s, e) => Hide();

            var scrollPanel = new Panel
            {
                Location = new Point(10, 74),
                Size = new Point(WindowWidth - 20, WindowHeight - 90),
                CanScroll = true,
                Parent = _window,
            };

            _list = new FlowPanel
            {
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                Parent = scrollPanel,
            };

            RefreshList();
        }

        public void Toggle()
        {
            _window.Visible = !_window.Visible;
            if (_window.Visible)
            {
                // Centered on first open only, using the screen's *current* size rather
                // than whatever it reported back at module load (which could still have
                // been zero/placeholder, pinning the window at (0,0) forever). Once the
                // player has dragged it, leave it wherever they put it.
                if (!_hasBeenPositioned)
                {
                    _window.Location = new Point(
                        Math.Max(0, _parent.Width / 2 - WindowWidth / 2),
                        Math.Max(0, _parent.Height / 2 - WindowHeight / 2));
                    _hasBeenPositioned = true;
                }

                _search.Text = "";
                RefreshList();
            }
        }

        /// <summary>Call every frame (e.g. from the module's Update) to drive dragging.</summary>
        public void UpdateDrag()
        {
            if (_dragging)
            {
                _window.Location = GameService.Input.Mouse.Position - _dragOffset;
            }
        }

        public void Hide() => _window.Visible = false;

        public void UpdateSelectedPath(string path)
        {
            _selectedPath = path;
            RefreshList();
        }

        public void UpdateFiles(List<string> songFiles)
        {
            _allFiles.Clear();
            _allFiles.AddRange(songFiles);
            RefreshList();
        }

        private void RefreshList()
        {
            _list.ClearChildren();

            string filter = _search.Text ?? "";

            var matches = string.IsNullOrEmpty(filter)
                ? _allFiles
                : _allFiles.Where(f => Path.GetFileNameWithoutExtension(f).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            foreach (var file in matches)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                bool isSelected = string.Equals(file, _selectedPath, StringComparison.OrdinalIgnoreCase);

                var row = new Label
                {
                    Text = isSelected ? $"* {name}" : name,
                    TextColor = isSelected ? Color.Gold : Color.White,
                    AutoSizeHeight = true,
                    AutoSizeWidth = true,
                    Parent = _list,
                };
                row.Click += (s, e) =>
                {
                    _selectedPath = file;
                    _onSelected(file);
                    Hide();
                };
            }

            if (matches.Count == 0)
            {
                new Label
                {
                    Text = "(no matches)",
                    TextColor = Color.Gray,
                    AutoSizeHeight = true,
                    AutoSizeWidth = true,
                    Parent = _list,
                };
            }
        }

        public void Dispose()
        {
            GameService.Input.Mouse.LeftMouseButtonReleased -= _onGlobalMouseRelease;
            _window.Dispose();
        }
    }
}
