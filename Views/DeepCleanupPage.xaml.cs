using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BctWinsetup.Views
{
    public partial class DeepCleanupPage : UserControl
    {
        public DeepCleanupPage()
        {
            InitializeComponent();
            Loaded += DeepCleanupPage_Loaded;
        }

        private void DeepCleanupPage_Loaded(object sender, RoutedEventArgs e)
        {
            CheckWindowsOldStatus();
        }

        private void CheckWindowsOldStatus()
        {
            if (Directory.Exists(@"C:\Windows.old"))
            {
                TxtWindowsOldStatus.Text = "Phát hiện (Chưa dọn dẹp, có thể giải phóng dung lượng)";
                TxtWindowsOldStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)); 
                ChkDeleteWindowsOld.IsEnabled = true;
            }
            else
            {
                TxtWindowsOldStatus.Text = "Không phát hiện (Đã dọn dẹp hoặc không tồn tại)";
                TxtWindowsOldStatus.Foreground = new SolidColorBrush(Color.FromRgb(16, 124, 65)); 
                ChkDeleteWindowsOld.IsChecked = false;
                ChkDeleteWindowsOld.IsEnabled = false;
            }
        }

        private async void BtnStartCleanup_Click(object sender, RoutedEventArgs e)
        {
            bool cleanSxS = ChkCleanWinSxS.IsChecked == true;
            bool deleteWinOld = ChkDeleteWindowsOld.IsChecked == true;

            if (!cleanSxS && !deleteWinOld)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một hạng mục để dọn dẹp.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnStartCleanup.IsEnabled = false;
            ChkCleanWinSxS.IsEnabled = false;
            ChkDeleteWindowsOld.IsEnabled = false;
            TxtLog.Clear();

            AppendLog("=== BẮT ĐẦU TIẾN TRÌNH DỌN DẸP HỆ THỐNG CHUYÊN SÂU ===");
            AppendLog($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            AppendLog("--------------------------------------------------");

            ProgBar.Value = 0;
            ProgBar.IsIndeterminate = false;
            TxtPercentage.Text = "0%";
            TxtStatus.Text = "Đang dọn dẹp...";

            var progressLog = new Progress<string>(msg => AppendLog(msg));
            var progressStatus = new Progress<string>(status => TxtStatus.Text = status);
            var progressPercent = new Progress<int>(val =>
            {
                ProgBar.Value = val;
                TxtPercentage.Text = $"{val}%";
            });

            await Task.Run(async () =>
            {
                int totalSteps = (cleanSxS ? 1 : 0) + (deleteWinOld ? 3 : 0);
                int currentStep = 0;

                
                if (cleanSxS)
                {
                    currentStep++;
                    ((IProgress<string>)progressStatus).Report("Đang chạy dọn dẹp WinSxS Component Store...");
                    ((IProgress<string>)progressLog).Report(">>> Chạy công cụ DISM (startcomponentcleanup)...");
                    ((IProgress<string>)progressLog).Report("Lưu ý: Quá trình này có thể mất từ 5-15 phút. Vui lòng giữ ứng dụng hoạt động.");

                    Dispatcher.Invoke(() =>
                    {
                        ProgBar.IsIndeterminate = true;
                        TxtPercentage.Text = "Chờ...";
                    });

                    bool success = await RunProcessAsync("dism.exe", "/online /cleanup-image /startcomponentcleanup /resetbase", progressLog);
                    
                    Dispatcher.Invoke(() =>
                    {
                        ProgBar.IsIndeterminate = false;
                        ProgBar.Value = (int)((double)currentStep / totalSteps * 100);
                        TxtPercentage.Text = $"{(int)((double)currentStep / totalSteps * 100)}%";
                    });

                    if (success)
                    {
                        ((IProgress<string>)progressLog).Report("[Thành công] Hoàn thành dọn dẹp WinSxS Component Store.");
                    }
                    else
                    {
                        ((IProgress<string>)progressLog).Report("[Lỗi] Công cụ DISM trả về lỗi hoặc không thành công.");
                    }
                }

                
                if (deleteWinOld)
                {
                    if (Directory.Exists(@"C:\Windows.old"))
                    {
                        
                        currentStep++;
                        ((IProgress<string>)progressStatus).Report("Đang chiếm quyền sở hữu C:\\Windows.old...");
                        ((IProgress<string>)progressLog).Report(">>> Chiếm quyền sở hữu thư mục C:\\Windows.old (takeown)...");
                        
                        bool successTakeown = await RunProcessAsync("takeown.exe", "/F C:\\Windows.old /A /R /D Y", progressLog);
                        Dispatcher.Invoke(() =>
                        {
                            ProgBar.Value = (int)((double)currentStep / totalSteps * 100);
                            TxtPercentage.Text = $"{(int)((double)currentStep / totalSteps * 100)}%";
                        });

                        if (successTakeown)
                        {
                            ((IProgress<string>)progressLog).Report("[Thành công] Đã chiếm quyền sở hữu thư mục.");
                        }
                        else
                        {
                            ((IProgress<string>)progressLog).Report("[Cảnh báo] Lỗi khi chạy lệnh takeown, có thể gặp lỗi khi xóa.");
                        }

                        
                        currentStep++;
                        ((IProgress<string>)progressStatus).Report("Đang phân quyền truy cập C:\\Windows.old...");
                        ((IProgress<string>)progressLog).Report(">>> Cấp toàn quyền ghi và sửa cho Administrators (icacls)...");
                        
                        bool successIcacls = await RunProcessAsync("icacls.exe", "C:\\Windows.old /grant *S-1-5-32-544:F /T /C /Q", progressLog);
                        Dispatcher.Invoke(() =>
                        {
                            ProgBar.Value = (int)((double)currentStep / totalSteps * 100);
                            TxtPercentage.Text = $"{(int)((double)currentStep / totalSteps * 100)}%";
                        });

                        if (successIcacls)
                        {
                            ((IProgress<string>)progressLog).Report("[Thành công] Đã gán quyền ghi đầy đủ.");
                        }
                        else
                        {
                            ((IProgress<string>)progressLog).Report("[Cảnh báo] Lỗi khi chạy lệnh icacls, có thể một số tệp tin không xóa được.");
                        }

                        
                        currentStep++;
                        ((IProgress<string>)progressStatus).Report("Đang thực hiện xóa C:\\Windows.old...");
                        ((IProgress<string>)progressLog).Report(">>> Xóa thư mục C:\\Windows.old...");

                        bool successDelete = await RunProcessAsync("cmd.exe", "/c rd /s /q C:\\Windows.old", progressLog);
                        Dispatcher.Invoke(() =>
                        {
                            ProgBar.Value = 100;
                            TxtPercentage.Text = "100%";
                        });

                        if (!Directory.Exists(@"C:\Windows.old"))
                        {
                            ((IProgress<string>)progressLog).Report("[Thành công] Thư mục C:\\Windows.old đã được xóa sạch hoàn toàn.");
                        }
                        else
                        {
                            ((IProgress<string>)progressLog).Report("[Lỗi] Thư mục C:\\Windows.old vẫn tồn tại. Có thể do một số tệp tin hệ thống đang bị khóa.");
                        }
                    }
                    else
                    {
                        ((IProgress<string>)progressLog).Report("[Bỏ qua] Thư mục C:\\Windows.old không tồn tại.");
                        currentStep += 3;
                        Dispatcher.Invoke(() =>
                        {
                            ProgBar.Value = 100;
                            TxtPercentage.Text = "100%";
                        });
                    }
                }
            });

            AppendLog("\n--------------------------------------------------");
            AppendLog("=== HOÀN TẤT DỌN DẸP CHUYÊN SÂU ===");
            AppendLog("==================================================");

            TxtStatus.Text = "Dọn dẹp hoàn tất!";
            BtnStartCleanup.IsEnabled = true;
            ChkCleanWinSxS.IsEnabled = true;
            
            CheckWindowsOldStatus();
        }

        private Task<bool> RunProcessAsync(string fileName, string arguments, IProgress<string> logger)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var process = new Process();
                    process.StartInfo.FileName = fileName;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            logger.Report(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            logger.Report($"[Lỗi] {e.Data}");
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    return process.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    logger.Report($"[Lỗi thực thi] {fileName} {arguments}: {ex.Message}");
                    return false;
                }
            });
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
            ChkCleanWinSxS.IsChecked = true;
            if (ChkDeleteWindowsOld.IsEnabled)
            {
                ChkDeleteWindowsOld.IsChecked = true;
            }
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            ChkCleanWinSxS.IsChecked = false;
            ChkDeleteWindowsOld.IsChecked = false;
        }
    }
}
