using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.VisualBasic.FileIO;
using MessageBox = System.Windows.MessageBox;

namespace LargeFileFinder
{
    public partial class MainWindow : Window
    {
        private const int BatchSize = 100;
        private static readonly TimeSpan BatchInterval = TimeSpan.FromMilliseconds(250);

        public ObservableCollection<FileDetail> Files { get; set; } = [];
        private ObservableCollection<FileDetail> AllFiles { get; set; } = [];

        public MainWindow()
        {
            InitializeComponent();
            dgFiles.ItemsSource = Files;
        }

        private async void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            string path = txtSearchPath.Text.Trim();
            if (!int.TryParse(txtMinSize.Text, out int sizeLimit) || sizeLimit < 0)
            {
                MessageBox.Show("Invalid size limit.");
                return;
            }

            if (!Directory.Exists(path))
            {
                MessageBox.Show($"Folder not found:\n{path}", "Invalid Search Path", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            long multiplier = cmbSizeUnit.SelectedIndex switch
            {
                0 => // MB
                    1024 * 1024,
                1 => // GB
                    1024 * 1024 * 1024,
                2 => // TB
                    1024L * 1024 * 1024 * 1024,
                _ => 1
            };

            long sizeLimitBytes = sizeLimit * multiplier;
            bool skipSystemDirectories = chkSkipSystemDirs.IsChecked ?? true;

            AllFiles = [];
            txtSearch.Clear();
            Files.Clear();

            // The progress window is modeless, so lock the controls that would interfere with a running scan
            SetScanControlsEnabled(false);

            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            ProgressWindow progressWindow = new(cancellationTokenSource)
            {
                Owner = this
            };
            progressWindow.Show();
            progressWindow.UpdateStatus("Scanning directories...");

            bool completed = false;
            List<FileDetail> batch = new(BatchSize);
            try
            {
                await Task.Run(() =>
                {
                    using var largeFiles = new LargeFileEnumerator(
                        path, sizeLimitBytes, skipSystemDirectories, progressWindow.UpdateCurrentDirectory, cancellationToken);
                    var batchTimer = Stopwatch.StartNew();

                    while (largeFiles.MoveNext())
                    {
                        var fileInfo = largeFiles.Current;
                        batch.Add(new FileDetail
                        {
                            FileName = fileInfo.Name,
                            SizeMB = fileInfo.Length / (1024 * 1024),
                            FullPath = fileInfo.FullName,
                            LastModified = fileInfo.LastWriteTime
                        });

                        // Hand results to the UI in batches to keep dispatcher traffic low
                        if (batch.Count >= BatchSize || batchTimer.Elapsed >= BatchInterval)
                        {
                            var items = batch.ToArray();
                            batch.Clear();
                            batchTimer.Restart();

                            Dispatcher.Invoke(() =>
                            {
                                foreach (var item in items)
                                    Files.Add(item);
                                progressWindow.UpdateFoundFiles(Files.Count);
                            });
                        }
                    }
                }, cancellationToken);
                completed = true;
            }
            catch (OperationCanceledException)
            {
                // Cancelled from the progress window; keep what was found so far
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Scan Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // The scan task has finished, so the last partial batch is safe to read here
                foreach (var item in batch)
                    Files.Add(item);

                if (!progressWindow.IsClosed)
                    progressWindow.Close();

                AllFiles = new ObservableCollection<FileDetail>(Files); // For backup
                SetScanControlsEnabled(true);
            }

            if (completed)
            {
                txtStatus.Text = $"Found {Files.Count} large files.";
                MessageBox.Show($"Done. Found {Files.Count} large files.", "Done!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (cancellationToken.IsCancellationRequested)
            {
                txtStatus.Text = $"Scan cancelled. Showing {Files.Count} large files found so far.";
            }
        }

        private void SetScanControlsEnabled(bool enabled)
        {
            UIElement[] controls =
            [
                txtMinSize, cmbSizeUnit, txtSearchPath, btnBrowse, btnScan, chkSkipSystemDirs,
                txtSearch, btnClearSearch, btnSelectAll, btnDeselectAll,
                btnOpenLocation, btnDeleteSelected, btnMoveToRecycle
            ];

            foreach (var control in controls)
                control.IsEnabled = enabled;
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var file in Files) file.IsSelected = true;
            dgFiles.Items.Refresh();
        }

        private void TxtSearch_TextChanged(object sender, RoutedEventArgs e)
        {
            string searchTerm = txtSearch.Text.ToLower();

            var filteredFiles = string.IsNullOrWhiteSpace(searchTerm) 
                ? AllFiles 
                : new ObservableCollection<FileDetail>(AllFiles.Where(f => f.FileName.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase)));

            Files.Clear();
            foreach (var file in filteredFiles)
            {
                Files.Add(file);
            }
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            Files.Clear();
            foreach (var file in AllFiles)
            {
                Files.Add(file);
            }
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var file in Files) file.IsSelected = false;
            dgFiles.Items.Refresh();
        }

