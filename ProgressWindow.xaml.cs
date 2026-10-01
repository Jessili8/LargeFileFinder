using System.ComponentModel;
using System.Windows;

namespace LargeFileFinder
{
    public partial class ProgressWindow : Window
    {
        private readonly CancellationTokenSource _cancellationTokenSource;

        public bool IsClosed { get; private set; }

        public ProgressWindow(CancellationTokenSource cancellationTokenSource)
        {
            InitializeComponent();
            _cancellationTokenSource = cancellationTokenSource;
        }

        // The Update* methods can be called from any thread

        public void UpdateCurrentDirectory(string directory)
        {
            Dispatcher.InvokeAsync(() => txtCurrentDirectory.Text = directory);
        }

        public void UpdateFoundFiles(int count)
        {
            Dispatcher.InvokeAsync(() => txtFoundFiles.Text = $" | Found: {count} files");
        }

        public void UpdateStatus(string status)
        {
            Dispatcher.InvokeAsync(() => txtStatus.Text = status);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = "Cancelling scan...";
            btnCancel.IsEnabled = false;
            _cancellationTokenSource.Cancel();
        }

        // Closing the window with its X button cancels the scan too
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _cancellationTokenSource.Cancel();
        }

        protected override void OnClosed(EventArgs e)
        {
            IsClosed = true;
            base.OnClosed(e);
        }
    }
}
