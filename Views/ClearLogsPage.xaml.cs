using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BctWinsetup.Views
{
    public partial class ClearLogsPage : UserControl
    {
        public ObservableCollection<LogCategoryItem> LogCategories { get; set; } = new ObservableCollection<LogCategoryItem>();

        public ClearLogsPage()
        {
            InitializeComponent();
            InitializeCategories();
            LstCategories.ItemsSource = LogCategories;

            
            Loaded += ClearLogsPage_Loaded;
        }

        private void InitializeCategories()
        {
            AddCategory("EventLogs", "Nhật ký Sự kiện Windows (Event Logs)", "Ứng dụng, hệ thống, bảo mật và nhật ký sự kiện dịch vụ (Application, System, Security...)", true);
            AddCategory("SystemSetupLogs", "Nhật ký Hệ thống & Cài đặt", "Các tệp nhật ký cài đặt và bảo trì (CBS, DISM, Panther, setupapi.log...)", true);
            AddCategory("ErrorReports", "Báo cáo lỗi & Crash Dumps", "Bộ nhớ đệm báo cáo lỗi Windows Error Reporting, Minidump, MEMORY.DMP", true);
            AddCategory("PSHistory", "Lịch sử dòng lệnh PowerShell", "Lịch sử nhập lệnh của Console PowerShell (PSReadLine)", true);
            AddCategory("Prefetch", "Bộ nhớ đệm Prefetch", "Lịch sử chạy phần mềm và tối ưu hóa khởi động ứng dụng của Windows", true);
            AddCategory("TempFiles", "Tập tin tạm thời (Temp Files)", "Thư mục Temp của hệ thống và người dùng hiện tại", true);
            AddCategory("UpdateCache", "Bộ đệm cập nhật Windows [Nâng cao]", "Các gói cập nhật Windows đã cài đặt xong và bộ nhớ đệm Delivery Optimization (Có thể chiếm vài GB)", false);
        }

        private void AddCategory(string id, string title, string description, bool isSelected)
        {
            var item = new LogCategoryItem
            {
                TargetId = id,
                Title = title,
                Description = description,
                IsSelected = isSelected
            };
            item.PropertyChanged += CategoryItem_PropertyChanged;
            LogCategories.Add(item);
        }

        private void CategoryItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LogCategoryItem.IsSelected))
            {
                UpdateTotalSelectionSize();
            }
        }

        private async void ClearLogsPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ScanAllSizesAsync();
        }

        private async Task ScanAllSizesAsync()
        {
            TxtSummary.Text = "Đang quét dung lượng các mục...";
            BtnStartCleanup.IsEnabled = false;

            foreach (var category in LogCategories)
            {
                category.SizeText = "Đang quét...";
            }

            await Task.Run(() =>
            {
                foreach (var category in LogCategories)
                {
                    long size = CalculateCategorySize(category.TargetId);
                    category.SizeBytes = size;

                    
                    Dispatcher.Invoke(() =>
                    {
                        category.SizeText = FormatSize(size);
                    });
                }
            });

            UpdateTotalSelectionSize();
            BtnStartCleanup.IsEnabled = true;
        }

        private void UpdateTotalSelectionSize()
        {
            long totalBytes = 0;
            foreach (var category in LogCategories)
            {
                if (category.IsSelected)
                {
                    totalBytes += category.SizeBytes;
                }
            }
            TxtSummary.Text = $"Tổng kích thước chọn: {FormatSize(totalBytes)}";
        }

        private long CalculateCategorySize(string targetId)
        {
            long size = 0;
            switch (targetId)
            {
                case "EventLogs":
                    try
                    {
                        var session = EventLogSession.GlobalSession;
                        foreach (var logName in session.GetLogNames())
                        {
                            try
                            {
                                var info = session.GetLogInformation(logName, PathType.LogName);
                                if (info.FileSize.HasValue)
                                {
                                    size += info.FileSize.Value;
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                    break;

                case "SystemSetupLogs":
                    size += SafeGetDirectorySize(@"C:\Windows\Logs\CBS");
                    size += SafeGetDirectorySize(@"C:\Windows\Logs\DISM");
                    size += SafeGetDirectorySize(@"C:\Windows\Panther");
                    size += SafeGetDirectorySize(@"C:\Windows\System32\Sysprep\Panther");
                    size += SafeGetFileSize(@"C:\Windows\setupapi.log");
                    size += SafeGetDirectoryFilesSize(@"C:\Windows\inf", "setupapi*.log");
                    break;

                case "ErrorReports":
                    size += SafeGetDirectorySize(@"C:\ProgramData\Microsoft\Windows\WER");
                    string userWer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\Windows\\WER");
                    size += SafeGetDirectorySize(userWer);
                    size += SafeGetDirectorySize(@"C:\Windows\Minidump");
                    size += SafeGetFileSize(@"C:\Windows\MEMORY.DMP");
                    break;

                case "PSHistory":
                    string psHistory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\\Windows\\PowerShell\\PSReadLine\\ConsoleHost_history.txt");
                    size += SafeGetFileSize(psHistory);
                    break;

                case "Prefetch":
                    size += SafeGetDirectorySize(@"C:\Windows\Prefetch");
                    break;

                case "TempFiles":
                    size += SafeGetDirectorySize(@"C:\Windows\Temp");
                    size += SafeGetDirectorySize(Path.GetTempPath());
                    break;

                case "UpdateCache":
                    size += SafeGetDirectorySize(@"C:\Windows\SoftwareDistribution\Download");
                    size += SafeGetDirectorySize(@"C:\Windows\SoftwareDistribution\DeliveryOptimization");
                    size += SafeGetFileSize(@"C:\Windows\WindowsUpdate.log");
                    size += SafeGetFileSize(@"C:\Windows\SoftwareDistribution\ReportingEvents.log");
                    break;
            }
            return size;
        }

        private long SafeGetDirectorySize(string path)
        {
            if (!Directory.Exists(path)) return 0;
            long size = 0;
            try
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    try
                    {
                        size += new FileInfo(file).Length;
                    }
                    catch { }
                }
                foreach (string dir in Directory.GetDirectories(path))
                {
                    size += SafeGetDirectorySize(dir);
                }
            }
            catch { }
            return size;
        }

        private long SafeGetFileSize(string path)
        {
            if (!File.Exists(path)) return 0;
            try
            {
                return new FileInfo(path).Length;
            }
            catch { return 0; }
        }

        private long SafeGetDirectoryFilesSize(string directory, string searchPattern)
        {
            if (!Directory.Exists(directory)) return 0;
            long size = 0;
            try
            {
                foreach (var file in Directory.GetFiles(directory, searchPattern))
                {
                    try
                    {
                        size += new FileInfo(file).Length;
                    }
                    catch { }
                }
            }
            catch { }
            return size;
        }

        private async void BtnStartCleanup_Click(object sender, RoutedEventArgs e)
        {
            var selectedCategories = LogCategories.Where(c => c.IsSelected).ToList();
            if (selectedCategories.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một hạng mục để dọn dẹp.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnStartCleanup.IsEnabled = false;
            LstCategories.IsEnabled = false;
            TxtLog.Clear();

            AppendLog("=== BẮT ĐẦU TIẾN TRÌNH DỌN DẸP NHẬT KÝ HỆ THỐNG ===");
            AppendLog($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            AppendLog("--------------------------------------------------");

            ProgBar.Value = 0;
            ProgBar.IsIndeterminate = false;
            TxtPercentage.Text = "0%";

            var progressLog = new Progress<string>(msg => AppendLog(msg));
            var progressStatus = new Progress<string>(status => TxtStatus.Text = status);
            var progressPercentage = new Progress<int>(percent =>
            {
                ProgBar.Value = percent;
                TxtPercentage.Text = $"{percent}%";
            });

            long totalClearedBytes = 0;
            int totalDeletedFiles = 0;
            int totalSkippedFiles = 0;

            await Task.Run(() =>
            {
                int totalSteps = selectedCategories.Count;
                for (int i = 0; i < totalSteps; i++)
                {
                    var category = selectedCategories[i];
                    int currentStep = i + 1;

                    ((IProgress<string>)progressStatus).Report($"Đang xử lý: {category.Title}...");
                    ((IProgress<string>)progressLog).Report($"\n>>> [{currentStep}/{totalSteps}] {category.Title}...");

                    long clearedBytes = 0;
                    int deletedFiles = 0;
                    int skippedFiles = 0;

                    ExecuteCleanup(category.TargetId, progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);

                    totalClearedBytes += clearedBytes;
                    totalDeletedFiles += deletedFiles;
                    totalSkippedFiles += skippedFiles;

                    int percent = (int)((double)currentStep / totalSteps * 100);
                    ((IProgress<int>)progressPercentage).Report(percent);
                }
            });

            AppendLog("\n--------------------------------------------------");
            AppendLog("=== TIẾN TRÌNH DỌN DẸP HOÀN TẤT ===");
            AppendLog($"Tổng dung lượng giải phóng: {FormatSize(totalClearedBytes)}");
            AppendLog($"Số tệp đã xóa thành công: {totalDeletedFiles}");
            AppendLog($"Số tệp bị bỏ qua (đang bận/yêu cầu đặc biệt): {totalSkippedFiles}");
            AppendLog("==================================================");

            TxtStatus.Text = "Dọn dẹp hoàn tất!";

            
            await ScanAllSizesAsync();

            LstCategories.IsEnabled = true;
        }

        private void ExecuteCleanup(string targetId, Progress<string> progressLog, ref long clearedBytes, ref int deletedFiles, ref int skippedFiles)
        {
            IProgress<string> logger = progressLog;

            switch (targetId)
            {
                case "EventLogs":
                    try
                    {
                        var session = EventLogSession.GlobalSession;
                        var logNames = session.GetLogNames().ToList();
                        int clearedCount = 0;
                        int failedCount = 0;

                        foreach (var logName in logNames)
                        {
                            try
                            {
                                long logSize = 0;
                                try
                                {
                                    var info = session.GetLogInformation(logName, PathType.LogName);
                                    if (info.FileSize.HasValue) logSize = info.FileSize.Value;
                                }
                                catch { }

                                logger.Report($"[Xóa Event Log] {logName} ({FormatSize(logSize)})");
                                session.ClearLog(logName);
                                clearedBytes += logSize;
                                clearedCount++;
                                System.Threading.Thread.Sleep(5);
                            }
                            catch
                            {
                                failedCount++;
                            }
                        }
                        logger.Report($"\n>>> Dọn dẹp thành công {clearedCount} kênh Event Log. Bỏ qua {failedCount} kênh.");
                    }
                    catch (Exception ex)
                    {
                        logger.Report($"[Lỗi] Không thể truy cập hệ thống Event Logs: {ex.Message}");
                    }
                    break;

                case "SystemSetupLogs":
                    SafeDeleteDirectory(@"C:\Windows\Logs\CBS", progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    SafeDeleteDirectory(@"C:\Windows\Logs\DISM", progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    SafeDeleteDirectory(@"C:\Windows\Panther", progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    SafeDeleteDirectory(@"C:\Windows\System32\Sysprep\Panther", progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    SafeDeleteFile(@"C:\Windows\setupapi.log", progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    SafeDeleteDirectoryFiles(@"C:\Windows\inf", "setupapi*.log", progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    break;

                case "ErrorReports":
                    SafeDeleteDirectory(@"C:\ProgramData\Microsoft\Windows\WER", progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    string userWer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\Windows\\WER");
                    SafeDeleteDirectory(userWer, progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    SafeDeleteDirectory(@"C:\Windows\Minidump", progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    SafeDeleteFile(@"C:\Windows\MEMORY.DMP", progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    break;

                case "PSHistory":
                    string psHistory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\\Windows\\PowerShell\\PSReadLine\\ConsoleHost_history.txt");
                    SafeDeleteFile(psHistory, progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    break;

                case "Prefetch":
                    SafeDeleteDirectory(@"C:\Windows\Prefetch", progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    break;

                case "TempFiles":
                    SafeDeleteDirectory(@"C:\Windows\Temp", progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    SafeDeleteDirectory(Path.GetTempPath(), progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    break;

                case "UpdateCache":
                    SafeDeleteDirectory(@"C:\Windows\SoftwareDistribution\Download", progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    SafeDeleteDirectory(@"C:\Windows\SoftwareDistribution\DeliveryOptimization", progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    SafeDeleteFile(@"C:\Windows\WindowsUpdate.log", progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    SafeDeleteFile(@"C:\Windows\SoftwareDistribution\ReportingEvents.log", progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    break;
            }
        }

        private void SafeDeleteDirectory(string path, Progress<string> progressLog, ref long clearedBytes, ref int deletedFiles, ref int skippedFiles)
        {
            if (!Directory.Exists(path)) return;
            IProgress<string> logger = progressLog;

            try
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        long fileSize = fi.Length;
                        fi.Delete();
                        clearedBytes += fileSize;
                        deletedFiles++;
                        logger.Report($"[Đã xóa] {file} ({FormatSize(fileSize)})");
                    }
                    catch (Exception ex)
                    {
                        skippedFiles++;
                        logger.Report($"[Bỏ qua] {file} - File đang bận/Quyền truy cập ({ex.Message})");
                    }
                }

                foreach (string dir in Directory.GetDirectories(path))
                {
                    SafeDeleteDirectory(dir, progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                    try
                    {
                        if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
                        {
                            Directory.Delete(dir);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                logger.Report($"[Lỗi] Không thể truy cập thư mục {path}: {ex.Message}");
            }
        }

        private void SafeDeleteFile(string path, Progress<string> progressLog, ref long clearedBytes, ref int deletedFiles, ref int skippedFiles)
        {
            IProgress<string> logger = progressLog;
            if (!File.Exists(path)) return;
            try
            {
                var fi = new FileInfo(path);
                long fileSize = fi.Length;
                fi.Delete();
                clearedBytes += fileSize;
                deletedFiles++;
                logger.Report($"[Đã xóa] {path} ({FormatSize(fileSize)})");
            }
            catch (Exception ex)
            {
                skippedFiles++;
                logger.Report($"[Bỏ qua] {path} - File đang bận/Quyền truy cập ({ex.Message})");
            }
        }

        private void SafeDeleteDirectoryFiles(string directory, string searchPattern, Progress<string> progressLog, ref long clearedBytes, ref int deletedFiles, ref int skippedFiles)
        {
            IProgress<string> logger = progressLog;
            if (!Directory.Exists(directory)) return;
            try
            {
                foreach (var file in Directory.GetFiles(directory, searchPattern))
                {
                    SafeDeleteFile(file, progressLog, ref clearedBytes, ref deletedFiles, ref skippedFiles);
                }
            }
            catch (Exception ex)
            {
                logger.Report($"[Lỗi] Không thể duyệt tệp trong {directory} ({ex.Message})");
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

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var category in LogCategories)
            {
                category.IsSelected = true;
            }
            UpdateTotalSelectionSize();
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var category in LogCategories)
            {
                category.IsSelected = false;
            }
            UpdateTotalSelectionSize();
        }

        private async void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            await ScanAllSizesAsync();
        }
    }

    public class LogCategoryItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private string _sizeText = "Đang quét...";

        public string TargetId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public string SizeText
        {
            get => _sizeText;
            set
            {
                if (_sizeText != value)
                {
                    _sizeText = value;
                    OnPropertyChanged(nameof(SizeText));
                }
            }
        }

        public long SizeBytes { get; set; } = 0;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
