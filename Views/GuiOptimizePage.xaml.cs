using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BctWinsetup.Views
{
    public partial class GuiOptimizePage : UserControl
    {
        public GuiOptimizePage()
        {
            InitializeComponent();
            Loaded += GuiOptimizePage_Loaded;
        }

        private void GuiOptimizePage_Loaded(object sender, RoutedEventArgs e)
        {
            ReadCurrentSettings();
        }

        private void ReadCurrentSettings()
        {
            try
            {
                
                object? fFlagsVal = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Bags\1\Desktop", "FFlags", null);
                if (fFlagsVal is int fFlags)
                {
                    
                    ChkAutoArrange.IsChecked = (fFlags & 0x1) == 0x1;
                }
                else
                {
                    ChkAutoArrange.IsChecked = false;
                }

                
                object? launchToVal = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", null);
                if (launchToVal is int launchTo)
                {
                    ChkLaunchTo.IsChecked = launchTo == 1;
                }
                else
                {
                    ChkLaunchTo.IsChecked = false;
                }

                
                object? hiddenVal = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", null);
                if (hiddenVal is int hidden)
                {
                    ChkShowHidden.IsChecked = hidden == 1;
                }
                else
                {
                    ChkShowHidden.IsChecked = false;
                }

                
                object? hideFileExtVal = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", null);
                if (hideFileExtVal is int hideFileExt)
                {
                    ChkShowExtensions.IsChecked = hideFileExt == 0;
                }
                else
                {
                    ChkShowExtensions.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[Cảnh báo] Lỗi khi đọc cấu hình hiện tại từ Registry: {ex.Message}");
            }
        }

        private async void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            BtnApply.IsEnabled = false;
            TxtLog.Clear();
            AppendLog("=== BẮT ĐẦU ÁP DỤNG TỐI ƯU GIAO DIỆN ===");
            AppendLog($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            AppendLog("--------------------------------------------------");

            bool autoArrange = ChkAutoArrange.IsChecked == true;
            bool launchToThisPc = ChkLaunchTo.IsChecked == true;
            bool showHidden = ChkShowHidden.IsChecked == true;
            bool showExtensions = ChkShowExtensions.IsChecked == true;
            bool restartExplorer = ChkRestartExplorer.IsChecked == true;

            ProgBar.Value = 0;
            ProgBar.IsIndeterminate = false;
            TxtPercentage.Text = "0%";
            TxtStatus.Text = "Đang áp dụng...";

            var progressLog = new Progress<string>(msg => AppendLog(msg));
            var progressStatus = new Progress<string>(status => TxtStatus.Text = status);
            var progressPercent = new Progress<int>(val =>
            {
                ProgBar.Value = val;
                TxtPercentage.Text = $"{val}%";
            });

            await Task.Run(() =>
            {
                int totalSteps = 4 + (restartExplorer ? 1 : 0);
                int currentStep = 0;

                
                currentStep++;
                ((IProgress<string>)progressStatus).Report("Cấu hình biểu tượng Desktop...");
                ((IProgress<int>)progressPercent).Report((int)((double)currentStep / totalSteps * 100));
                try
                {
                    int newVal = autoArrange ? 1075839525 : 1075839520;
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Bags\1\Desktop", "FFlags", newVal, RegistryValueKind.DWord);
                    ((IProgress<string>)progressLog).Report($"[Thành công] Thiết lập Auto Arrange Desktop = {autoArrange}");
                }
                catch (Exception ex)
                {
                    ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể cấu hình Auto Arrange Desktop: {ex.Message}");
                }

                
                currentStep++;
                ((IProgress<string>)progressStatus).Report("Cấu hình File Explorer...");
                ((IProgress<int>)progressPercent).Report((int)((double)currentStep / totalSteps * 100));
                try
                {
                    int newVal = launchToThisPc ? 1 : 2;
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", newVal, RegistryValueKind.DWord);
                    ((IProgress<string>)progressLog).Report($"[Thành công] Thiết lập mở File Explorer mặc định vào: {(launchToThisPc ? "This PC" : "Home/Quick Access")}");
                }
                catch (Exception ex)
                {
                    ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể thiết lập mặc định File Explorer: {ex.Message}");
                }

                
                currentStep++;
                ((IProgress<string>)progressStatus).Report("Cấu hình hiển thị tệp ẩn...");
                ((IProgress<int>)progressPercent).Report((int)((double)currentStep / totalSteps * 100));
                try
                {
                    int newVal = showHidden ? 1 : 2;
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", newVal, RegistryValueKind.DWord);
                    
                    int superHiddenVal = showHidden ? 1 : 2;
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSuperHidden", superHiddenVal, RegistryValueKind.DWord);
                    
                    ((IProgress<string>)progressLog).Report($"[Thành công] Thiết lập hiển thị tệp ẩn = {showHidden}");
                }
                catch (Exception ex)
                {
                    ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể cấu hình tệp ẩn: {ex.Message}");
                }

                
                currentStep++;
                ((IProgress<string>)progressStatus).Report("Cấu hình hiển thị đuôi file...");
                ((IProgress<int>)progressPercent).Report((int)((double)currentStep / totalSteps * 100));
                try
                {
                    int newVal = showExtensions ? 0 : 1;
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", newVal, RegistryValueKind.DWord);
                    ((IProgress<string>)progressLog).Report($"[Thành công] Thiết lập hiển thị đuôi file (Extensions) = {showExtensions}");
                }
                catch (Exception ex)
                {
                    ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể cấu hình đuôi file: {ex.Message}");
                }

                
                if (restartExplorer)
                {
                    currentStep++;
                    ((IProgress<string>)progressStatus).Report("Đang khởi động lại Windows Explorer...");
                    ((IProgress<int>)progressPercent).Report((int)((double)currentStep / totalSteps * 100));
                    try
                    {
                        ((IProgress<string>)progressLog).Report("[Đang xử lý] Đang tắt các tiến trình explorer.exe...");
                        foreach (var process in Process.GetProcessesByName("explorer"))
                        {
                            try
                            {
                                process.Kill();
                                process.WaitForExit(3000);
                            }
                            catch { }
                        }
                        ((IProgress<string>)progressLog).Report("[Thành công] Đã khởi động lại Windows Explorer.");
                    }
                    catch (Exception ex)
                    {
                        ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể khởi động lại Windows Explorer: {ex.Message}");
                    }
                }
            });

            AppendLog("\n--------------------------------------------------");
            AppendLog("=== TỐI ƯU GIAO DIỆN HOÀN TẤT ===");
            AppendLog("==================================================");

            TxtStatus.Text = "Hoàn tất tối ưu!";
            BtnApply.IsEnabled = true;

            
            ReadCurrentSettings();
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

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            ChkAutoArrange.IsChecked = true;
            ChkLaunchTo.IsChecked = true;
            ChkShowHidden.IsChecked = true;
            ChkShowExtensions.IsChecked = true;
            ChkRestartExplorer.IsChecked = true;
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            ChkAutoArrange.IsChecked = false;
            ChkLaunchTo.IsChecked = false;
            ChkShowHidden.IsChecked = false;
            ChkShowExtensions.IsChecked = false;
            ChkRestartExplorer.IsChecked = false;
        }

        private async void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            BtnReset.IsEnabled = false;
            BtnApply.IsEnabled = false;
            TxtLog.Clear();
            AppendLog("=== BẮT ĐẦU KHÔI PHỤC GIAO DIỆN MẶC ĐỊNH ===");
            AppendLog($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            AppendLog("--------------------------------------------------");

            bool restartExplorer = ChkRestartExplorer.IsChecked == true;

            ProgBar.Value = 0;
            ProgBar.IsIndeterminate = false;
            TxtPercentage.Text = "0%";
            TxtStatus.Text = "Đang khôi phục mặc định...";

            var progressLog = new Progress<string>(msg => AppendLog(msg));
            var progressStatus = new Progress<string>(status => TxtStatus.Text = status);
            var progressPercent = new Progress<int>(val =>
            {
                ProgBar.Value = val;
                TxtPercentage.Text = $"{val}%";
            });

            await Task.Run(() =>
            {
                int totalSteps = 4 + (restartExplorer ? 1 : 0);
                int currentStep = 0;

                
                currentStep++;
                ((IProgress<string>)progressStatus).Report("Khôi phục biểu tượng Desktop...");
                ((IProgress<int>)progressPercent).Report((int)((double)currentStep / totalSteps * 100));
                try
                {
                    int defaultVal = 1075839520;
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Bags\1\Desktop", "FFlags", defaultVal, RegistryValueKind.DWord);
                    ((IProgress<string>)progressLog).Report("[Mặc định] Thiết lập Auto Arrange Desktop = Tắt");
                }
                catch (Exception ex)
                {
                    ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể cấu hình Auto Arrange Desktop: {ex.Message}");
                }

                
                currentStep++;
                ((IProgress<string>)progressStatus).Report("Khôi phục File Explorer...");
                ((IProgress<int>)progressPercent).Report((int)((double)currentStep / totalSteps * 100));
                try
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", 2, RegistryValueKind.DWord);
                    ((IProgress<string>)progressLog).Report("[Mặc định] Thiết lập mở File Explorer mặc định vào: Quick Access / Home");
                }
                catch (Exception ex)
                {
                    ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể thiết lập mặc định File Explorer: {ex.Message}");
                }

                
                currentStep++;
                ((IProgress<string>)progressStatus).Report("Khôi phục hiển thị tệp ẩn...");
                ((IProgress<int>)progressPercent).Report((int)((double)currentStep / totalSteps * 100));
                try
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", 2, RegistryValueKind.DWord);
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSuperHidden", 2, RegistryValueKind.DWord);
                    ((IProgress<string>)progressLog).Report("[Mặc định] Thiết lập hiển thị tệp ẩn = Ẩn tệp ẩn và tệp hệ thống");
                }
                catch (Exception ex)
                {
                    ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể cấu hình tệp ẩn: {ex.Message}");
                }

                
                currentStep++;
                ((IProgress<string>)progressStatus).Report("Khôi phục hiển thị đuôi file...");
                ((IProgress<int>)progressPercent).Report((int)((double)currentStep / totalSteps * 100));
                try
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 1, RegistryValueKind.DWord);
                    ((IProgress<string>)progressLog).Report("[Mặc định] Thiết lập hiển thị đuôi file (Extensions) = Ẩn");
                }
                catch (Exception ex)
                {
                    ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể cấu hình đuôi file: {ex.Message}");
                }

                
                if (restartExplorer)
                {
                    currentStep++;
                    ((IProgress<string>)progressStatus).Report("Đang khởi động lại Windows Explorer...");
                    ((IProgress<int>)progressPercent).Report((int)((double)currentStep / totalSteps * 100));
                    try
                    {
                        foreach (var process in Process.GetProcessesByName("explorer"))
                        {
                            try
                            {
                                process.Kill();
                                process.WaitForExit(3000);
                            }
                            catch { }
                        }
                        ((IProgress<string>)progressLog).Report("[Thành công] Đã khởi động lại Windows Explorer.");
                    }
                    catch (Exception ex)
                    {
                        ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể khởi động lại Windows Explorer: {ex.Message}");
                    }
                }
            });

            AppendLog("\n--------------------------------------------------");
            AppendLog("=== KHÔI PHỤC GIAO DIỆN MẶC ĐỊNH HOÀN TẤT ===");
            AppendLog("==================================================");

            TxtStatus.Text = "Khôi phục mặc định hoàn tất!";
            BtnReset.IsEnabled = true;
            BtnApply.IsEnabled = true;

            ReadCurrentSettings();
        }
    }
}
