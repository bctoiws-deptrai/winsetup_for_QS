using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace BctWinsetup.Views
{
    public partial class SystemOptimizePage : UserControl
    {
        public SystemOptimizePage()
        {
            InitializeComponent();
            Loaded += SystemOptimizePage_Loaded;
        }

        private async void SystemOptimizePage_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshStatusesAsync();
        }

        private async Task RefreshStatusesAsync()
        {
            TxtUpdateStatus.Text = "Đang quét...";
            TxtUpdateStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)); 

            TxtDefenderStatus.Text = "Đang quét...";
            TxtDefenderStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)); 

            string wuStatus = string.Empty;
            string defenderStatus = string.Empty;

            await Task.Run(() =>
            {
                wuStatus = GetServiceStatusSummary();
                defenderStatus = GetDefenderStatusSummary();
            });

            TxtUpdateStatus.Text = wuStatus;
            if (wuStatus.Contains("Hoạt động") || wuStatus.Contains("Chạy"))
            {
                TxtUpdateStatus.Foreground = new SolidColorBrush(Color.FromRgb(16, 124, 65)); 
            }
            else if (wuStatus.Contains("Đã tắt") || wuStatus.Contains("Disabled"))
            {
                TxtUpdateStatus.Foreground = new SolidColorBrush(Color.FromRgb(168, 0, 0)); 
            }
            else
            {
                TxtUpdateStatus.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)); 
            }

            TxtDefenderStatus.Text = defenderStatus;
            if (defenderStatus.Contains("Bảo vệ") || defenderStatus.Contains("Bật"))
            {
                TxtDefenderStatus.Foreground = new SolidColorBrush(Color.FromRgb(16, 124, 65)); 
            }
            else if (defenderStatus.Contains("Đã tắt") || defenderStatus.Contains("Disabled"))
            {
                TxtDefenderStatus.Foreground = new SolidColorBrush(Color.FromRgb(168, 0, 0)); 
            }
            else
            {
                TxtDefenderStatus.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)); 
            }
        }

        private string GetServiceStatusSummary()
        {
            string wuState = GetServiceState("wuauserv");
            string usoState = GetServiceState("UsoSvc");
            return $"WU: {wuState}\nOrchestrator: {usoState}";
        }

        private string GetServiceState(string serviceName)
        {
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = "sc.exe";
                process.StartInfo.Arguments = $"query {serviceName}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                string runningState = "Không rõ";
                if (output.Contains("RUNNING"))
                {
                    runningState = "Chạy";
                }
                else if (output.Contains("STOPPED"))
                {
                    runningState = "Dừng";
                }

                object? startVal = Registry.GetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{serviceName}", "Start", null);
                if (startVal is int startType)
                {
                    string typeStr = startType switch
                    {
                        2 => "Auto",
                        3 => "Manual",
                        4 => "Disabled",
                        _ => $"Type {startType}"
                    };
                    return $"{runningState} ({typeStr})";
                }

                return runningState;
            }
            catch (Exception ex)
            {
                return $"Lỗi ({ex.Message})";
            }
        }

        private string GetDefenderStatusSummary()
        {
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = "-NoProfile -Command \"(Get-MpPreference -ErrorAction SilentlyContinue).DisableRealtimeMonitoring\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                if (string.IsNullOrEmpty(output))
                {
                    
                    
                    string winDefendState = GetServiceState("WinDefend");
                    if (winDefendState.Contains("Disabled")) return "Đã tắt (WinDefend Disabled)";
                    return "Bảo vệ (Mặc định)";
                }

                if (output.Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    return "Đã tắt (Disabled)";
                }
                if (output.Equals("False", StringComparison.OrdinalIgnoreCase))
                {
                    return "Đang bảo vệ (Running)";
                }

                return "Không rõ";
            }
            catch (Exception ex)
            {
                return $"Lỗi ({ex.Message})";
            }
        }

        private async void BtnApplyTweaks_Click(object sender, RoutedEventArgs e)
        {
            BtnApplyTweaks.IsEnabled = false;
            TxtLog.Clear();
            AppendLog("=== BẮT ĐẦU ÁP DỤNG TỐI ƯU HIỆU NĂNG HỆ THỐNG ===");
            AppendLog($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            AppendLog("--------------------------------------------------");

            bool ultimatePerf = ChkUltimatePerf.IsChecked == true;
            bool disableCortana = ChkDisableCortana.IsChecked == true;
            bool disableTelemetry = ChkDisableTelemetry.IsChecked == true;
            bool disableBgApps = ChkDisableBackgroundApps.IsChecked == true;

            ProgBar.Value = 0;
            ProgBar.IsIndeterminate = false;
            TxtPercentage.Text = "0%";
            TxtStatus.Text = "Đang áp dụng tối ưu...";

            var progressLog = new Progress<string>(msg => AppendLog(msg));
            var progressStatus = new Progress<string>(status => TxtStatus.Text = status);
            var progressPercent = new Progress<int>(val =>
            {
                ProgBar.Value = val;
                TxtPercentage.Text = $"{val}%";
            });

            await Task.Run(() =>
            {
                int totalSteps = 4;
                int currentStep = 0;

                
                currentStep++;
                ((IProgress<string>)progressStatus).Report("Cấu hình Ultimate Performance...");
                ((IProgress<int>)progressPercent).Report((int)((double)currentStep / totalSteps * 100));

                if (ultimatePerf)
                {
                    try
                    {
                        using var process = new Process();
                        process.StartInfo.FileName = "powercfg.exe";
                        process.StartInfo.Arguments = "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.CreateNoWindow = true;
                        process.Start();
                        string output = process.StandardOutput.ReadToEnd().Trim();
                        process.WaitForExit();

                        using var procActive = new Process();
                        procActive.StartInfo.FileName = "powercfg.exe";
                        procActive.StartInfo.Arguments = "-setactive e9a42b02-d5df-448d-aa00-03f14749eb61";
                        procActive.StartInfo.UseShellExecute = false;
                        procActive.StartInfo.CreateNoWindow = true;
                        procActive.Start();
                        procActive.WaitForExit();

                        ((IProgress<string>)progressLog).Report("[Thành công] Đã kích hoạt Ultimate Performance Power Scheme.");
                    }
                    catch (Exception ex)
                    {
                        ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể kích hoạt Ultimate Performance: {ex.Message}");
                    }
                }
                else
                {
                    ((IProgress<string>)progressLog).Report("[Bỏ qua] Kích hoạt Ultimate Performance.");
                }

                
                currentStep++;
                ((IProgress<string>)progressStatus).Report("Cấu hình Cortana & Bing Search...");
                ((IProgress<int>)progressPercent).Report((int)((double)currentStep / totalSteps * 100));

                if (disableCortana)
                {
                    try
                    {
                        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search"))
                        {
                            key.SetValue("BingSearchEnabled", 0, RegistryValueKind.DWord);
                            key.SetValue("CortanaConsent", 0, RegistryValueKind.DWord);
                        }
                        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer"))
                        {
                            key.SetValue("DisableSearchBoxSuggestions", 1, RegistryValueKind.DWord);
                        }
                        ((IProgress<string>)progressLog).Report("[Thành công] Đã tắt Cortana và Bing Search Suggestions trong Start Menu.");
                    }
                    catch (Exception ex)
                    {
                        ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể tắt Cortana/Bing Search: {ex.Message}");
                    }
                }
                else
                {
                    ((IProgress<string>)progressLog).Report("[Bỏ qua] Cấu hình Cortana & Bing Search.");
                }

                
                currentStep++;
                ((IProgress<string>)progressStatus).Report("Cấu hình Telemetry...");
                ((IProgress<int>)progressPercent).Report((int)((double)currentStep / totalSteps * 100));

                if (disableTelemetry)
                {
                    try
                    {
                        using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection"))
                        {
                            key.SetValue("AllowTelemetry", 0, RegistryValueKind.DWord);
                        }
                        using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection"))
                        {
                            key.SetValue("AllowTelemetry", 0, RegistryValueKind.DWord);
                        }

                        
                        RunCommand("sc.exe", "config DiagTrack start= disabled");
                        RunCommand("sc.exe", "stop DiagTrack");

                        ((IProgress<string>)progressLog).Report("[Thành công] Đã tắt Telemetry và dừng dịch vụ DiagTrack (Connected User Experiences).");
                    }
                    catch (Exception ex)
                    {
                        ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể tắt Telemetry: {ex.Message}");
                    }
                }
                else
                {
                    ((IProgress<string>)progressLog).Report("[Bỏ qua] Cấu hình Telemetry.");
                }

                
                currentStep++;
                ((IProgress<string>)progressStatus).Report("Cấu hình Background Apps...");
                ((IProgress<int>)progressPercent).Report((int)((double)currentStep / totalSteps * 100));

                if (disableBgApps)
                {
                    try
                    {
                        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications"))
                        {
                            key.SetValue("GlobalUserDisabled", 1, RegistryValueKind.DWord);
                        }
                        using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy"))
                        {
                            key.SetValue("LetAppsRunInBackground", 2, RegistryValueKind.DWord);
                        }
                        ((IProgress<string>)progressLog).Report("[Thành công] Đã tắt toàn bộ ứng dụng chạy ngầm (Background Apps).");
                    }
                    catch (Exception ex)
                    {
                        ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể cấu hình ứng dụng chạy ngầm: {ex.Message}");
                    }
                }
                else
                {
                    ((IProgress<string>)progressLog).Report("[Bỏ qua] Cấu hình ứng dụng chạy ngầm.");
                }
            });

            AppendLog("\n--------------------------------------------------");
            AppendLog("=== HOÀN TẤT TỐI ƯU HIỆU NĂNG ===");
            AppendLog("==================================================");

            TxtStatus.Text = "Tối ưu hiệu năng hoàn tất!";
            BtnApplyTweaks.IsEnabled = true;
        }

        private async void BtnEnableUpdate_Click(object sender, RoutedEventArgs e)
        {
            SetUpdateControls(false);
            TxtLog.Clear();
            AppendLog("=== BẮT ĐẦU BẬT WINDOWS UPDATE ===");
            
            ProgBar.IsIndeterminate = true;
            TxtStatus.Text = "Đang kích hoạt Windows Update...";

            await Task.Run(() =>
            {
                try
                {
                    AppendLog("1. Cấu hình dịch vụ Windows Update (wuauserv) sang Manual...");
                    RunCommand("sc.exe", "config wuauserv start= demand");
                    AppendLog("Khởi động dịch vụ Windows Update...");
                    RunCommand("sc.exe", "start wuauserv");

                    AppendLog("2. Cấu hình dịch vụ Update Orchestrator (UsoSvc) sang Automatic...");
                    RunCommand("sc.exe", "config UsoSvc start= auto");
                    AppendLog("Khởi động dịch vụ Update Orchestrator...");
                    RunCommand("sc.exe", "start UsoSvc");

                    AppendLog("[Thành công] Windows Update đã được kích hoạt.");
                }
                catch (Exception ex)
                {
                    AppendLog($"[Lỗi] Không thể kích hoạt Windows Update: {ex.Message}");
                }
            });

            ProgBar.IsIndeterminate = false;
            ProgBar.Value = 100;
            TxtPercentage.Text = "100%";
            TxtStatus.Text = "Đã bật Windows Update!";
            
            await RefreshStatusesAsync();
            SetUpdateControls(true);
        }

        private async void BtnDisableUpdate_Click(object sender, RoutedEventArgs e)
        {
            SetUpdateControls(false);
            TxtLog.Clear();
            AppendLog("=== BẮT ĐẦU TẮT WINDOWS UPDATE ===");
            
            ProgBar.IsIndeterminate = true;
            TxtStatus.Text = "Đang tắt Windows Update...";

            await Task.Run(() =>
            {
                try
                {
                    AppendLog("1. Dừng và vô hiệu hóa dịch vụ Windows Update (wuauserv)...");
                    RunCommand("sc.exe", "stop wuauserv");
                    RunCommand("sc.exe", "config wuauserv start= disabled");

                    AppendLog("2. Dừng và vô hiệu hóa dịch vụ Update Orchestrator (UsoSvc)...");
                    RunCommand("sc.exe", "stop UsoSvc");
                    RunCommand("sc.exe", "config UsoSvc start= disabled");

                    AppendLog("[Thành công] Windows Update đã bị vô hiệu hóa.");
                }
                catch (Exception ex)
                {
                    AppendLog($"[Lỗi] Không thể vô hiệu hóa Windows Update: {ex.Message}");
                }
            });

            ProgBar.IsIndeterminate = false;
            ProgBar.Value = 100;
            TxtPercentage.Text = "100%";
            TxtStatus.Text = "Đã tắt Windows Update!";

            await RefreshStatusesAsync();
            SetUpdateControls(true);
        }

        private async void BtnEnableDefender_Click(object sender, RoutedEventArgs e)
        {
            SetDefenderControls(false);
            TxtLog.Clear();
            AppendLog("=== BẮT ĐẦU BẬT WINDOWS DEFENDER ===");
            
            ProgBar.IsIndeterminate = true;
            TxtStatus.Text = "Đang kích hoạt Windows Defender...";

            await Task.Run(() =>
            {
                try
                {
                    AppendLog("Đang chạy lệnh kích hoạt Real-time Monitoring...");
                    string errorMsg = RunPowerShellCommand("Set-MpPreference -DisableRealtimeMonitoring $false");
                    if (string.IsNullOrEmpty(errorMsg))
                    {
                        AppendLog("[Thành công] Windows Defender Real-time Protection đã được bật.");
                    }
                    else
                    {
                        AppendLog($"[Cảnh báo] Lỗi khi chạy lệnh: {errorMsg}");
                        AppendLog("[Gợi ý] Hãy đảm bảo tính năng 'Tamper Protection' trong Windows Security đã được tắt để cho phép chỉnh sửa.");
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"[Lỗi] {ex.Message}");
                }
            });

            ProgBar.IsIndeterminate = false;
            ProgBar.Value = 100;
            TxtPercentage.Text = "100%";
            TxtStatus.Text = "Đã bật Windows Defender!";

            await RefreshStatusesAsync();
            SetDefenderControls(true);
        }

        private async void BtnDisableDefender_Click(object sender, RoutedEventArgs e)
        {
            SetDefenderControls(false);
            TxtLog.Clear();
            AppendLog("=== BẮT ĐẦU TẮT WINDOWS DEFENDER ===");
            
            ProgBar.IsIndeterminate = true;
            TxtStatus.Text = "Đang tắt Windows Defender...";

            await Task.Run(() =>
            {
                try
                {
                    AppendLog("Đang chạy lệnh tắt Real-time Monitoring...");
                    string errorMsg = RunPowerShellCommand("Set-MpPreference -DisableRealtimeMonitoring $true");
                    if (string.IsNullOrEmpty(errorMsg))
                    {
                        AppendLog("[Thành công] Windows Defender Real-time Protection đã được tắt.");
                    }
                    else
                    {
                        AppendLog($"[Cảnh báo] Lỗi khi chạy lệnh: {errorMsg}");
                        AppendLog("[Gợi ý] Nếu lệnh bị chặn, hãy kiểm tra và tắt thủ công tính năng 'Tamper Protection' trong: Settings -> Update & Security -> Windows Security -> Virus & threat protection -> Manage settings.");
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"[Lỗi] {ex.Message}");
                }
            });

            ProgBar.IsIndeterminate = false;
            ProgBar.Value = 100;
            TxtPercentage.Text = "100%";
            TxtStatus.Text = "Đã tắt Windows Defender!";

            await RefreshStatusesAsync();
            SetDefenderControls(true);
        }

        private void SetUpdateControls(bool enabled)
        {
            BtnEnableUpdate.IsEnabled = enabled;
            BtnDisableUpdate.IsEnabled = enabled;
        }

        private void SetDefenderControls(bool enabled)
        {
            BtnEnableDefender.IsEnabled = enabled;
            BtnDisableDefender.IsEnabled = enabled;
        }

        private void RunCommand(string fileName, string arguments)
        {
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();
            }
            catch { }
        }

        private string RunPowerShellCommand(string cmd)
        {
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = $"-NoProfile -Command \"{cmd}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string error = process.StandardError.ReadToEnd().Trim();
                process.WaitForExit();
                return error;
            }
            catch (Exception ex)
            {
                return ex.Message;
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

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            ChkUltimatePerf.IsChecked = true;
            ChkDisableCortana.IsChecked = true;
            ChkDisableTelemetry.IsChecked = true;
            ChkDisableBackgroundApps.IsChecked = true;
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            ChkUltimatePerf.IsChecked = false;
            ChkDisableCortana.IsChecked = false;
            ChkDisableTelemetry.IsChecked = false;
            ChkDisableBackgroundApps.IsChecked = false;
        }

        private async void BtnResetTweaks_Click(object sender, RoutedEventArgs e)
        {
            BtnResetTweaks.IsEnabled = false;
            BtnApplyTweaks.IsEnabled = false;
            TxtLog.Clear();
            AppendLog("=== BẮT ĐẦU KHÔI PHỤC THIẾT LẬP HỆ THỐNG MẶC ĐỊNH ===");
            AppendLog($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            AppendLog("--------------------------------------------------");

            ProgBar.Value = 0;
            ProgBar.IsIndeterminate = false;
            TxtPercentage.Text = "0%";
            TxtStatus.Text = "Đang khôi phục...";

            var progressLog = new Progress<string>(msg => AppendLog(msg));
            var progressStatus = new Progress<string>(status => TxtStatus.Text = status);
            var progressPercent = new Progress<int>(val =>
            {
                ProgBar.Value = val;
                TxtPercentage.Text = $"{val}%";
            });

            await Task.Run(() =>
            {
                int totalSteps = 4;
                int currentStep = 0;

                
                currentStep++;
                ((IProgress<string>)progressStatus).Report("Khôi phục Power Scheme mặc định...");
                ((IProgress<int>)progressPercent).Report((int)((double)currentStep / totalSteps * 100));
                try
                {
                    using var process = new Process();
                    process.StartInfo.FileName = "powercfg.exe";
                    process.StartInfo.Arguments = "-setactive 381b4222-f694-41f0-9685-ff5bb260df2e";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit();
                    ((IProgress<string>)progressLog).Report("[Mặc định] Đã đặt sơ đồ nguồn về Balanced (Mặc định).");
                }
                catch (Exception ex)
                {
                    ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể đặt Balanced Power Scheme: {ex.Message}");
                }

                
                currentStep++;
                ((IProgress<string>)progressStatus).Report("Khôi phục Cortana & Bing Search...");
                ((IProgress<int>)progressPercent).Report((int)((double)currentStep / totalSteps * 100));
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search", true))
                    {
                        if (key != null)
                        {
                            key.DeleteValue("BingSearchEnabled", false);
                            key.DeleteValue("CortanaConsent", false);
                        }
                    }
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Policies\Microsoft\Windows\Explorer", true))
                    {
                        key?.DeleteValue("DisableSearchBoxSuggestions", false);
                    }
                    ((IProgress<string>)progressLog).Report("[Mặc định] Đã bật lại Cortana và kết quả tìm kiếm Bing trong Start Menu.");
                }
                catch (Exception ex)
                {
                    ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể khôi phục Cortana/Bing Search: {ex.Message}");
                }

                
                currentStep++;
                ((IProgress<string>)progressStatus).Report("Khôi phục Telemetry...");
                ((IProgress<int>)progressPercent).Report((int)((double)currentStep / totalSteps * 100));
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", true))
                    {
                        key?.DeleteValue("AllowTelemetry", false);
                    }
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", true))
                    {
                        key?.DeleteValue("AllowTelemetry", false);
                    }

                    
                    RunCommand("sc.exe", "config DiagTrack start= auto");
                    RunCommand("sc.exe", "start DiagTrack");

                    ((IProgress<string>)progressLog).Report("[Mặc định] Đã bật lại Telemetry và dịch vụ DiagTrack.");
                }
                catch (Exception ex)
                {
                    ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể khôi phục Telemetry: {ex.Message}");
                }

                
                currentStep++;
                ((IProgress<string>)progressStatus).Report("Khôi phục Background Apps...");
                ((IProgress<int>)progressPercent).Report((int)((double)currentStep / totalSteps * 100));
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", true))
                    {
                        key?.DeleteValue("GlobalUserDisabled", false);
                    }
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", true))
                    {
                        key?.DeleteValue("LetAppsRunInBackground", false);
                    }
                    ((IProgress<string>)progressLog).Report("[Mặc định] Đã cho phép các ứng dụng chạy ngầm.");
                }
                catch (Exception ex)
                {
                    ((IProgress<string>)progressLog).Report($"[Lỗi] Không thể khôi phục ứng dụng chạy ngầm: {ex.Message}");
                }
            });

            AppendLog("\n--------------------------------------------------");
            AppendLog("=== KHÔI PHỤC HỆ THỐNG MẶC ĐỊNH HOÀN TẤT ===");
            AppendLog("==================================================");

            TxtStatus.Text = "Khôi phục thiết lập hoàn tất!";
            BtnResetTweaks.IsEnabled = true;
            BtnApplyTweaks.IsEnabled = true;
        }
    }
}
