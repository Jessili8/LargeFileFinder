using System.Windows;

namespace LargeFileFinder
{
    public partial class ProgressWindow : Window
    {
        private readonly Window? _owner;
        private readonly CancellationTokenSource? _cancellationTokenSource;

        public ProgressWindow(Window? owner = null, CancellationTokenSource? cancellationTokenSource = null)
        {
            InitializeComponent();
            _owner = owner;
            _cancellationTokenSource = cancellationTokenSource;
        }

        public void UpdateProgress(string currentDirectory, string status, int foundFiles)
        {
            Dispatcher.Invoke(() =>
            {
                txtCurrentDirectory.Text = currentDirectory;
                txtStatus.Text = status;
                txtFoundFiles.Text = $" | Found: {foundFiles} files";
            });
        }

        public void UpdateCurrentDirectory(string directory)
        {
            Dispatcher.Invoke(() =>
            {
                txtCurrentDirectory.Text = directory;
            });
        }

        public void UpdateFoundFiles(int count)
        {
            Dispatcher.Invoke(() =>
            {
                txtFoundFiles.Text = $" | Found: {count} files";
            });
        }

        public void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                txtStatus.Text = status;
            });
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                txtStatus.Text = "Cancelling scan...";
                btnCancel.IsEnabled = false;
                _cancellationTokenSource.Cancel();
            }
        }
    }
}
