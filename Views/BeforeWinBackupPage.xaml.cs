using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BctWinsetup.Views
{
    public partial class BeforeWinBackupPage : UserControl
    {
        private string _desktopPath = string.Empty;
        private List<string> _filesToBackup = new List<string>();
        private long _totalBackupSize = 0;

        public BeforeWinBackupPage()
        {
            InitializeComponent();
            _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            SetDefaultDestPath();
        }

        private void SetDefaultDestPath()
        {
            try
            {
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady && !d.Name.StartsWith("C:", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(d => d.AvailableFreeSpace)
                    .ToList();

                string defaultDrive = drives.Count > 0 ? drives[0].Name : "C:\\";
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                TxtDestPath.Text = Path.Combine(defaultDrive, "BctWinsetup_Backup_" + timestamp);
            }
            catch
            {
                TxtDestPath.Text = @"C:\BctWinsetup_Backup";
            }
        }

        private void BtnBrowseDest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = "Chọn thư mục lưu trữ sao lưu",
                    InitialDirectory = "D:\\"
                };
                if (dialog.ShowDialog() == true)
                {
                    TxtDestPath.Text = dialog.FolderName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở hộp thoại chọn thư mục: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnScanDesktop_Click(object sender, RoutedEventArgs e)
        {
            BtnScanDesktop.IsEnabled = false;
            BtnStartBackup.IsEnabled = false;
            TxtDesktopStats.Text = "Đang quét dữ liệu Desktop...";
            _filesToBackup.Clear();
            _totalBackupSize = 0;

            await Task.Run(() =>
            {
                if (!Directory.Exists(_desktopPath)) return;
                ScanDirectoryRecursively(_desktopPath);
            });

            TxtDesktopStats.Text = $"Quét xong: Tìm thấy {_filesToBackup.Count} tệp tin. Tổng dung lượng: {FormatSize(_totalBackupSize)}";
            BtnScanDesktop.IsEnabled = true;
            if (_filesToBackup.Count > 0)
            {
                BtnStartBackup.IsEnabled = true;
            }
        }

        private void ScanDirectoryRecursively(string path)
        {
            try
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    string ext = Path.GetExtension(file).ToLower();
                    if (ext == ".lnk" || ext == ".url") continue;

                    try
                    {
                        var fi = new FileInfo(file);
                        _filesToBackup.Add(file);
                        _totalBackupSize += fi.Length;
                    }
                    catch { }
                }

                foreach (string dir in Directory.GetDirectories(path))
                {
                    ScanDirectoryRecursively(dir);
                }
            }
            catch { }
        }

        private async void BtnStartBackup_Click(object sender, RoutedEventArgs e)
        {
            string destFolder = TxtDestPath.Text.Trim();
            if (string.IsNullOrEmpty(destFolder))
            {
                MessageBox.Show("Vui lòng chỉ định thư mục sao lưu đích.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnStartBackup.IsEnabled = false;
            BtnScanDesktop.IsEnabled = false;
            BtnBrowseDest.IsEnabled = false;
            BtnExportApps.IsEnabled = false;
            TxtLog.Clear();

            AppendLog("=== BẮT ĐẦU TIẾN TRÌNH SAO LƯU DỮ LIỆU DESKTOP ===");
            AppendLog($"Từ: {_desktopPath}");
            AppendLog($"Đến: {destFolder}");
            AppendLog($"Tổng số tệp: {_filesToBackup.Count}");
            AppendLog($"Tổng dung lượng: {FormatSize(_totalBackupSize)}");
            AppendLog("--------------------------------------------------");

            ProgBar.Value = 0;
            TxtPercentage.Text = "0%";

            var progressLog = new Progress<string>(msg => AppendLog(msg));
            var progressStatus = new System.Progress<string>(status => TxtStatus.Text = status);
            var progressPercentage = new Progress<int>(percent =>
            {
                ProgBar.Value = percent;
                TxtPercentage.Text = $"{percent}%";
            });

            bool success = await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(destFolder))
                    {
                        Directory.CreateDirectory(destFolder);
                    }

                    int total = _filesToBackup.Count;
                    long processedBytes = 0;

                    for (int i = 0; i < total; i++)
                    {
                        string sourceFile = _filesToBackup[i];
                        string relativePath = sourceFile.Substring(_desktopPath.Length).TrimStart('\\', '/');
                        string destFile = Path.Combine(destFolder, "Desktop", relativePath);

                        try
                        {
                            string? dir = Path.GetDirectoryName(destFile);
                            if (dir != null && !Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }

                            var fi = new FileInfo(sourceFile);
                            long fileSize = fi.Length;

                            ((IProgress<string>)progressStatus).Report($"Đang copy ({i + 1}/{total}): {Path.GetFileName(sourceFile)}");
                            File.Copy(sourceFile, destFile, true);
                            
                            processedBytes += fileSize;
                            ((IProgress<string>)progressLog).Report($"[Copy] {relativePath} ({FormatSize(fileSize)})");

                            int percent = (int)((double)(i + 1) / total * 100);
                            ((IProgress<int>)progressPercentage).Report(percent);
                        }
                        catch (Exception ex)
                        {
                            ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể copy {relativePath}: {ex.Message}");
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    ((IProgress<string>)progressLog).Report($"[Lỗi Nghiêm Trọng] {ex.Message}");
                    return false;
                }
            });

            AppendLog("--------------------------------------------------");
            if (success)
            {
                AppendLog("=== TIẾN TRÌNH SAO LƯU HOÀN TẤT THÀNH CÔNG ===");
                TxtStatus.Text = "Sao lưu hoàn tất!";
            }
            else
            {
                AppendLog("=== TIẾN TRÌNH SAO LƯU THẤT BẠI HOẶC BỊ GIÁN ĐOẠN ===");
                TxtStatus.Text = "Sao lưu thất bại!";
            }

            BtnScanDesktop.IsEnabled = true;
            BtnBrowseDest.IsEnabled = true;
            BtnExportApps.IsEnabled = true;
            ProgBar.Value = 100;
            TxtPercentage.Text = "100%";
        }

        private async void BtnExportApps_Click(object sender, RoutedEventArgs e)
        {
            string destFolder = TxtDestPath.Text.Trim();
            if (string.IsNullOrEmpty(destFolder))
            {
                MessageBox.Show("Vui lòng chỉ định thư mục sao lưu đích.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnExportApps.IsEnabled = false;
            TxtLog.Clear();
            AppendLog("=== ĐANG QUÉT DANH SÁCH ỨNG DỤNG ĐÃ CÀI ĐẶT ===");
            
            ProgBar.IsIndeterminate = true;
            TxtStatus.Text = "Đang truy vấn Registry hệ thống...";

            var appList = await Task.Run(() => GetInstalledApplications());

            ProgBar.IsIndeterminate = false;
            ProgBar.Value = 50;

            string txtFilePath = Path.Combine(destFolder, "Danh_Sach_Phan_Mem_Da_Cai.txt");

            bool success = await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(destFolder))
                    {
                        Directory.CreateDirectory(destFolder);
                    }

                    using (var writer = new StreamWriter(txtFilePath, false, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine("==================================================");
                        writer.WriteLine("DANH SÁCH PHẦN MỀM ĐÃ CÀI TRƯỚC KHI CÀI WIN");
                        writer.WriteLine($"Thời gian quét: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                        writer.WriteLine($"Tổng số phần mềm phát hiện: {appList.Count}");
                        writer.WriteLine("==================================================");
                        writer.WriteLine();

                        int index = 1;
                        foreach (var app in appList)
                        {
                            writer.WriteLine($"{index:D3}. {app}");
                            index++;
                        }
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            });

            ProgBar.Value = 100;
            TxtPercentage.Text = "100%";

            if (success)
            {
                foreach (var app in appList)
                {
                    AppendLog(app);
                }
                AppendLog("--------------------------------------------------");
                AppendLog($"=== QUÉT HOÀN TẤT: Đã tìm thấy {appList.Count} phần mềm ===");
                AppendLog($"Danh sách đã được xuất ra file:");
                AppendLog(txtFilePath);
                TxtStatus.Text = "Xuất danh sách thành công!";
            }
            else
            {
                AppendLog("[Lỗi] Không thể ghi file danh sách ứng dụng.");
                TxtStatus.Text = "Xuất danh sách thất bại!";
            }

            BtnExportApps.IsEnabled = true;
        }

        private List<string> GetInstalledApplications()
        {
            var apps = new SortedSet<string>();

            string[] uninstallKeys = {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (string keyPath in uninstallKeys)
            {
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
                using (var key = hklm.OpenSubKey(keyPath))
                {
                    if (key != null)
                    {
                        QueryUninstallKey(key, apps);
                    }
                }
            }

            using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
            using (var key = hkcu.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall"))
            {
                if (key != null)
                {
                    QueryUninstallKey(key, apps);
                }
            }

            return apps.ToList();
        }

        private void QueryUninstallKey(RegistryKey key, SortedSet<string> apps)
        {
            foreach (string subkeyName in key.GetSubKeyNames())
            {
                using (var subkey = key.OpenSubKey(subkeyName))
                {
                    if (subkey == null) continue;

                    string name = subkey.GetValue("DisplayName") as string ?? string.Empty;
                    string version = subkey.GetValue("DisplayVersion") as string ?? string.Empty;
                    int systemComponent = (int)(subkey.GetValue("SystemComponent") ?? 0);
                    string parentKeyName = subkey.GetValue("ParentKeyName") as string ?? string.Empty;

                    if (string.IsNullOrEmpty(name)) continue;
                    if (systemComponent != 0) continue;
                    if (!string.IsNullOrEmpty(parentKeyName)) continue;

                    if (name.Contains("Update for Windows", StringComparison.OrdinalIgnoreCase)) continue;
                    if (name.Contains("Security Update", StringComparison.OrdinalIgnoreCase)) continue;
                    if (name.StartsWith("KB", StringComparison.OrdinalIgnoreCase) && name.Length > 6 && char.IsDigit(name[2])) continue;

                    string appEntry = name.Trim();
                    if (!string.IsNullOrEmpty(version))
                    {
                        appEntry += $" ({version.Trim()})";
                    }

                    apps.Add(appEntry);
                }
            }
        }

        private void AppendLog(string message)
        {
            if (Dispatcher.CheckAccess())
            {
                TxtLog.AppendText(message + "\n");
                TxtLog.ScrollToEnd();
            }
            else
            {
                Dispatcher.Invoke(() => AppendLog(message));
            }
        }

        private string FormatSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n2} {suffixes[counter]}";
        }
    }
}
