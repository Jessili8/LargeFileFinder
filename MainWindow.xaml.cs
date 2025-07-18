using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.VisualBasic.FileIO;
using MessageBox = System.Windows.MessageBox;

namespace LargeFileFinder
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<FileDetail> Files { get; set; } = [];
        private ObservableCollection<FileDetail> AllFiles { get; set; } = [];

        public MainWindow()
        {
            InitializeComponent();
            dgFiles.ItemsSource = Files;
        }

        private async void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            Files.Clear();
            string path = txtSearchPath.Text;
            if (!int.TryParse(txtMinSize.Text, out int sizeLimit))
            {
                MessageBox.Show("Invalid size limit.");
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

            // Show progress window
            ProgressWindow progressWindow = new()
            {
                Owner = this
            };
            progressWindow.Show();

            try
            {
                int foundFiles = 0;
                await Task.Run(() =>
                {
                    progressWindow.Dispatcher.Invoke(() => progressWindow.UpdateStatus("Scanning directories..."));

                    foreach (var file in SafeEnumerateFiles(path, "*.*", progressWindow))
                    {
                        FileInfo fileInfo = new(file);
                        progressWindow.Dispatcher.Invoke(() => progressWindow.UpdateStatus("Checking file sizes..."));

                        if (fileInfo.Length > sizeLimitBytes)
                        {
                            foundFiles++;
                            Dispatcher.Invoke(() =>
                            {
                                Files.Add(new FileDetail
                                {
                                    FileName = fileInfo.Name,
                                    SizeMB = fileInfo.Length / (1024 * 1024),
                                    FullPath = fileInfo.FullName,
                                    LastModified = fileInfo.LastWriteTime
                                });
                                progressWindow.UpdateFoundFiles(foundFiles);
                            });
                        }
                    }
                    progressWindow.Dispatcher.Invoke(() => progressWindow.UpdateStatus("Scan completed!"));
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                progressWindow.Close();
            }

            MessageBox.Show($"Done. Found {Files.Count} large files.", "Done!", MessageBoxButton.OK, MessageBoxImage.Information);
            txtStatus.Text = $"Found {Files.Count} large files.";
            AllFiles = new ObservableCollection<FileDetail>(Files); // For backup
        }

        private static List<string> SafeEnumerateFiles(string path, string searchPattern, ProgressWindow progressWindow = null)
        {
            Queue<string> directories = new();
            directories.Enqueue(path);

            List<string> enumerateFiles = [];
            while (directories.Count > 0)
            {
                try
                {
                    string currentDir = directories.Dequeue();
                    progressWindow?.UpdateCurrentDirectory(currentDir);
                    
                    // Get all files in current directory
                    var files = Directory.GetFiles(currentDir, searchPattern);
                    enumerateFiles.AddRange(files);
                    
                    // Add subdirectories to queue
                    foreach (var dir in Directory.GetDirectories(currentDir))
                    {
                        directories.Enqueue(dir);
                    }
                }
                catch (UnauthorizedAccessException) 
                { 
                    // Ignore directories we can't access
                    progressWindow?.UpdateStatus("Skipping restricted directory...");
                }
                catch (Exception ex)
                {
                    progressWindow?.UpdateStatus($"Error: {ex.Message}");
                }
            }

            return enumerateFiles;
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
        public string FileName { get; set; }
        public long SizeMB { get; set; }
        public string FullPath { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsSelected { get; set; }
    }
}