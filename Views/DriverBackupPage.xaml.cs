using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BctWinsetup.Views
{
    public partial class DriverBackupPage : UserControl
    {
        private bool _isBackingUp = false;
        private int _failedCount = 0;

        public DriverBackupPage()
        {
            InitializeComponent();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                
                OpenFolderDialog folderDialog = new OpenFolderDialog
                {
                    Title = "Chọn thư mục để sao lưu Driver",
                    InitialDirectory = string.IsNullOrEmpty(TxtDestPath.Text) ? "D:\\" : Path.GetDirectoryName(TxtDestPath.Text)
                };

                if (folderDialog.ShowDialog() == true)
                {
                    TxtDestPath.Text = folderDialog.FolderName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hiển thị hộp thoại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnStartBackup_Click(object sender, RoutedEventArgs e)
        {
            if (_isBackingUp) return;

            string destPath = TxtDestPath.Text.Trim();
            if (string.IsNullOrEmpty(destPath))
            {
                MessageBox.Show("Vui lòng chọn thư mục lưu trữ trước khi tiếp tục.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _isBackingUp = true;
                _failedCount = 0;
                SetUiState(false);
                TxtLog.Clear();
                ProgBar.IsIndeterminate = true;
                TxtStatus.Text = "Đang chuẩn bị sao lưu...";
                TxtPercentage.Text = "";

                
                if (!Directory.Exists(destPath))
                {
                    Directory.CreateDirectory(destPath);
                    AppendLog($"Đã tạo thư mục lưu trữ: {destPath}\n");
                }

                AppendLog($"[Bắt đầu sao lưu lúc {DateTime.Now:HH:mm:ss}]\n");
                AppendLog($"Thư mục đích: {destPath}\n");
                AppendLog("Đang gọi pnputil.exe /export-driver * ...\n");
                AppendLog("--------------------------------------------------\n");

                int exitCode = await Task.Run(() => RunBackupProcess(destPath));

                ProgBar.IsIndeterminate = false;
                
                if (exitCode == 0 || exitCode == 3)
                {
                    ProgBar.Value = 100;
                    TxtStatus.Text = "Sao lưu Driver thành công!";
                    TxtPercentage.Text = "100%";
                    BtnOpenFolder.IsEnabled = true;
                    AppendLog("\n--------------------------------------------------");
                    
                    if (exitCode == 3 || _failedCount > 0)
                    {
                        AppendLog($"\n[Lưu ý] Phát hiện thấy khoảng {_failedCount} driver có đăng ký trong hệ thống nhưng tệp nguồn gốc không tồn tại.");
                        AppendLog("\n-> Hiện tượng này hoàn toàn bình thường do các driver cũ đã bị dọn dẹp khỏi Windows Driver Store.");
                        AppendLog("\n-> Quá trình sao lưu toàn bộ các driver còn lại thành công!");
                    }
                    else
                    {
                        AppendLog("\nQuá trình sao lưu hoàn thành thành công!");
                    }
                    AppendLog($"\n[Hoàn thành lúc {DateTime.Now:HH:mm:ss}]");
                }
                else
                {
                    ProgBar.Value = 0;
                    TxtStatus.Text = $"Quá trình kết thúc với mã lỗi: {exitCode}";
                    AppendLog($"\n[Lỗi] Quá trình kết thúc với mã lỗi: {exitCode}");
                }
            }
            catch (Exception ex)
            {
                ProgBar.IsIndeterminate = false;
                TxtStatus.Text = "Đã xảy ra lỗi trong quá trình sao lưu.";
                AppendLog($"\n[NGOẠI LỆ] {ex.Message}");
                MessageBox.Show($"Lỗi thực thi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isBackingUp = false;
                SetUiState(true);
            }
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            string destPath = TxtDestPath.Text.Trim();
            if (Directory.Exists(destPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{destPath}\"",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể mở thư mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SetUiState(bool enabled)
        {
            BtnStartBackup.IsEnabled = enabled;
            BtnBrowse.IsEnabled = enabled;
        }

        private int RunBackupProcess(string destPath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "pnputil.exe",
                Arguments = $"/export-driver * \"{destPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            AppendLog(e.Data + "\n");
                            
                            
                            if (e.Data.Contains("Failed to export") || e.Data.Contains("Failed to copy") || 
                                e.Data.Contains("không thể xuất") || e.Data.Contains("Lỗi sao chép") || e.Data.Contains("error 3"))
                            {
                                _failedCount++;
                            }
                            
                            
                            if (e.Data.Contains("Exporting driver"))
                            {
                                TxtStatus.Text = e.Data;
                            }
                        });
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            AppendLog($"[ERR] {e.Data}\n");
                        });
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                return process.ExitCode;
            }
        }

        private void AppendLog(string text)
        {
            TxtLog.AppendText(text);
            TxtLog.ScrollToEnd();
        }
    }
}