        private void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedFiles = Files.Where(f => f.IsSelected).ToList();
            
            if (selectedFiles.Count == 0)
            {
                MessageBox.Show("Please select at least one file to delete.", "No Files Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Show confirmation dialog
            string message = selectedFiles.Count == 1 
                ? $"Are you sure you want to permanently delete this file?\n\n{selectedFiles[0].FileName}\n\nThis action cannot be undone."
                : $"Are you sure you want to permanently delete {selectedFiles.Count} selected files?\n\nThis action cannot be undone.";

            MessageBoxResult result = MessageBox.Show(message, "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            int deletedCount = 0;
            foreach (var file in selectedFiles)
            {
                try
                {
                    File.Delete(file.FullPath);
                    Files.Remove(file);
                    AllFiles.Remove(file);
                    deletedCount++;
                }
                catch (Exception ex) 
                { 
                    MessageBox.Show($"Failed to delete file: {file.FileName}\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            MessageBox.Show($"Done. {deletedCount} file(s) deleted permanently.", "Delete Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            txtStatus.Text = $"{deletedCount} selected files deleted.";
            dgFiles.Items.Refresh();
        }

        private void BtnMoveToRecycle_Click(object sender, RoutedEventArgs e)
        {
            var selectedFiles = Files.Where(f => f.IsSelected).ToList();

            if (selectedFiles.Count == 0)
            {
                MessageBox.Show("Please select at least one file to move to Recycle Bin.", "No Files Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Show confirmation dialog
            string message = selectedFiles.Count == 1 
                ? $"Are you sure you want to move this file to Recycle Bin?\n\n{selectedFiles[0].FileName}\n\nYou can restore it from Recycle Bin later."
                : $"Are you sure you want to move {selectedFiles.Count} selected files to Recycle Bin?\n\nYou can restore them from Recycle Bin later.";

            MessageBoxResult result = MessageBox.Show(message, "Confirm Move to Recycle Bin", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            int movedCount = 0;
            foreach (var file in selectedFiles)
            {
                try
                {
                    FileSystem.DeleteFile(file.FullPath,
                        UIOption.OnlyErrorDialogs,
                        RecycleOption.SendToRecycleBin);
                    Files.Remove(file);
                    AllFiles.Remove(file);
                    movedCount++;
                }
                catch (Exception ex) 
                { 
                    MessageBox.Show($"Failed to move file to Recycle Bin: {file.FileName}\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            MessageBox.Show($"Done. {movedCount} file(s) moved to Recycle Bin.", "Move Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            txtStatus.Text = $"{movedCount} selected files moved to Recycle Bin.";
            dgFiles.Items.Refresh();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                txtSearchPath.Text = dialog.SelectedPath;
            }
        }

        private void BtnOpenLocation_Click(object sender, RoutedEventArgs e)
        {
            var selectedFiles = Files.Where(f => f.IsSelected).ToList();
            
            if (selectedFiles.Count == 0)
            {
                MessageBox.Show("Please select at least one file to open its location.", "No Files Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Get unique directories to avoid opening the same folder multiple times
            var uniqueDirectories = selectedFiles
                .Select(f => Path.GetDirectoryName(f.FullPath))
                .Distinct()
                .Where(dir => !string.IsNullOrEmpty(dir))
                .ToList();

            foreach (var directory in uniqueDirectories)
            {
                try
                {
                    // Open File Explorer and navigate to the directory
                    if (directory != null) 
                        System.Diagnostics.Process.Start("explorer.exe", directory);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open location: {directory}\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            txtStatus.Text = $"Opened {uniqueDirectories.Count} folder location(s).";
        }
    }

    public class FileDetail
    {
        public required string FileName { get; set; }
        public long SizeMB { get; set; }
        public required string FullPath { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsSelected { get; set; }
    }
}