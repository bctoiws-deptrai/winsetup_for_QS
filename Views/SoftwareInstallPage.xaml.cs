using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BctWinsetup.Views
{
    public class SoftwareItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private string _status = "Chờ cài đặt...";

        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string FileType { get; set; } = ""; 

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

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(StatusColor));
                }
            }
        }

        public Brush StatusColor
        {
            get
            {
                if (Status == "Đang cài đặt...") return Brushes.LightSkyBlue;
                if (Status == "Hoàn thành") return Brushes.LightGreen;
                if (Status.StartsWith("Thất bại")) return Brushes.Tomato;
                return Brushes.Gray;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class SoftwareInstallPage : UserControl
    {
        private ObservableCollection<SoftwareItem> _softwareList = new ObservableCollection<SoftwareItem>();
        private bool _isInstalling = false;

        public SoftwareInstallPage()
        {
            InitializeComponent();
            LstSoftwares.ItemsSource = _softwareList;
            Loaded += SoftwareInstallPage_Loaded;
        }

        private void SoftwareInstallPage_Loaded(object sender, RoutedEventArgs e)
        {
            ScanDirectory();
        }

        private void ScanDirectory()
        {
            try
            {
                _softwareList.Clear();
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                string myExeName = Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName ?? "BctWinsetup.exe");

                
                var files = Directory.GetFiles(currentDir, "*.*")
                    .Where(f => f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || 
                                f.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f)
                    .ToList();

                int addedCount = 0;
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    
                    
                    if (fileName.Equals(myExeName, StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals("BctWinsetup.dll", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals("apphost.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    _softwareList.Add(new SoftwareItem
                    {
                        FileName = fileName,
                        FilePath = file,
                        FileType = Path.GetExtension(file).ToUpper().Replace(".", ""),
                        IsSelected = true,
                        Status = "Chờ cài đặt..."
                    });
                    addedCount++;
                }

                TxtAppCount.Text = $"Tìm thấy {addedCount} phần mềm cài đặt cùng thư mục.";
            }
            catch (Exception ex)
            {
                TxtAppCount.Text = "Lỗi khi quét thư mục bộ cài.";
                MessageBox.Show($"Lỗi quét thư mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling) return;
            foreach (var item in _softwareList)
            {
                item.IsSelected = true;
            }
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling) return;
            foreach (var item in _softwareList)
            {
                item.IsSelected = false;
            }
        }

        private void BtnRescan_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling) return;
            ScanDirectory();
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{AppDomain.CurrentDomain.BaseDirectory}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở thư mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnStartInstall_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling) return;

            var selectedItems = _softwareList.Where(i => i.IsSelected).ToList();
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một phần mềm để cài đặt.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _isInstalling = true;
                SetUiState(false);
                TxtLog.Text = $"[Bắt đầu cài đặt hàng loạt lúc {DateTime.Now:HH:mm:ss}]\n";
                TxtLog.AppendText("--------------------------------------------------\n");

                int total = selectedItems.Count;
                int current = 0;

                foreach (var item in selectedItems)
                {
                    current++;
                    item.Status = "Đang cài đặt...";
                    TxtStatus.Text = $"Đang cài đặt: {item.FileName} ({current}/{total})...";
                    
                    double progressValue = ((double)(current - 1) / total) * 100;
                    ProgBar.Value = progressValue;
                    TxtPercentage.Text = $"{Math.Round(progressValue)}%";

                    TxtLog.AppendText($"[{current}/{total}] Đang chạy {item.FileName}...\n");
                    TxtLog.ScrollToEnd();

                    bool success = await Task.Run(() => RunInstaller(item));

                    if (success)
                    {
                        item.Status = "Hoàn thành";
                        TxtLog.AppendText($"[Thành công] {item.FileName} đã hoàn thành.\n");
                    }
                    else
                    {
                        item.Status = "Thất bại";
                        TxtLog.AppendText($"[Thất bại] {item.FileName} có lỗi hoặc bị hủy.\n");
                    }
                    TxtLog.AppendText("--------------------------------------------------\n");
                    TxtLog.ScrollToEnd();
                }

                ProgBar.Value = 100;
                TxtPercentage.Text = "100%";
                TxtStatus.Text = "Cài đặt hàng loạt hoàn tất!";
                TxtLog.AppendText($"[Hoàn thành lúc {DateTime.Now:HH:mm:ss}] Quá trình cài đặt kết thúc.");
                TxtLog.ScrollToEnd();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi trong quá trình cài đặt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isInstalling = false;
                SetUiState(true);
            }
        }

        private bool RunInstaller(SoftwareItem item)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = item.FilePath,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(item.FilePath)
                };

                
                
                if (item.FileType.Equals("MSI", StringComparison.OrdinalIgnoreCase))
                {
                    startInfo.FileName = "msiexec.exe";
                    startInfo.Arguments = $"/i \"{item.FilePath}\" /passive";
                }

                using (Process? process = Process.Start(startInfo))
                {
                    if (process == null) return false;
                    process.WaitForExit();
                    return process.ExitCode == 0 || process.ExitCode == 3010; 
                }
            }
            catch
            {
                return false;
            }
        }

        private void SetUiState(bool enabled)
        {
            BtnStartInstall.IsEnabled = enabled;
            BtnOpenFolder.IsEnabled = enabled;
            LstSoftwares.IsEnabled = enabled;
        }
    }
}
