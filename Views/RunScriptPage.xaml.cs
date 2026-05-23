using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BctWinsetup.Views
{
    public partial class RunScriptPage : UserControl
    {
        private readonly string _defaultScriptPath;

        public RunScriptPage()
        {
            InitializeComponent();
            _defaultScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "default_script.txt");
            LoadDefaultScriptSilently();
        }

        private void LoadDefaultScriptSilently()
        {
            try
            {
                if (File.Exists(_defaultScriptPath))
                {
                    TxtScript.Text = File.ReadAllText(_defaultScriptPath, Encoding.UTF8);
                }
                else
                {
                    TxtScript.Text = "# Nhập script PowerShell hoặc CMD của bạn tại đây\nWrite-Output \"Hello from BCT Winsetup!\"\n";
                }
            }
            catch { }
        }

        private void BtnSaveDefault_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string script = TxtScript.Text;
                File.WriteAllText(_defaultScriptPath, script, Encoding.UTF8);
                MessageBox.Show("Đã lưu script hiện tại làm mặc định thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể lưu script mặc định: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLoadDefault_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(_defaultScriptPath))
                {
                    TxtScript.Text = File.ReadAllText(_defaultScriptPath, Encoding.UTF8);
                    AppendLog("[Hệ thống] Đã nạp lại script mặc định.");
                }
                else
                {
                    MessageBox.Show("Chưa có script mặc định nào được lưu.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể nạp script mặc định: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnRunScript_Click(object sender, RoutedEventArgs e)
        {
            string scriptText = TxtScript.Text.Trim();
            if (string.IsNullOrEmpty(scriptText))
            {
                MessageBox.Show("Vui lòng nhập nội dung script cần thực thi.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnRunScript.IsEnabled = false;
            BtnSaveDefault.IsEnabled = false;
            BtnLoadDefault.IsEnabled = false;
            TxtScript.IsEnabled = false;
            TxtLog.Clear();

            bool isPowerShell = RadPowerShell.IsChecked == true;
            string shellName = isPowerShell ? "PowerShell" : "Command Prompt";
            
            AppendLog($"=== BẮT ĐẦU THỰC THI SCRIPT ({shellName.ToUpper()}) ===");
            AppendLog($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            AppendLog("--------------------------------------------------");

            ProgBar.IsIndeterminate = true;
            TxtStatus.Text = $"Đang thực thi script qua {shellName}...";

            string ext = isPowerShell ? ".ps1" : ".bat";
            string tempScriptPath = Path.Combine(Path.GetTempPath(), $"bct_script_{Guid.NewGuid():N}{ext}");

            await Task.Run(async () =>
            {
                try
                {
                    if (isPowerShell)
                    {
                        File.WriteAllText(tempScriptPath, scriptText, new UTF8Encoding(false));
                    }
                    else
                    {
                        File.WriteAllText(tempScriptPath, scriptText, Encoding.GetEncoding(1258));
                    }

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = isPowerShell ? "powershell.exe" : "cmd.exe",
                        Arguments = isPowerShell 
                            ? $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScriptPath}\"" 
                            : $"/c \"{tempScriptPath}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = Encoding.UTF8
                    };

                    using (Process process = new Process { StartInfo = psi })
                    {
                        process.OutputDataReceived += (s, ev) =>
                        {
                            if (ev.Data != null) AppendLog(ev.Data);
                        };
                        process.ErrorDataReceived += (s, ev) =>
                        {
                            if (ev.Data != null) AppendLog($"[Lỗi] {ev.Data}");
                        };

                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        
                        await process.WaitForExitAsync();
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"[Lỗi Hệ Thống] {ex.Message}");
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tempScriptPath))
                        {
                            File.Delete(tempScriptPath);
                        }
                    }
                    catch { }
                }
            });

            AppendLog("--------------------------------------------------");
            AppendLog("=== QUÁ TRÌNH THỰC THI SCRIPT KẾT THÚC ===");
            
            ProgBar.IsIndeterminate = false;
            ProgBar.Value = 100;
            TxtStatus.Text = "Thực thi hoàn tất!";

            BtnRunScript.IsEnabled = true;
            BtnSaveDefault.IsEnabled = true;
            BtnLoadDefault.IsEnabled = true;
            TxtScript.IsEnabled = true;
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
    }
}
