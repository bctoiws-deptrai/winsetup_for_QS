using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BctWinsetup.Views
{
    public class FontFolderItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;

        public string FolderPath { get; set; } = "";
        public int FontCount { get; set; } = 0;

        public string FontCountText => $"{FontCount} font" + (FontCount > 1 ? "s" : "");

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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class FontInstallPage : UserControl
    {
        private ObservableCollection<FontFolderItem> _folderList = new ObservableCollection<FontFolderItem>();
        private bool _isScanning = false;
        private bool _isInstalling = false;

        
        [DllImport("gdi32.dll", EntryPoint = "AddFontResourceW", SetLastError = true)]
        private static extern int AddFontResource([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_FONTCHANGE = 0x001D;
        private const int HWND_BROADCAST = 0xffff;

        public FontInstallPage()
        {
            InitializeComponent();
            LstFolders.ItemsSource = _folderList;
            Loaded += FontInstallPage_Loaded;
        }

        private async void FontInstallPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ScanDrivesAsync();
        }

        private async Task ScanDrivesAsync()
        {
            if (_isScanning || _isInstalling) return;

            try
            {
                _isScanning = true;
                SetUiState(false);
                _folderList.Clear();
                TxtFolderCount.Text = "Đang chuẩn bị quét các ổ đĩa...";
                ProgBar.IsIndeterminate = true;
                TxtStatus.Text = "Đang quét đĩa tìm thư mục Font...";

                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady && !d.Name.StartsWith("C:", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (drives.Count == 0)
                {
                    TxtFolderCount.Text = "Không phát hiện ổ đĩa nào khác ngoài ổ C.";
                    TxtStatus.Text = "Hoàn tất. Không tìm thấy ổ đĩa phù hợp.";
                    ProgBar.IsIndeterminate = false;
                    return;
                }

                await Task.Run(() =>
                {
                    foreach (var drive in drives)
                    {
                        ScanDir(drive.RootDirectory.FullName);
                    }
                });

                TxtFolderCount.Text = $"Tìm thấy {_folderList.Count} thư mục chứa font.";
                TxtStatus.Text = "Quét hoàn tất. Hãy chọn thư mục và bấm Bắt đầu cài Font.";
            }
            catch (Exception ex)
            {
                TxtFolderCount.Text = "Lỗi khi quét thư mục.";
                MessageBox.Show($"Lỗi quét đĩa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ProgBar.IsIndeterminate = false;
                _isScanning = false;
                SetUiState(true);
            }
        }

        private void ScanDir(string path)
        {
            try
            {
                string dirName = Path.GetFileName(path);
                if (string.IsNullOrEmpty(dirName))
                {
                    dirName = path; 
                }

                
                if (dirName.StartsWith("$") || 
                    dirName.Equals("System Volume Information", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals("Recovery", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals("MSOCache", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals("AppData", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals("Windows", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals("Program Files", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals("Program Files (x86)", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                
                if (dirName.Contains("font", StringComparison.OrdinalIgnoreCase))
                {
                    var fontFiles = Directory.GetFiles(path, "*.*")
                        .Where(f => f.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
                                    f.EndsWith(".otf", StringComparison.OrdinalIgnoreCase) ||
                                    f.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (fontFiles.Count > 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            _folderList.Add(new FontFolderItem
                            {
                                FolderPath = path,
                                FontCount = fontFiles.Count,
                                IsSelected = true
                            });
                            TxtFolderCount.Text = $"Đang quét... Tìm thấy {_folderList.Count} thư mục.";
                        });
                    }
                }

                
                foreach (var subDir in Directory.GetDirectories(path))
                {
                    ScanDir(subDir);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
            catch (PathTooLongException) { }
            catch (Exception) { }
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (_isScanning || _isInstalling) return;
            foreach (var item in _folderList)
            {
                item.IsSelected = true;
            }
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            if (_isScanning || _isInstalling) return;
            foreach (var item in _folderList)
            {
                item.IsSelected = false;
            }
        }

        private async void BtnRescan_Click(object sender, RoutedEventArgs e)
        {
            await ScanDrivesAsync();
        }

        private async void BtnStartInstall_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling || _isScanning) return;

            var selectedFolders = _folderList.Where(f => f.IsSelected).ToList();
            if (selectedFolders.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một thư mục chứa font để cài đặt.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _isInstalling = true;
                SetUiState(false);
                ProgBar.Value = 0;
                TxtPercentage.Text = "0%";
                TxtLog.Text = $"[Bắt đầu cài đặt Font lúc {DateTime.Now:HH:mm:ss}]\n";
                TxtLog.AppendText("--------------------------------------------------\n");

                
                var fontFilesToInstall = new List<string>();
                foreach (var folder in selectedFolders)
                {
                    try
                    {
                        var files = Directory.GetFiles(folder.FolderPath, "*.*")
                            .Where(f => f.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
                                        f.EndsWith(".otf", StringComparison.OrdinalIgnoreCase) ||
                                        f.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase));
                        fontFilesToInstall.AddRange(files);
                    }
                    catch (Exception ex)
                    {
                        TxtLog.AppendText($"[Lỗi] Không thể truy cập thư mục: {folder.FolderPath}. Chi tiết: {ex.Message}\n");
                    }
                }

                if (fontFilesToInstall.Count == 0)
                {
                    TxtLog.AppendText("Không tìm thấy tệp font hợp lệ nào (.ttf, .otf, .ttc) trong các thư mục được chọn.\n");
                    TxtStatus.Text = "Cài đặt kết thúc. Không có font nào được cài.";
                    return;
                }

                int total = fontFilesToInstall.Count;
                int installedCount = 0;
                int errorCount = 0;
                int skippedCount = 0;

                string winFontsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");

                await Task.Run(() =>
                {
                    for (int i = 0; i < total; i++)
                    {
                        string fontPath = fontFilesToInstall[i];
                        string fileName = Path.GetFileName(fontPath);
                        string destPath = Path.Combine(winFontsDir, fileName);

                        int currentNum = i + 1;
                        Dispatcher.Invoke(() =>
                        {
                            TxtStatus.Text = $"Đang cài đặt font: {fileName} ({currentNum}/{total})...";
                            double progress = ((double)i / total) * 100;
                            ProgBar.Value = progress;
                            TxtPercentage.Text = $"{Math.Round(progress)}%";
                            TxtLog.AppendText($"[{currentNum}/{total}] Đang xử lý: {fileName}...\n");
                            TxtLog.ScrollToEnd();
                        });

                        bool isSameFile = false;
                        if (File.Exists(destPath))
                        {
                            try
                            {
                                FileInfo srcInfo = new FileInfo(fontPath);
                                FileInfo destInfo = new FileInfo(destPath);
                                if (srcInfo.Length == destInfo.Length)
                                {
                                    isSameFile = true;
                                }
                            }
                            catch { }
                        }

                        bool copySuccess = false;
                        if (isSameFile)
                        {
                            copySuccess = true;
                            skippedCount++;
                            Dispatcher.Invoke(() =>
                            {
                                TxtLog.AppendText($"-> Font đã tồn tại trong hệ thống (Bỏ qua việc sao chép).\n");
                            });
                        }
                        else
                        {
                            try
                            {
                                File.Copy(fontPath, destPath, true);
                                copySuccess = true;
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                Dispatcher.Invoke(() =>
                                {
                                    TxtLog.AppendText($"-> [Lỗi sao chép] {ex.Message}\n");
                                });
                            }
                        }

                        if (copySuccess)
                        {
                            try
                            {
                                string fontName = GetFontName(fontPath);
                                string extension = Path.GetExtension(fontPath).ToLower();
                                string registryName = fontName;

                                if (extension == ".otf")
                                {
                                    if (!registryName.EndsWith(" (OpenType)", StringComparison.OrdinalIgnoreCase))
                                        registryName += " (OpenType)";
                                }
                                else
                                {
                                    if (!registryName.EndsWith(" (TrueType)", StringComparison.OrdinalIgnoreCase))
                                        registryName += " (TrueType)";
                                }

                                
                                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts", true))
                                {
                                    if (key != null)
                                    {
                                        key.SetValue(registryName, fileName);
                                    }
                                }

                                
                                AddFontResource(destPath);
                                installedCount++;
                                Dispatcher.Invoke(() =>
                                {
                                    TxtLog.AppendText($"-> Đăng ký thành công: {registryName}\n");
                                });
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                Dispatcher.Invoke(() =>
                                {
                                    TxtLog.AppendText($"-> [Lỗi đăng ký Registry] {ex.Message}\n");
                                });
                            }
                        }
                        
                        Dispatcher.Invoke(() => TxtLog.ScrollToEnd());
                    }

                    
                    SendMessage((IntPtr)HWND_BROADCAST, WM_FONTCHANGE, IntPtr.Zero, IntPtr.Zero);
                });

                ProgBar.Value = 100;
                TxtPercentage.Text = "100%";
                TxtStatus.Text = $"Cài đặt hoàn tất! Thành công: {installedCount}, Bỏ qua: {skippedCount}, Lỗi: {errorCount}";
                TxtLog.AppendText("--------------------------------------------------\n");
                TxtLog.AppendText($"[Hoàn thành lúc {DateTime.Now:HH:mm:ss}] Đã hoàn tất cài đặt font chữ.\n");
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

        private string GetFontName(string fontPath)
        {
            try
            {
                var uri = new Uri(fontPath);
                var glyphTypeface = new System.Windows.Media.GlyphTypeface(uri);

                if (glyphTypeface.FamilyNames.TryGetValue(System.Globalization.CultureInfo.GetCultureInfo("en-US"), out string? name) && name != null)
                {
                    string style = "";
                    if (glyphTypeface.FaceNames.TryGetValue(System.Globalization.CultureInfo.GetCultureInfo("en-US"), out string? faceName) && faceName != null)
                    {
                        if (faceName != "Regular" && faceName != "Normal")
                        {
                            style = " " + faceName;
                        }
                    }
                    return name + style;
                }

                if (glyphTypeface.FamilyNames.Count > 0)
                {
                    return glyphTypeface.FamilyNames.Values.First();
                }
            }
            catch { }

            return Path.GetFileNameWithoutExtension(fontPath);
        }

        private void SetUiState(bool enabled)
        {
            BtnStartInstall.IsEnabled = enabled;
            LstFolders.IsEnabled = enabled;
        }
    }
}
