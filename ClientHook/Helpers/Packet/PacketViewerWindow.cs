using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClientHook
{
    /// <summary>
    /// Live, scrollable hex+ASCII viewer for captured packets (both directions).
    /// Read-only display of what PacketHook already captured.
    /// </summary>
    internal sealed class PacketViewerWindow
    {
        private const int MaxRows = 1000;

        private Window _window;
        private DataGrid _grid;
        private ObservableCollection<PacketEntry> _entries;
        private int _sequence;

        public void AddPacket(PacketDirection direction, byte[] data)
        {
            EnsureWindow();

            var entry = new PacketEntry
            {
                Seq = ++_sequence,
                Timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
                Direction = direction.ToString(),
                Length = data.Length,
                HexPreview = ToHexPreview(data, 24),
                AsciiPreview = ToAsciiPreview(data, 24),
                RawData = data
            };

            _entries.Add(entry);

            if (_entries.Count > MaxRows)
            {
                _entries.RemoveAt(0);
            }

            if (_grid.Items.Count > 0)
            {
                _grid.ScrollIntoView(_grid.Items[_grid.Items.Count - 1]);
            }
        }

        private void EnsureWindow()
        {
            if (_window != null && _window.IsLoaded)
            {
                return;
            }

            _entries = new ObservableCollection<PacketEntry>();

            _grid = new DataGrid
            {
                ItemsSource = _entries,
                AutoGenerateColumns = false,
                IsReadOnly = true,
                CanUserAddRows = false,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal
            };

            _grid.Columns.Add(new DataGridTextColumn { Header = "#", Binding = new System.Windows.Data.Binding("Seq"), Width = 45 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "Time", Binding = new System.Windows.Data.Binding("Timestamp"), Width = 90 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "Dir", Binding = new System.Windows.Data.Binding("Direction"), Width = 65 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "Len", Binding = new System.Windows.Data.Binding("Length"), Width = 50 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "Hex", Binding = new System.Windows.Data.Binding("HexPreview"), Width = 260 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "ASCII", Binding = new System.Windows.Data.Binding("AsciiPreview"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });

            _grid.LoadingRow += (s, e) =>
            {
                if (e.Row.Item is PacketEntry entry)
                {
                    e.Row.Background = entry.Direction == nameof(PacketDirection.Send)
                        ? new SolidColorBrush(Color.FromRgb(0x2A, 0x3A, 0x2A))
                        : new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x3A));
                }
            };

            _grid.MouseDoubleClick += (s, e) =>
            {
                if (_grid.SelectedItem is PacketEntry entry)
                {
                    ShowFullDump(entry);
                }
            };

            var clearButton = new Button { Content = "Clear", Width = 80, Margin = new Thickness(8) };
            clearButton.Click += (s, e) => _entries.Clear();

            var root = new DockPanel();
            var toolbar = new StackPanel { Orientation = Orientation.Horizontal };
            DockPanel.SetDock(toolbar, Dock.Top);
            toolbar.Children.Add(clearButton);

            root.Children.Add(toolbar);
            root.Children.Add(_grid);

            _window = new Window
            {
                Title = "ClientHook - Packet Viewer",
                Width = 780,
                Height = 480,
                Content = root,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            _window.Show();
        }

        private static void ShowFullDump(PacketEntry entry)
        {
            var dumpWindow = new Window
            {
                Title = $"Packet #{entry.Seq} - {entry.Direction} - {entry.Length} bytes",
                Width = 640,
                Height = 480,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var textBox = new TextBox
            {
                IsReadOnly = true,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                TextWrapping = TextWrapping.NoWrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Text = ToFullHexDump(entry.RawData)
            };

            dumpWindow.Content = textBox;
            dumpWindow.Show();
        }

        private static string ToHexPreview(byte[] data, int maxBytes)
        {
            int take = Math.Min(data.Length, maxBytes);
            var sb = new StringBuilder(take * 3);
            for (int i = 0; i < take; i++)
            {
                sb.Append(data[i].ToString("X2")).Append(' ');
            }
            if (data.Length > take)
            {
                sb.Append("...");
            }
            return sb.ToString().TrimEnd();
        }

        private static string ToAsciiPreview(byte[] data, int maxBytes)
        {
            int take = Math.Min(data.Length, maxBytes);
            var sb = new StringBuilder(take);
            for (int i = 0; i < take; i++)
            {
                byte b = data[i];
                sb.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
            }
            if (data.Length > take)
            {
                sb.Append("...");
            }
            return sb.ToString();
        }

        private static string ToFullHexDump(byte[] data)
        {
            var sb = new StringBuilder();
            for (int offset = 0; offset < data.Length; offset += 16)
            {
                sb.Append(offset.ToString("X8")).Append("  ");

                int rowLen = Math.Min(16, data.Length - offset);
                for (int i = 0; i < 16; i++)
                {
                    if (i < rowLen)
                    {
                        sb.Append(data[offset + i].ToString("X2")).Append(' ');
                    }
                    else
                    {
                        sb.Append("   ");
                    }

                    if (i == 7)
                    {
                        sb.Append(' ');
                    }
                }

                sb.Append(" ");
                for (int i = 0; i < rowLen; i++)
                {
                    byte b = data[offset + i];
                    sb.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
                }

                sb.AppendLine();
            }
            return sb.ToString();
        }

        private sealed class PacketEntry
        {
            public int Seq { get; set; }
            public string Timestamp { get; set; }
            public string Direction { get; set; }
            public int Length { get; set; }
            public string HexPreview { get; set; }
            public string AsciiPreview { get; set; }
            public byte[] RawData { get; set; }
        }
    }
}
