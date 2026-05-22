using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BctWinsetup.Views
{
    public partial class DriverRestorePage : UserControl
    {
        private bool _isRestoring = false;

        public DriverRestorePage()
        {
            InitializeComponent();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFolderDialog folderDialog = new OpenFolderDialog
                {
                    Title = "Chọn thư mục chứa bản sao lưu Driver",
                    InitialDirectory = string.IsNullOrEmpty(TxtSourcePath.Text) ? "D:\\" : Path.GetDirectoryName(TxtSourcePath.Text)
                };

                if (folderDialog.ShowDialog() == true)
                {
                    TxtSourcePath.Text = folderDialog.FolderName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hiển thị hộp thoại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnStartRestore_Click(object sender, RoutedEventArgs e)
        {
            if (_isRestoring) return;

            string sourcePath = TxtSourcePath.Text.Trim();
            if (string.IsNullOrEmpty(sourcePath) || !Directory.Exists(sourcePath))
            {
                MessageBox.Show("Thư mục nguồn không tồn tại hoặc không hợp lệ. Vui lòng chọn lại.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _isRestoring = true;
                SetUiState(false);
                TxtLog.Clear();
                ProgBar.IsIndeterminate = true;
                TxtStatus.Text = "Đang chuẩn bị khôi phục Driver...";
                TxtPercentage.Text = "";

                AppendLog($"[Bắt đầu khôi phục lúc {DateTime.Now:HH:mm:ss}]\n");
                AppendLog($"Thư mục nguồn: {sourcePath}\n");
                AppendLog("Đang gọi pnputil.exe /add-driver ... /subdirs /install\n");
                AppendLog("Quá trình này có thể mất vài phút tùy vào số lượng driver...\n");
                AppendLog("--------------------------------------------------\n");

                int exitCode = await Task.Run(() => RunRestoreProcess(sourcePath));

                ProgBar.IsIndeterminate = false;
                if (exitCode == 0)
                {
                    ProgBar.Value = 100;
                    TxtStatus.Text = "Khôi phục Driver thành công!";
                    TxtPercentage.Text = "100%";
                    AppendLog("\n--------------------------------------------------");
                    AppendLog($"\n[Hoàn thành thành công lúc {DateTime.Now:HH:mm:ss}]");
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
                TxtStatus.Text = "Đã xảy ra lỗi trong quá trình khôi phục.";
                AppendLog($"\n[NGOẠI LỆ] {ex.Message}");
                MessageBox.Show($"Lỗi thực thi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isRestoring = false;
                SetUiState(true);
            }
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            string sourcePath = TxtSourcePath.Text.Trim();
            if (Directory.Exists(sourcePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{sourcePath}\"",
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
            BtnStartRestore.IsEnabled = enabled;
            BtnBrowse.IsEnabled = enabled;
        }

        private int RunRestoreProcess(string sourcePath)
        {
            
            string searchPath = Path.Combine(sourcePath, "*.inf");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "pnputil.exe",
                Arguments = $"/add-driver \"{searchPath}\" /subdirs /install",
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
                            
                            if (e.Data.Contains("Adding driver package") || e.Data.Contains("Installing driver"))
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
