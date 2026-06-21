using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using DjmaxRandomSelectorV.Desktop.Platform;
using DjmaxRandomSelectorV.Desktop.Services;
using Dmrsv.RandomSelector;

namespace DjmaxRandomSelectorV.Desktop
{
    public partial class MainWindow : Window
    {
        // macOS virtual key code for F7 (kVK_F7).
        private const uint F7KeyCode = 0x62;

        private SelectorService? _selector;
        private IGlobalHotkey? _hotkey;
        private int _busy;

        public MainWindow()
        {
            InitializeComponent();

            SelectButton.Click += (_, _) => TriggerSelect();
            Opened += OnOpened;
            Closed += OnClosed;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Data", "AllTrackList.json");
                var tracks = TrackLoader.Load(path);
                _selector = new SelectorService(tracks);

                _hotkey = GlobalHotkey.Create();
                bool registered = _hotkey.Register(F7KeyCode, () => Dispatcher.UIThread.Post(TriggerSelect));

                StatusText.Text = registered
                    ? $"{_selector.CandidateCount} tracks loaded · press F7 in-game, or click the button."
                    : $"{_selector.CandidateCount} tracks loaded · global F7 unavailable on this platform — use the button.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to load track list: {ex.Message}";
                SelectButton.IsEnabled = false;
            }
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            _hotkey?.Dispose();
            _hotkey = null;
        }

        private void TriggerSelect()
        {
            if (_selector is null)
            {
                return;
            }
            // Ignore re-entrancy while a selection (which sleeps during locate) runs.
            if (Interlocked.Exchange(ref _busy, 1) == 1)
            {
                return;
            }

            Task.Run(() =>
            {
                Pattern? selected = null;
                string? error = null;
                try
                {
                    selected = _selector.SelectRandom();
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
                finally
                {
                    Interlocked.Exchange(ref _busy, 0);
                }

                Dispatcher.UIThread.Post(() => ShowResult(selected, error));
            });
        }

        private void ShowResult(Pattern? selected, string? error)
        {
            if (error is not null)
            {
                StatusText.Text = $"Error: {error}";
                return;
            }
            if (selected is null)
            {
                StatusText.Text = "No track matches the current filter.";
                return;
            }

            TitleText.Text = selected.Info.Title;
            ComposerText.Text = selected.Info.Composer;
            PatternText.Text = $"{selected.Style} · Lv.{selected.Level}";
        }
    }
}
