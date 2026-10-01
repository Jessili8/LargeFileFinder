using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;

namespace LargeFileFinder
{
    /// <summary>
    /// Recursively streams files larger than a size limit. File sizes come straight from the
    /// directory listing, so small files cost no extra system call and no FileInfo allocation.
    /// </summary>
    internal sealed class LargeFileEnumerator : FileSystemEnumerator<FileInfo>
    {
        private static readonly EnumerationOptions Options = new()
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            // Include hidden and system files, but don't follow junctions or symlinks (avoids loops and duplicates)
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        // Matched by full path, so a folder that merely shares the name (e.g. D:\Backups\Windows) is still scanned
        private static readonly string[] SystemFolders = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        }.Where(folder => !string.IsNullOrEmpty(folder)).ToArray();

        // Only skipped directly under a drive root, e.g. C:\$Recycle.Bin
        private static readonly string[] DriveRootSystemFolders = ["$Recycle.Bin", "System Volume Information", "PerfLogs"];

        private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(100);

        private readonly long _sizeLimitBytes;
        private readonly bool _skipSystemDirectories;
        private readonly Action<string>? _onProgress;
        private readonly CancellationToken _cancellationToken;
        private readonly Stopwatch _progressTimer = Stopwatch.StartNew();

        /// <param name="onProgress">Called on the enumerating thread with the current directory, at most every 100 ms.</param>
        public LargeFileEnumerator(
            string root,
            long sizeLimitBytes,
            bool skipSystemDirectories,
            Action<string>? onProgress = null,
            CancellationToken cancellationToken = default)
            : base(root, Options)
        {
            _sizeLimitBytes = sizeLimitBytes;
            _skipSystemDirectories = skipSystemDirectories;
            _onProgress = onProgress;
            _cancellationToken = cancellationToken;
        }

        protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (_onProgress != null && _progressTimer.Elapsed >= ProgressInterval)
            {
                _progressTimer.Restart();
                _onProgress(entry.Directory.ToString());
            }

            return !entry.IsDirectory && entry.Length > _sizeLimitBytes;
        }

        // Not called for the scan root, so a folder the user picked explicitly is never skipped
        protected override bool ShouldRecurseIntoEntry(ref FileSystemEntry entry)
            => !_skipSystemDirectories || !IsSystemDirectory(entry.ToFullPath());

        protected override FileInfo TransformEntry(ref FileSystemEntry entry)
            => (FileInfo)entry.ToFileSystemInfo();

        // Skip directories that fail for reasons other than access denied instead of aborting the whole scan
        protected override bool ContinueOnError(int error) => true;

        private static bool IsSystemDirectory(string path)
        {
            if (SystemFolders.Contains(path, StringComparer.OrdinalIgnoreCase))
                return true;

            string? parent = Path.GetDirectoryName(path);
            return parent != null
                && string.Equals(parent, Path.GetPathRoot(path), StringComparison.OrdinalIgnoreCase)
                && DriveRootSystemFolders.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);
        }
    }
}
