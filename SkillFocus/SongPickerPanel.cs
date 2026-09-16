using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly Panel _window;
        private readonly TextBox _search;
        private readonly FlowPanel _list;
        private readonly List<string> _allFiles;
        private readonly Action<string> _onSelected;
        private string _selectedPath;

        public SongPickerPanel(Container parent, List<string> songFiles, string selectedPath, Action<string> onSelected)
        {
            _allFiles = songFiles;
            _selectedPath = selectedPath;
            _onSelected = onSelected;

            const int windowWidth = 360;
            const int windowHeight = 460;

            _window = new Panel
            {
                Title = "Choose Rotation",
                ShowBorder = true,
                Size = new Point(windowWidth, windowHeight),
                Location = new Point(
                    Math.Max(0, parent.Width / 2 - windowWidth / 2),
                    Math.Max(0, parent.Height / 2 - windowHeight / 2)),
                Parent = parent,
                Visible = false,
                ZIndex = Screen.CONTEXTMENU_BASEINDEX + 1,
            };

            _search = new TextBox
            {
                PlaceholderText = "Search...",
                Width = windowWidth - 40,
                Location = new Point(10, 40),
                Parent = _window,
            };
            _search.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(TextBox.Text)) RefreshList(); };

            var closeLabel = new Label
            {
                Text = "Close",
                TextColor = Color.LightGray,
                AutoSizeWidth = true,
                Location = new Point(windowWidth - 60, 10),
                Parent = _window,
            };
            closeLabel.Click += (s, e) => Hide();

            var scrollPanel = new Panel
            {
                Location = new Point(10, 74),
                Size = new Point(windowWidth - 20, windowHeight - 90),
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
                _search.Text = "";
                RefreshList();
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
            _window.Dispose();
        }
    }
}
