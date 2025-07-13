# Large File Finder

A powerful WPF application for Windows that helps you find, manage, and clean up large files on your system. Perfect for disk cleanup and storage management.

## Features

### 🔍 **Smart File Scanning**
- Scan any directory (including entire drives) for large files
- Configurable size thresholds with flexible units (MB, GB, TB)
- Recursive directory scanning with permission-aware access
- Real-time progress tracking

### 📊 **Detailed File Information**
- File name and full path display
- File size in MB with precise calculations
- Last modified date for better file management
- Sortable data grid for easy browsing

### 🛠️ **File Management Actions**
- **Select/Deselect All**: Bulk selection controls
- **Open Location**: Navigate to file locations in Windows Explorer
- **Delete Selected**: Permanently remove files (with confirmation)
- **Move to Recycle Bin**: Safely move files to recycle bin (with confirmation)

### 🔒 **Safety Features**
- Confirmation dialogs for all destructive actions
- Different warning levels for permanent deletion vs. recycle bin
- Individual file error reporting
- Safe directory enumeration (skips inaccessible folders)

### 🎯 **User-Friendly Interface**
- Clean, modern WPF interface
- Intuitive file browser for path selection
- Real-time status updates
- Responsive design with proper window sizing

## Screenshots

### Main Interface
The application features a clean, organized interface with:
- **Search Settings Panel**: Configure size limits and search paths
- **Results Grid**: View found files with detailed information
- **Action Buttons**: Manage selected files with various operations
- **Status Bar**: Real-time feedback and progress information

## System Requirements

- **Operating System**: Windows 10/11 (x64)
- **.NET Runtime**: .NET 9.0 or later
- **Framework**: WPF (Windows Presentation Foundation)
- **Additional**: Windows Forms support for folder browser dialog

## Installation

### Option 1: Build from Source
1. **Clone or download** the project files
2. **Open terminal/command prompt** in the project directory
3. **Build the application**:
   ```bash
   dotnet build
   ```
4. **Run the application**:
   ```bash
   dotnet run
   ```

### Option 2: Create Executable
1. **Build release version**:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained
   ```
2. **Find executable** in: `bin\Release\net9.0-windows\win-x64\publish\`

## Usage Guide

### 1. **Configure Search Settings**
- **Minimum Size**: Enter the threshold size for "large" files
- **Size Unit**: Select from MB, GB, or TB using the dropdown
- **Search Path**: 
  - Type the path manually (e.g., `C:\`, `D:\Users\`)
  - Use the **Browse** button to select a folder graphically

### 2. **Start Scanning**
- Click the **Scan** button to begin searching
- Wait for the scan to complete (status shown in status bar)
- Found files will appear in the results grid

### 3. **Review Results**
- **Sort columns** by clicking column headers
- **Review file information**: name, size, path, last modified date
- **Select files** using the checkboxes in the first column

### 4. **Manage Files**
- **Select All/Deselect All**: Use bulk selection buttons
- **Open Location**: Navigate to selected files in Windows Explorer
- **Delete Selected**: Permanently remove files (⚠️ **Warning**: Cannot be undone!)
- **Move to Recycle Bin**: Safely move files to recycle bin (can be restored)

### 5. **Safety Confirmations**
- **Delete confirmation**: Shows warning dialog with file count/names
- **Recycle bin confirmation**: Shows question dialog with restore information
- **Error handling**: Individual file errors are reported separately

## Example Usage Scenarios

### 🧹 **Disk Cleanup**
1. Set minimum size to `500 MB`
2. Scan `C:\` drive
3. Review large files and old downloads
4. Move unnecessary files to recycle bin

### 📂 **Project Cleanup**
1. Set minimum size to `50 MB`
2. Browse to your projects folder
3. Find large build artifacts or cache files
4. Delete outdated compilation outputs

### 🎮 **Game Directory Management**
1. Set minimum size to `1 GB`
2. Scan game installation directories
3. Identify large game files or updates
4. Clean up old game installations

## Technical Details

### **Architecture**
- **Framework**: WPF (.NET 9.0)
- **Language**: C# with modern language features
- **UI Pattern**: MVVM-like with ObservableCollection
- **File Operations**: Native .NET File I/O + VB.NET FileSystem for recycle bin

### **Key Components**
- **MainWindow.xaml**: UI layout and controls
- **MainWindow.xaml.cs**: Application logic and event handlers
- **FileDetail**: Data model for file information
- **SafeEnumerateFiles**: Recursive directory scanning with error handling

### **Safety Mechanisms**
- **Permission handling**: Gracefully skips inaccessible directories
- **Error isolation**: Individual file operation errors don't stop the process
- **Confirmation dialogs**: Prevent accidental data loss
- **Status feedback**: Real-time operation status and results

## Troubleshooting

### **Common Issues**

**"Access Denied" errors**
- Normal behavior for system directories
- Application automatically skips inaccessible folders
- Run as administrator for broader access (if needed)

**Large scan taking too long**
- Scanning entire drives (especially C:) can take time
- Consider scanning specific folders instead
- Check status bar for progress updates

**Files not appearing in results**
- Verify the minimum size threshold
- Check the selected size unit (MB/GB/TB)
- Ensure the search path is correct

**Cannot delete some files**
- Files may be in use by other applications
- Some system files are protected
- Check individual error messages for details

## Contributing

This is an open-source project. Feel free to:
- Report bugs or issues
- Suggest new features
- Submit pull requests
- Improve documentation

## License

This project is provided as-is for educational and personal use.

## Version History

### v1.0.0 (Current)
- Initial release
- Core file scanning functionality
- WPF user interface
- File management operations
- Safety confirmations
- Folder browser integration

---

**⚠️ Important Safety Notice**: Always review files carefully before deletion. While the application provides safety measures, permanent file deletion cannot be undone. Use the recycle bin option when in doubt.

**📧 Support**: For issues or questions, please create an issue in the project repository.
