using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WinForms = System.Windows.Forms;
using ZpaqGUI.Properties;
using MessageBox = System.Windows.MessageBox;

namespace ZpaqGUI
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<FileItem> fileItems = new ObservableCollection<FileItem>();
        private ObservableCollection<FileItem> archiveItems = new ObservableCollection<FileItem>();
        private string zpaqPath = string.Empty;
        private string currentArchivePath = string.Empty;
        private int userThreadCount = Environment.ProcessorCount;

        // Model class for file/folder items
        public class FileItem
        {
            public string Name { get; set; } = string.Empty;
            public string FullPath { get; set; } = string.Empty;
            public string Size { get; set; } = string.Empty;
            public string Modified { get; set; } = string.Empty;
        }

        public MainWindow()
        {
            InitializeComponent();

            var fileListView = FindName("FileListView") as System.Windows.Controls.ListView;
            if (fileListView != null)
            {
                fileListView.ItemsSource = fileItems;
            }
            
            var archiveContentsView = FindName("archiveContentsView") as System.Windows.Controls.ListView;
            if (archiveContentsView != null)
            {
                archiveContentsView.ItemsSource = archiveItems;
            }

            // Find zpaq executable
            zpaqPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "zpaq.exe");
            if (!File.Exists(zpaqPath))
            {
                System.Windows.MessageBox.Show("Could not find zpaq.exe. Please ensure it's in the same directory as this application.",
                    "ZPAQ Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Load thread count from settings
            if (Settings.Default.ThreadCount > 0)
                userThreadCount = Settings.Default.ThreadCount;
        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Title = "Select Files to Archive"
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (string file in dialog.FileNames)
                {
                    AddFileToList(file);
                }
            }
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WinForms.FolderBrowserDialog
            {
                Description = "Select Folder to Archive"
            };

            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                AddFileToList(dialog.SelectedPath);
            }
        }

        private void AddFileToList(string path)
        {
            string exeDir = AppContext.BaseDirectory;
            string logPath = Path.Combine(exeDir, "log.txt");
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now}] AddFileToList called with: {path}\n");
                var existingPaths = new HashSet<string>(fileItems.Select(f => f.FullPath), StringComparer.OrdinalIgnoreCase);
                if (Directory.Exists(path))
                {
                    var dirInfo = new DirectoryInfo(path);
                    string dirFullPath = dirInfo.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    if (!existingPaths.Contains(dirFullPath))
                    {
                        long totalSize = 0;
                        try
                        {
                            totalSize = dirInfo.GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
                        }
                        catch { }
                        fileItems.Add(new FileItem
                        {
                            Name = dirInfo.Name + "/",
                            FullPath = dirFullPath,
                            Size = FormatSize(totalSize),
                            Modified = dirInfo.LastWriteTime.ToString("g")
                        });
                    }
                }
                else if (File.Exists(path))
                {
                    var info = new FileInfo(path);
                    if (!existingPaths.Contains(info.FullName))
                    {
                        fileItems.Add(new FileItem
                        {
                            Name = info.Name,
                            FullPath = info.FullName,
                            Size = FormatSize(info.Length),
                            Modified = info.LastWriteTime.ToString("g")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"[{DateTime.Now}] ERROR: {ex}\n");
                System.Windows.MessageBox.Show($"Error adding files or folders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private long GetDirectorySize(DirectoryInfo dirInfo)
        {
            long size = 0;
            try
            {
                // Add size of all files
                foreach (var fileInfo in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        size += fileInfo.Length;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip files we can't access
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we can't access
            }
            return size;
        }

        private async void CreateArchive_Click(object sender, RoutedEventArgs e)
        {
            if (fileItems.Count == 0)
            {
                System.Windows.MessageBox.Show("Please add files to archive first.", "No Files",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "ZPAQ Archives|*.zpaq",
                Title = "Save Archive As"
            };

            if (saveDialog.ShowDialog() == true)
            {
                var progressBar = FindName("progressBar") as System.Windows.Controls.ProgressBar;
                var statusText = FindName("statusText") as TextBlock;
                if (progressBar != null && statusText != null)
                {
                    progressBar.IsIndeterminate = true;
                    progressBar.Visibility = Visibility.Visible;
                    statusText.Text = "Creating archive...";

                    try
                    {
                        await CreateArchiveAsync(saveDialog.FileName);
                        System.Windows.MessageBox.Show("Archive created successfully!", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Error creating archive: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        progressBar.Visibility = Visibility.Collapsed;
                        progressBar.IsIndeterminate = false;
                        statusText.Text = "Ready";
                    }
                }
            }
        }

        private async Task CreateArchiveAsync(string archivePath)
        {
            var level = GetCompressionLevel();
            var files = string.Join(" ", fileItems.Select(f => $"\"{f.FullPath.TrimEnd(Path.DirectorySeparatorChar)}\""));
            int threads = userThreadCount;
            var encryptionCheckBox = FindName("encryptionCheckBox") as System.Windows.Controls.CheckBox;
            var passwordBox = FindName("passwordBox") as System.Windows.Controls.PasswordBox;
            bool ultra = (level == "5");
            bool encryption = encryptionCheckBox?.IsChecked == true && !string.IsNullOrEmpty(passwordBox?.Password);
            if (ultra && encryption && threads > 2)
            {
                var result = MessageBox.Show(
                    "Ultra compression with encryption and more than 2 threads can cause out-of-memory errors. Limit threads to 2?",
                    "Memory Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    threads = 2;
                }
            }
            var arguments = $"a \"{archivePath}\" {files} -m{level} -threads {threads} -summary 1";
            if (encryption && passwordBox != null)
            {
                arguments += $" -key {passwordBox.Password}";
            }
            var progressBar = FindName("progressBar") as System.Windows.Controls.ProgressBar;
            var statusText = FindName("statusText") as TextBlock;
            string? lastError = null;
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = zpaqPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.OutputDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                Dispatcher.Invoke(() =>
                {
                    if (e.Data.Contains("%"))
                    {
                        var parts = e.Data.Split('%');
                        if (parts.Length > 0 && float.TryParse(parts[0], out float progress))
                        {
                            if (progressBar != null)
                            {
                                progressBar.IsIndeterminate = false;
                                progressBar.Maximum = 100;
                                progressBar.Value = progress;
                            }
                        }
                    }
                    if (statusText != null)
                    {
                        statusText.Text = e.Data;
                    }
                });
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    lastError = e.Data;
                    Dispatcher.Invoke(() =>
                    {
                        if (statusText != null)
                        {
                            statusText.Text = "Error: " + e.Data;
                        }
                    });
                }
            };
            await Task.Run(() =>
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new Exception($"ZPAQ process exited with code {process.ExitCode}\n{lastError}");
                }
            });
        }

        private string GetCompressionLevel()
        {
            var comboBox = FindName("compressionLevelComboBox") as System.Windows.Controls.ComboBox;
            return comboBox?.SelectedIndex switch
            {
                0 => "1",  // Fastest
                1 => "2",  // Fast
                2 => "3",  // Default
                3 => "4",  // Maximum
                4 => "5",  // Ultra
                _ => "3"   // Default
            };
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        private async void OpenArchive_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "ZPAQ Archives|*.zpaq",
                Title = "Open ZPAQ Archive"
            };

            if (dialog.ShowDialog() == true)
            {
                currentArchivePath = dialog.FileName;
                var progressBar = FindName("progressBar") as System.Windows.Controls.ProgressBar;
                var statusText = FindName("statusText") as TextBlock;

                if (progressBar != null && statusText != null)
                {
                    progressBar.Visibility = Visibility.Visible;
                    statusText.Text = "Reading archive...";

                    try
                    {
                        await ListArchiveContentsAsync(dialog.FileName);
                        statusText.Text = "Archive opened successfully";
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Error opening archive: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        statusText.Text = "Error opening archive";
                    }
                    finally
                    {
                        progressBar.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private async Task ListArchiveContentsAsync(string archivePath)
        {
            var arguments = $"l \"{archivePath}\"";
            var extractPasswordBox = FindName("extractPasswordBox") as System.Windows.Controls.PasswordBox;
            if (extractPasswordBox != null && !string.IsNullOrEmpty(extractPasswordBox.Password))
            {
                arguments += $" -key {extractPasswordBox.Password}";
            }

            archiveItems.Clear();

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = zpaqPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            await Task.Run(() =>
            {
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"ZPAQ process exited with code {process.ExitCode}");
                }

                // Parse zpaq list output and add to archiveItems
                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;
                    if (char.IsDigit(trimmed[0]) || trimmed[0] == '=' || trimmed[0] == '#' || trimmed[0] == '-' || trimmed[0] == '+')
                    {
                        var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 4)
                        {
                            string name = string.Join(" ", parts.Skip(3));
                            Dispatcher.Invoke(() =>
                            {
                                archiveItems.Add(new FileItem
                                {
                                    Name = name,
                                    Size = parts[2],
                                    Modified = parts[1],
                                    FullPath = name
                                });
                            });
                        }
                    }
                }
            });
        }

        private async void ExtractArchive_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentArchivePath))
            {
                System.Windows.MessageBox.Show("Please open an archive first.", "No Archive",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new WinForms.FolderBrowserDialog
            {
                Description = "Select Folder to Extract To"
            };

            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                var progressBar = FindName("progressBar") as System.Windows.Controls.ProgressBar;
                var statusText = FindName("statusText") as TextBlock;

                if (progressBar != null && statusText != null)
                {
                    progressBar.Visibility = Visibility.Visible;
                    statusText.Text = "Extracting archive...";

                    try
                    {
                        await ExtractArchiveAsync(currentArchivePath, dialog.SelectedPath);
                        System.Windows.MessageBox.Show("Archive extracted successfully!", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        statusText.Text = "Archive extracted successfully";
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Error extracting archive: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        statusText.Text = "Error extracting archive";
                    }
                    finally
                    {
                        progressBar.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private async Task ExtractArchiveAsync(string archivePath, string extractPath)
        {
            int threads = userThreadCount;
            var arguments = $"x \"{archivePath}\" -to \"{extractPath}\" -threads {threads} -summary 1";
            var extractPasswordBox = FindName("extractPasswordBox") as System.Windows.Controls.PasswordBox;
            if (extractPasswordBox != null && !string.IsNullOrEmpty(extractPasswordBox.Password))
            {
                arguments += $" -key {extractPasswordBox.Password}";
            }
            var progressBar = FindName("progressBar") as System.Windows.Controls.ProgressBar;
            var statusText = FindName("statusText") as TextBlock;
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = zpaqPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.OutputDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                Dispatcher.Invoke(() =>
                {
                    if (e.Data.Contains("%"))
                    {
                        var parts = e.Data.Split('%');
                        if (parts.Length > 0 && float.TryParse(parts[0], out float progress))
                        {
                            if (progressBar != null)
                            {
                                progressBar.IsIndeterminate = false;
                                progressBar.Maximum = 100;
                                progressBar.Value = progress;
                            }
                        }
                    }
                    if (statusText != null)
                    {
                        statusText.Text = e.Data;
                    }
                });
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (statusText != null)
                        {
                            statusText.Text = "Error: " + e.Data;
                        }
                    });
                }
            };
            await Task.Run(() =>
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new Exception($"ZPAQ process exited with code {process.ExitCode}");
                }
            });
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            int maxThreads = Math.Max(2, Environment.ProcessorCount);
            var dlg = new SettingsDialog(userThreadCount, maxThreads);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                userThreadCount = dlg.ThreadCount;
                ZpaqGUI.Properties.Settings.Default.ThreadCount = userThreadCount;
                ZpaqGUI.Properties.Settings.Default.Save();
            }
        }

        private void ClearFiles_Click(object sender, RoutedEventArgs e)
        {
            fileItems.Clear();
        }
    }
}
