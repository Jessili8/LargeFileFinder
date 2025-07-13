using System.Windows;

namespace LargeFileFinder
{
    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            InitializeComponent();
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
    }
}
