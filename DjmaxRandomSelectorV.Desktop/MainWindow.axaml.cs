using System.Runtime.InteropServices;
using Avalonia.Controls;
using Dmrsv.RandomSelector.Input;

namespace DjmaxRandomSelectorV.Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            PlatformText.Text = $"Platform: {RuntimeInformation.OSDescription} ({RuntimeInformation.ProcessArchitecture})";

            // Demonstrates the cross-platform core selecting an OS input backend.
            var backend = InputSender.Create().GetType().Name;
            BackendText.Text = $"Input backend: {backend}";
        }
    }
}
