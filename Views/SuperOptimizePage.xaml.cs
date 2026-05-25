using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace BctWinsetup.Views
{
    public partial class SuperOptimizePage : UserControl
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private static readonly string[] BloatwareList = new string[]
        {
            "Microsoft.BingNews",
            "Microsoft.BingWeather",
            "Microsoft.GetHelp",
            "Microsoft.Getstarted",
            "Microsoft.MicrosoftSolitaireCollection",
            "Microsoft.People",
            "Microsoft.SkypeApp",
            "Microsoft.YourPhone",
            "Microsoft.ZuneVideo",
            "Microsoft.ZuneMusic",
            "Microsoft.WindowsFeedbackHub",
            "Clipchamp.Clipchamp",
            "Microsoft.Todos",
            "Microsoft.PowerAutomateDesktop",
            "Microsoft.MicrosoftOfficeHub",
            "Microsoft.BingSearch",
            "Microsoft.WindowsMaps",
            "Microsoft.LinkedInforWindows",
            "Microsoft.549981C3F5F10",
            "Microsoft.3DBuilder",
            "Microsoft.Microsoft3DViewer",
            "Microsoft.MSPaint",
            "Microsoft.Office.OneNote",
            "Microsoft.DevHome",
            "MicrosoftTeams",
            "Microsoft.MixedReality.Portal",
            "Microsoft.XboxApp",
            "Microsoft.XboxGameOverlay",
            "Microsoft.XboxGamingOverlay",
            "Microsoft.XboxSpeechToTextOverlay",
            "Microsoft.Xbox.TCUI",
            "Microsoft.GamingApp",
            "Microsoft.OneConnect",
            "Microsoft.WindowsCommunicationsApps"
        };

        private static readonly Dictionary<string, string> UwpDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Microsoft.BingNews", "Tin tức (Bing News)" },
            { "Microsoft.BingWeather", "Thời tiết (Bing Weather)" },
            { "Microsoft.GetHelp", "Trợ giúp (Get Help)" },
            { "Microsoft.Getstarted", "Mẹo / Bắt đầu (Tips / Get Started)" },
            { "Microsoft.MicrosoftSolitaireCollection", "Trò chơi Solitaire Collection" },
            { "Microsoft.People", "Danh bạ (Microsoft People)" },
            { "Microsoft.SkypeApp", "Skype" },
            { "Microsoft.YourPhone", "Liên kết điện thoại (Phone Link)" },
            { "Microsoft.ZuneVideo", "Phim & TV (Movies & TV)" },
            { "Microsoft.ZuneMusic", "Trình phát nhạc (Groove Music / Media Player cũ)" },
            { "Microsoft.WindowsFeedbackHub", "Trung tâm phản hồi (Feedback Hub)" },
            { "Clipchamp.Clipchamp", "Trình chỉnh sửa video Clipchamp" },
            { "Microsoft.Todos", "Microsoft To Do" },
            { "Microsoft.PowerAutomateDesktop", "Microsoft Power Automate" },
            { "Microsoft.MicrosoftOfficeHub", "Ứng dụng Microsoft 365 (Office Hub)" },
            { "Microsoft.BingSearch", "Tìm kiếm Bing" },
            { "Microsoft.WindowsMaps", "Bản đồ (Windows Maps)" },
            { "Microsoft.LinkedInforWindows", "LinkedIn" },
            { "Microsoft.549981C3F5F10", "Trợ lý ảo Cortana" },
            { "Microsoft.3DBuilder", "3D Builder" },
            { "Microsoft.Microsoft3DViewer", "3D Viewer" },
            { "Microsoft.MSPaint", "Paint 3D" },
            { "Microsoft.Office.OneNote", "Microsoft OneNote" },
            { "Microsoft.DevHome", "Dev Home" },
            { "MicrosoftTeams", "Microsoft Teams (Consumer)" },
            { "Microsoft.MixedReality.Portal", "Mixed Reality Portal" },
            { "Microsoft.XboxApp", "Xbox App phụ" },
            { "Microsoft.XboxGameOverlay", "Xbox Game Overlay" },
            { "Microsoft.XboxGamingOverlay", "Xbox Game Bar (Gaming Overlay)" },
            { "Microsoft.XboxSpeechToTextOverlay", "Xbox Speech to Text" },
            { "Microsoft.Xbox.TCUI", "Xbox Connection UI" },
            { "Microsoft.GamingApp", "Xbox App chính (Game Hub)" },
            { "Microsoft.OneConnect", "Dịch vụ kết nối mạng phụ" },
            { "Microsoft.WindowsCommunicationsApps", "Ứng dụng Thư & Lịch (Mail and Calendar)" }
        };

        private static readonly Tuple<string, string, string, bool>[] SystemServices = new Tuple<string, string, string, bool>[]
        {
            Tuple.Create("SysMain", "SysMain (Superfetch)", "Tối ưu hóa RAM cho HDD, nhưng gây ngốn RAM/CPU trên SSD.", true),
            Tuple.Create("DiagTrack", "Telemetry (DiagTrack)", "Dịch vụ thu thập thông tin và chẩn đoán gửi về Microsoft.", true),
            Tuple.Create("WSearch", "Windows Search Indexer", "Lập chỉ mục tìm kiếm tệp tin. Tắt đi giúp giảm tải Disk/CPU.", false),
            Tuple.Create("dmwappushservice", "WAP Push Routing Service", "Dịch vụ thu thập thông tin telemetry ngầm bổ sung.", true),
            Tuple.Create("WerSvc", "Windows Error Reporting", "Dịch vụ ghi nhận và gửi báo cáo lỗi hệ thống về Microsoft.", true),
            Tuple.Create("MapsBroker", "Downloaded Maps Manager", "Quản lý bản đồ ngoại tuyến tải về.", true),
            Tuple.Create("RemoteRegistry", "Remote Registry", "Cho phép người dùng từ xa chỉnh sửa Registry.", true),
            Tuple.Create("WalletService", "Wallet Service", "Hỗ trợ thanh toán ví điện tử của Microsoft.", true),
            Tuple.Create("Spooler", "Print Spooler (Máy in)", "Dịch vụ quản lý in ấn. Tắt nếu không dùng máy in.", false),
            Tuple.Create("bthserv", "Bluetooth Support Service", "Hỗ trợ kết nối các thiết bị Bluetooth.", false),
            Tuple.Create("XblAuthManager", "Xbox Live Auth Manager", "Xác thực tài khoản Xbox Live.", false),
            Tuple.Create("XblGameSave", "Xbox Live Game Save", "Đồng bộ lưu file game Xbox Live.", false),
            Tuple.Create("XboxNetApiSvc", "Xbox Live Networking Service", "Dịch vụ mạng Xbox Live.", false),
            Tuple.Create("XboxGipSvc", "Xbox Accessory Management", "Quản lý tay cầm Xbox.", false)
        };

        public SuperOptimizePage()
        {
            InitializeComponent();
            Loaded += SuperOptimizePage_Loaded;
        }

        private async void SuperOptimizePage_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshAllAsync();
        }

        private async Task RefreshAllAsync()
        {
            ProgBar.IsIndeterminate = true;
            TxtStatus.Text = "Đang quét thông tin hệ thống...";
            AppendLog("Bắt đầu quét ứng dụng UWP, dịch vụ và cấu hình hệ thống...");

            await Task.WhenAll(
                RefreshUwpAppsAsync(),
                RefreshServicesAsync(),
                RefreshHvciStatusAsync(),
                RefreshPerformanceStatusAsync(),
                Task.Run(() => Dispatcher.Invoke(RefreshStartupApps))
            );

            ProgBar.IsIndeterminate = false;
            ProgBar.Value = 0;
            TxtStatus.Text = "Đã tải xong thông tin hệ thống.";
            AppendLog("Quét hoàn tất. Sẵn sàng thực hiện tối ưu.");
        }

        private async Task RefreshUwpAppsAsync()
        {
            var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = "-NoProfile -Command \"Get-AppxPackage -AllUsers | Select-Object -ExpandProperty Name\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();

                string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    installed.Add(line.Trim());
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AppendLog($"Lỗi quét UWP: {ex.Message}"));
            }

            Dispatcher.Invoke(() =>
            {
                PnlUwpList.Children.Clear();
                int count = 0;

                foreach (string appName in BloatwareList)
                {
                    if (installed.Contains(appName))
                    {
                        string desc = UwpDescriptions.TryGetValue(appName, out string? val) ? val : appName;

                        var border = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                            BorderBrush = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(4),
                            Padding = new Thickness(12),
                            Margin = new Thickness(0, 0, 0, 8)
                        };

                        var grid = new Grid();
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        var chk = new CheckBox
                        {
                            IsChecked = true,
                            Tag = appName,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(chk, 0);

                        var textPanel = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
                        var titleText = new TextBlock
                        {
                            Text = desc,
                            Foreground = Brushes.White,
                            FontWeight = FontWeights.SemiBold,
                            FontSize = 13
                        };
                        var packageText = new TextBlock
                        {
                            Text = appName,
                            Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                            FontSize = 11,
                            Margin = new Thickness(0, 2, 0, 0)
                        };
                        textPanel.Children.Add(titleText);
                        textPanel.Children.Add(packageText);
                        Grid.SetColumn(textPanel, 1);

                        grid.Children.Add(chk);
                        grid.Children.Add(textPanel);
                        border.Child = grid;
                        PnlUwpList.Children.Add(border);
                        count++;
                    }
                }

                if (count == 0)
                {
                    var emptyText = new TextBlock
                    {
                        Text = "Không phát hiện ứng dụng rác mặc định nào cần gỡ.",
                        Foreground = new SolidColorBrush(Color.FromRgb(16, 124, 65)),
                        FontWeight = FontWeights.Bold,
                        FontSize = 13,
                        Margin = new Thickness(0, 10, 0, 0)
                    };
                    PnlUwpList.Children.Add(emptyText);
                }
            });
        }

        private async Task RefreshServicesAsync()
        {
            var serviceStates = new List<Tuple<string, string, string, bool, string, Brush>>();

            await Task.Run(() =>
            {
                foreach (var service in SystemServices)
                {
                    string name = service.Item1;
                    string friendly = service.Item2;
                    string desc = service.Item3;
                    bool recommend = service.Item4;

                    string state = "Không rõ";
                    Brush color = Brushes.Gray;

                    try
                    {
                        using var process = new Process();
                        process.StartInfo.FileName = "sc.exe";
                        process.StartInfo.Arguments = $"query {name}";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.CreateNoWindow = true;
                        process.Start();
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        string startType = "Không rõ";
                        object? regVal = Registry.GetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{name}", "Start", null);
                        if (regVal is int val)
                        {
                            startType = val switch
                            {
                                2 => "Auto",
                                3 => "Manual",
                                4 => "Disabled",
                                _ => $"Type {val}"
                            };
                        }

                        if (output.Contains("RUNNING"))
                        {
                            state = $"Đang chạy ({startType})";
                            color = new SolidColorBrush(Color.FromRgb(16, 124, 65));
                        }
                        else if (output.Contains("STOPPED"))
                        {
                            state = $"Đã dừng ({startType})";
                            color = startType == "Disabled" ? new SolidColorBrush(Color.FromRgb(168, 0, 0)) : Brushes.Orange;
                        }
                    }
                    catch
                    {
                        state = "Lỗi đọc";
                    }

                    serviceStates.Add(Tuple.Create(name, friendly, desc, recommend, state, color));
                }
            });

            Dispatcher.Invoke(() =>
            {
                PnlServicesList.Children.Clear();
                foreach (var svc in serviceStates)
                {
                    var border = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(12),
                        Margin = new Thickness(0, 0, 0, 8)
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var chk = new CheckBox
                    {
                        IsChecked = svc.Item4,
                        Tag = svc.Item1,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(chk, 0);

                    var textPanel = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
                    var titleText = new TextBlock
                    {
                        Text = svc.Item2,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 13
                    };
                    var descText = new TextBlock
                    {
                        Text = svc.Item3,
                        Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                        FontSize = 11,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    textPanel.Children.Add(titleText);
                    textPanel.Children.Add(descText);
                    Grid.SetColumn(textPanel, 1);

                    var statusText = new TextBlock
                    {
                        Text = svc.Item5,
                        Foreground = svc.Item6,
                        FontWeight = FontWeights.Bold,
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(10, 0, 0, 0)
                    };
                    Grid.SetColumn(statusText, 2);

                    grid.Children.Add(chk);
                    grid.Children.Add(textPanel);
                    grid.Children.Add(statusText);
                    border.Child = grid;
                    PnlServicesList.Children.Add(border);
                }
            });
        }

        private async Task RefreshHvciStatusAsync()
        {
            string status = "Không rõ";
            Brush color = Brushes.Gray;

            await Task.Run(() =>
            {
                try
                {
                    object? val = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", null);
                    if (val is int i && i == 1)
                    {
                        status = "Đang BẬT (Bảo vệ an toàn)";
                        color = new SolidColorBrush(Color.FromRgb(16, 124, 65));
                    }
                    else
                    {
                        status = "Đang TẮT (Tối ưu hiệu năng CPU)";
                        color = new SolidColorBrush(Color.FromRgb(168, 0, 0));
                    }
                }
                catch
                {
                    status = "Lỗi đọc";
                }
            });

            Dispatcher.Invoke(() =>
            {
                TxtHvciStatus.Text = status;
                TxtHvciStatus.Foreground = color;
            });
        }

        private async Task RefreshPerformanceStatusAsync()
        {
            var states = new Dictionary<string, Tuple<bool, string, Brush>>();

            await Task.Run(() =>
            {
                bool visOpt = false;
                object? visVal = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", null);
                if (visVal is int visInt && visInt == 2) visOpt = true;
                states["VisualEffects"] = Tuple.Create(visOpt, visOpt ? "[Đã tối ưu]" : "[Mặc định]", visOpt ? (Brush)new SolidColorBrush(Color.FromRgb(16, 124, 65)) : Brushes.Gray);

                bool dvrOpt = false;
                object? dvrVal = Registry.GetValue(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_Enabled", null);
                if (dvrVal is int dvrInt && dvrInt == 0) dvrOpt = true;
                states["GameDvr"] = Tuple.Create(dvrOpt, dvrOpt ? "[Đã tối ưu]" : "[Mặc định]", dvrOpt ? (Brush)new SolidColorBrush(Color.FromRgb(16, 124, 65)) : Brushes.Gray);

                bool priorityOpt = false;
                object? priVal = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", null);
                if (priVal is int priInt && priInt == 38) priorityOpt = true;
                states["CpuPriority"] = Tuple.Create(priorityOpt, priorityOpt ? "[Đã tối ưu]" : "[Mặc định]", priorityOpt ? (Brush)new SolidColorBrush(Color.FromRgb(16, 124, 65)) : Brushes.Gray);

                bool pagingOpt = false;
                object? pagVal = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", null);
                if (pagVal is int pagInt && pagInt == 1) pagingOpt = true;
                states["Paging"] = Tuple.Create(pagingOpt, pagingOpt ? "[Đã tối ưu]" : "[Mặc định]", pagingOpt ? (Brush)new SolidColorBrush(Color.FromRgb(16, 124, 65)) : Brushes.Gray);

                bool cacheOpt = false;
                object? cacheVal = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", null);
                if (cacheVal is int cacheInt && cacheInt == 1) cacheOpt = true;
                states["Cache"] = Tuple.Create(cacheOpt, cacheOpt ? "[Đã tối ưu]" : "[Mặc định]", cacheOpt ? (Brush)new SolidColorBrush(Color.FromRgb(16, 124, 65)) : Brushes.Gray);

                bool throttleOpt = false;
                object? thrVal = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", null);
                if (thrVal is int thrInt && thrInt == 1) throttleOpt = true;
                states["Throttling"] = Tuple.Create(throttleOpt, throttleOpt ? "[Đã tối ưu]" : "[Mặc định]", throttleOpt ? (Brush)new SolidColorBrush(Color.FromRgb(16, 124, 65)) : Brushes.Gray);

                bool dynamicTickOpt = IsDynamicTickDisabled();
                states["DynamicTick"] = Tuple.Create(dynamicTickOpt, dynamicTickOpt ? "[Đã tối ưu]" : "[Mặc định]", dynamicTickOpt ? (Brush)new SolidColorBrush(Color.FromRgb(16, 124, 65)) : Brushes.Gray);

                bool hagsOpt = false;
                object? hagsVal = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", null);
                if (hagsVal is int hagsInt && hagsInt == 2) hagsOpt = true;
                states["Hags"] = Tuple.Create(hagsOpt, hagsOpt ? "[Đã tối ưu]" : "[Mặc định]", hagsOpt ? (Brush)new SolidColorBrush(Color.FromRgb(16, 124, 65)) : Brushes.Gray);
            });

            Dispatcher.Invoke(() =>
            {
                ChkVisualEffects.IsChecked = states["VisualEffects"].Item1;
                TxtStatusVisualEffects.Text = states["VisualEffects"].Item2;
                TxtStatusVisualEffects.Foreground = states["VisualEffects"].Item3;

                ChkGameDvr.IsChecked = states["GameDvr"].Item1;
                TxtStatusGameDvr.Text = states["GameDvr"].Item2;
                TxtStatusGameDvr.Foreground = states["GameDvr"].Item3;

                ChkCpuPriority.IsChecked = states["CpuPriority"].Item1;
                TxtStatusCpuPriority.Text = states["CpuPriority"].Item2;
                TxtStatusCpuPriority.Foreground = states["CpuPriority"].Item3;

                ChkPaging.IsChecked = states["Paging"].Item1;
                TxtStatusPaging.Text = states["Paging"].Item2;
                TxtStatusPaging.Foreground = states["Paging"].Item3;

                ChkCache.IsChecked = states["Cache"].Item1;
                TxtStatusCache.Text = states["Cache"].Item2;
                TxtStatusCache.Foreground = states["Cache"].Item3;

                ChkThrottling.IsChecked = states["Throttling"].Item1;
                TxtStatusThrottling.Text = states["Throttling"].Item2;
                TxtStatusThrottling.Foreground = states["Throttling"].Item3;

                ChkDynamicTick.IsChecked = states["DynamicTick"].Item1;
                TxtStatusDynamicTick.Text = states["DynamicTick"].Item2;
                TxtStatusDynamicTick.Foreground = states["DynamicTick"].Item3;

                ChkHags.IsChecked = states["Hags"].Item1;
                TxtStatusHags.Text = states["Hags"].Item2;
                TxtStatusHags.Foreground = states["Hags"].Item3;
            });
        }

        private bool IsDynamicTickDisabled()
        {
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = "bcdedit.exe";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output.IndexOf("disabledynamictick", StringComparison.OrdinalIgnoreCase) >= 0 &&
                       output.IndexOf("Yes", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private void RefreshStartupApps()
        {
            PnlStartupList.Children.Clear();
            AddStartupItemsFromKey(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", "HKCU");
            AddStartupItemsFromKey(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "HKLM");
        }

        private void AddStartupItemsFromKey(RegistryKey rootKey, string subKeyPath, string keyLabel)
        {
            try
            {
                using var key = rootKey.OpenSubKey(subKeyPath, false);
                if (key == null) return;
                foreach (string name in key.GetValueNames())
                {
                    object? val = key.GetValue(name);
                    string path = val?.ToString() ?? string.Empty;

                    var border = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(8),
                        Margin = new Thickness(0, 0, 0, 6)
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var chk = new CheckBox
                    {
                        IsChecked = false,
                        Tag = $"{keyLabel}|{name}",
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(chk, 0);

                    var textPanel = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
                    var titleText = new TextBlock
                    {
                        Text = $"{name} ({keyLabel})",
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 12
                    };
                    var pathText = new TextBlock
                    {
                        Text = path,
                        Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                        FontSize = 10,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    textPanel.Children.Add(titleText);
                    textPanel.Children.Add(pathText);
                    Grid.SetColumn(textPanel, 1);

                    grid.Children.Add(chk);
                    grid.Children.Add(textPanel);
                    border.Child = grid;
                    PnlStartupList.Children.Add(border);
                }
            }
            catch { }
        }

        private async void BtnUninstallUwp_Click(object sender, RoutedEventArgs e)
        {
            var appsToRemove = new List<string>();
            foreach (var child in PnlUwpList.Children)
            {
                if (child is Border border && border.Child is Grid grid)
                {
                    foreach (var sub in grid.Children)
                    {
                        if (sub is CheckBox chk && chk.IsChecked == true && chk.Tag is string tag)
                        {
                            appsToRemove.Add(tag);
                        }
                    }
                }
            }

            if (appsToRemove.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một ứng dụng để gỡ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnUninstallUwp.IsEnabled = false;
            ProgBar.Value = 0;
            ProgBar.IsIndeterminate = false;
            TxtStatus.Text = "Đang gỡ cài đặt ứng dụng...";

            await Task.Run(() =>
            {
                int count = 0;
                int total = appsToRemove.Count;

                foreach (string appName in appsToRemove)
                {
                    count++;
                    int pct = (int)((double)count / total * 100);

                    Dispatcher.Invoke(() =>
                    {
                        ProgBar.Value = pct;
                        TxtPercentage.Text = $"{pct}%";
                        TxtStatus.Text = $"Đang gỡ: {appName} ({count}/{total})";
                        AppendLog($"[{count}/{total}] Đang gỡ gói ứng dụng: {appName}...");
                    });

                    try
                    {
                        using var process = new Process();
                        process.StartInfo.FileName = "powershell.exe";
                        process.StartInfo.Arguments = $"-NoProfile -Command \"Get-AppxPackage -AllUsers -Name '{appName}' | Remove-AppxPackage -AllUsers\"";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.Start();
                        process.WaitForExit();

                        using var procProv = new Process();
                        procProv.StartInfo.FileName = "powershell.exe";
                        procProv.StartInfo.Arguments = $"-NoProfile -Command \"Get-AppxProvisionedPackage -Online | Where-Object {{$_.DisplayName -eq '{appName}'}} | Remove-AppxProvisionedPackage -Online\"";
                        procProv.StartInfo.UseShellExecute = false;
                        procProv.StartInfo.CreateNoWindow = true;
                        procProv.Start();
                        procProv.WaitForExit();

                        Dispatcher.Invoke(() => AppendLog($"  -> Đã gỡ thành công: {appName}"));
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => AppendLog($"  -> Gặp lỗi khi gỡ: {ex.Message}"));
                    }
                }
            });

            TxtStatus.Text = "Đã gỡ cài đặt hoàn tất các ứng dụng đã chọn!";
            ProgBar.Value = 100;
            TxtPercentage.Text = "100%";
            BtnUninstallUwp.IsEnabled = true;

            await RefreshUwpAppsAsync();
        }

        private void BtnSelectAllUwp_Click(object sender, RoutedEventArgs e)
        {
            SetAllCheckboxes(PnlUwpList, true);
        }

        private void BtnDeselectAllUwp_Click(object sender, RoutedEventArgs e)
        {
            SetAllCheckboxes(PnlUwpList, false);
        }

        private async void BtnRefreshUwp_Click(object sender, RoutedEventArgs e)
        {
            await RefreshUwpAppsAsync();
        }

        private async void BtnDisableServices_Click(object sender, RoutedEventArgs e)
        {
            var servicesToDisable = new List<string>();
            foreach (var child in PnlServicesList.Children)
            {
                if (child is Border border && border.Child is Grid grid)
                {
                    foreach (var sub in grid.Children)
                    {
                        if (sub is CheckBox chk && chk.IsChecked == true && chk.Tag is string tag)
                        {
                            servicesToDisable.Add(tag);
                        }
                    }
                }
            }

            if (servicesToDisable.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một dịch vụ để tắt.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnDisableServices.IsEnabled = false;
            ProgBar.Value = 0;
            ProgBar.IsIndeterminate = false;
            TxtStatus.Text = "Đang vô hiệu hóa các dịch vụ...";

            await Task.Run(() =>
            {
                int count = 0;
                int total = servicesToDisable.Count;

                foreach (string svc in servicesToDisable)
                {
                    count++;
                    int pct = (int)((double)count / total * 100);

                    Dispatcher.Invoke(() =>
                    {
                        ProgBar.Value = pct;
                        TxtPercentage.Text = $"{pct}%";
                        TxtStatus.Text = $"Đang tắt dịch vụ: {svc} ({count}/{total})";
                        AppendLog($"[{count}/{total}] Đang tắt dịch vụ: {svc}...");
                    });

                    try
                    {
                        RunCommand("sc.exe", $"stop {svc}");
                        RunCommand("sc.exe", $"config {svc} start= disabled");
                        Dispatcher.Invoke(() => AppendLog($"  -> Đã dừng và vô hiệu hóa dịch vụ: {svc}"));
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => AppendLog($"  -> Lỗi khi xử lý dịch vụ {svc}: {ex.Message}"));
                    }
                }
            });

            TxtStatus.Text = "Vô hiệu hóa các dịch vụ hoàn tất!";
            ProgBar.Value = 100;
            TxtPercentage.Text = "100%";
            BtnDisableServices.IsEnabled = true;

            await RefreshServicesAsync();
        }

        private async void BtnRestoreServices_Click(object sender, RoutedEventArgs e)
        {
            BtnRestoreServices.IsEnabled = false;
            ProgBar.Value = 0;
            ProgBar.IsIndeterminate = false;
            TxtStatus.Text = "Đang khôi phục các dịch vụ mặc định...";

            await Task.Run(() =>
            {
                int count = 0;
                int total = SystemServices.Length;

                foreach (var svc in SystemServices)
                {
                    string name = svc.Item1;
                    count++;
                    int pct = (int)((double)count / total * 100);

                    Dispatcher.Invoke(() =>
                    {
                        ProgBar.Value = pct;
                        TxtPercentage.Text = $"{pct}%";
                        TxtStatus.Text = $"Đang khôi phục dịch vụ: {name} ({count}/{total})";
                        AppendLog($"[{count}/{total}] Khôi phục trạng thái mặc định của dịch vụ: {name}...");
                    });

                    try
                    {
                        string defaultStart = name switch
                        {
                            "SysMain" => "auto",
                            "DiagTrack" => "auto",
                            "WSearch" => "auto",
                            "dmwappushservice" => "demand",
                            "WerSvc" => "demand",
                            "MapsBroker" => "auto",
                            "RemoteRegistry" => "demand",
                            "WalletService" => "demand",
                            "Spooler" => "auto",
                            "bthserv" => "demand",
                            "XblAuthManager" => "demand",
                            "XblGameSave" => "demand",
                            "XboxNetApiSvc" => "demand",
                            "XboxGipSvc" => "demand",
                            _ => "demand"
                        };

                        RunCommand("sc.exe", $"config {name} start= {defaultStart}");
                        if (defaultStart == "auto")
                        {
                            RunCommand("sc.exe", $"start {name}");
                        }
                        Dispatcher.Invoke(() => AppendLog($"  -> Đã khôi phục dịch vụ: {name} ({defaultStart})"));
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => AppendLog($"  -> Lỗi khôi phục dịch vụ {name}: {ex.Message}"));
                    }
                }
            });

            TxtStatus.Text = "Đã khôi phục các dịch vụ mặc định hoàn tất!";
            ProgBar.Value = 100;
            TxtPercentage.Text = "100%";
            BtnRestoreServices.IsEnabled = true;

            await RefreshServicesAsync();
        }

        private void BtnSelectAllServices_Click(object sender, RoutedEventArgs e)
        {
            SetAllCheckboxes(PnlServicesList, true);
        }

        private void BtnDeselectAllServices_Click(object sender, RoutedEventArgs e)
        {
            SetAllCheckboxes(PnlServicesList, false);
        }

        private async void BtnRefreshServices_Click(object sender, RoutedEventArgs e)
        {
            await RefreshServicesAsync();
        }

        private async void BtnCleanRam_Click(object sender, RoutedEventArgs e)
        {
            var memBefore = new MEMORYSTATUSEX();
            GlobalMemoryStatusEx(memBefore);
            ulong freeBefore = memBefore.ullAvailPhys;

            AppendLog("=== BẮT ĐẦU GIẢI PHÓNG BỘ NHỚ RAM ===");
            AppendLog($"Dung lượng RAM vật lý trống trước: {(freeBefore / 1024 / 1024)} MB");

            ProgBar.IsIndeterminate = true;
            TxtStatus.Text = "Đang dọn dẹp RAM vật lý của hệ thống...";

            await Task.Run(() =>
            {
                Process[] processes = Process.GetProcesses();
                int successCount = 0;
                foreach (Process proc in processes)
                {
                    try
                    {
                        if (proc.Id == 0 || proc.Id == 4) continue;
                        IntPtr hProcess = OpenProcess(0x1F0FFF, false, proc.Id);
                        if (hProcess == IntPtr.Zero)
                        {
                            hProcess = OpenProcess(0x0400 | 0x1358, false, proc.Id);
                        }
                        if (hProcess != IntPtr.Zero)
                        {
                            if (EmptyWorkingSet(hProcess))
                            {
                                successCount++;
                            }
                            CloseHandle(hProcess);
                        }
                    }
                    catch { }
                }
                Dispatcher.Invoke(() => AppendLog($"Đã tối ưu Working Set cho {successCount} tiến trình."));
            });

            var memAfter = new MEMORYSTATUSEX();
            GlobalMemoryStatusEx(memAfter);
            ulong freeAfter = memAfter.ullAvailPhys;

            ProgBar.IsIndeterminate = false;
            ProgBar.Value = 100;
            TxtPercentage.Text = "100%";
            TxtStatus.Text = "Giải phóng bộ nhớ RAM thành công!";

            AppendLog($"Dung lượng RAM vật lý trống sau: {(freeAfter / 1024 / 1024)} MB");
            long released = (long)freeAfter - (long)freeBefore;
            if (released > 0)
            {
                AppendLog($"Giải phóng thành công: {(released / 1024 / 1024)} MB RAM!");
            }
            else
            {
                AppendLog("Bộ nhớ hệ thống hiện tại đã ở trạng thái tối ưu.");
            }
            AppendLog("======================================");
        }

        private async void BtnApplyPerformance_Click(object sender, RoutedEventArgs e)
        {
            await ApplyPerformanceSettingsAsync(false);
        }

        private async void BtnRestorePerformance_Click(object sender, RoutedEventArgs e)
        {
            await ApplyPerformanceSettingsAsync(true);
        }

        private async Task ApplyPerformanceSettingsAsync(bool forceDefaultAll)
        {
            ProgBar.IsIndeterminate = true;
            TxtStatus.Text = forceDefaultAll ? "Đang khôi phục cấu hình mặc định..." : "Đang áp dụng cấu hình hiệu năng...";
            AppendLog(forceDefaultAll ? "Bắt đầu khôi phục các thiết lập Windows về mặc định..." : "Bắt đầu áp dụng tinh chỉnh hiệu năng hệ thống...");

            bool visualEffects = false;
            bool gameDvr = false;
            bool cpuPriority = false;
            bool paging = false;
            bool cache = false;
            bool throttling = false;
            bool dynamicTick = false;
            bool hags = false;

            Dispatcher.Invoke(() =>
            {
                if (forceDefaultAll)
                {
                    ChkVisualEffects.IsChecked = false;
                    ChkGameDvr.IsChecked = false;
                    ChkCpuPriority.IsChecked = false;
                    ChkPaging.IsChecked = false;
                    ChkCache.IsChecked = false;
                    ChkThrottling.IsChecked = false;
                    ChkDynamicTick.IsChecked = false;
                    ChkHags.IsChecked = false;
                }

                visualEffects = ChkVisualEffects.IsChecked == true;
                gameDvr = ChkGameDvr.IsChecked == true;
                cpuPriority = ChkCpuPriority.IsChecked == true;
                paging = ChkPaging.IsChecked == true;
                cache = ChkCache.IsChecked == true;
                throttling = ChkThrottling.IsChecked == true;
                dynamicTick = ChkDynamicTick.IsChecked == true;
                hags = ChkHags.IsChecked == true;
            });

            await Task.Run(() =>
            {
                try
                {
                    if (visualEffects)
                    {
                        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2, RegistryValueKind.DWord);
                        Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics", "MinAnimate", "0", RegistryValueKind.String);
                        Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "DragFullWindows", "0", RegistryValueKind.String);
                        Dispatcher.Invoke(() => AppendLog("  -> Đã tối ưu hiệu ứng đồ họa (tắt hoạt ảnh, bóng đổ)."));
                    }
                    else
                    {
                        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 1, RegistryValueKind.DWord);
                        Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics", "MinAnimate", "1", RegistryValueKind.String);
                        Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "DragFullWindows", "1", RegistryValueKind.String);
                        Dispatcher.Invoke(() => AppendLog("  -> Đã khôi phục hiệu ứng đồ họa mặc định."));
                    }

                    if (gameDvr)
                    {
                        Registry.SetValue(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_Enabled", 0, RegistryValueKind.DWord);
                        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0, RegistryValueKind.DWord);
                        Dispatcher.Invoke(() => AppendLog("  -> Đã tắt chế độ ghi hình ngầm Game DVR của Xbox."));
                    }
                    else
                    {
                        Registry.SetValue(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_Enabled", 1, RegistryValueKind.DWord);
                        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 1, RegistryValueKind.DWord);
                        Dispatcher.Invoke(() => AppendLog("  -> Đã bật lại chế độ ghi hình ngầm Game DVR mặc định."));
                    }

                    if (cpuPriority)
                    {
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 38, RegistryValueKind.DWord);
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0, RegistryValueKind.DWord);
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", -1, RegistryValueKind.DWord);
                        Dispatcher.Invoke(() => AppendLog("  -> Đã tối ưu độ trễ hệ thống và ưu tiên CPU cho Foreground Apps."));
                    }
                    else
                    {
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 2, RegistryValueKind.DWord);
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 20, RegistryValueKind.DWord);
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", 10, RegistryValueKind.DWord);
                        Dispatcher.Invoke(() => AppendLog("  -> Đã khôi phục mức độ ưu tiên CPU mặc định."));
                    }

                    if (paging)
                    {
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 1, RegistryValueKind.DWord);
                        Dispatcher.Invoke(() => AppendLog("  -> Đã buộc nhân hệ thống Windows lưu trên RAM vật lý."));
                    }
                    else
                    {
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 0, RegistryValueKind.DWord);
                        Dispatcher.Invoke(() => AppendLog("  -> Đã khôi phục lưu nhân hệ thống mặc định (cho phép ghi ổ cứng)."));
                    }

                    if (cache)
                    {
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 1, RegistryValueKind.DWord);
                        Dispatcher.Invoke(() => AppendLog("  -> Đã tăng kích thước bộ đệm hệ thống LargeSystemCache."));
                    }
                    else
                    {
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 0, RegistryValueKind.DWord);
                        Dispatcher.Invoke(() => AppendLog("  -> Đã đặt LargeSystemCache về mặc định."));
                    }

                    if (throttling)
                    {
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1, RegistryValueKind.DWord);
                        Dispatcher.Invoke(() => AppendLog("  -> Đã tắt giới hạn năng lượng CPU chạy nền (Power Throttling)."));
                    }
                    else
                    {
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 0, RegistryValueKind.DWord);
                        Dispatcher.Invoke(() => AppendLog("  -> Đã bật giới hạn năng lượng CPU chạy nền mặc định."));
                    }

                    if (dynamicTick)
                    {
                        RunCommand("bcdedit.exe", "/set disabledynamictick yes");
                        Dispatcher.Invoke(() => AppendLog("  -> Đã tắt tiết kiệm năng lượng CPU khi nhàn rỗi (Dynamic Tick)."));
                    }
                    else
                    {
                        RunCommand("bcdedit.exe", "/set disabledynamictick no");
                        Dispatcher.Invoke(() => AppendLog("  -> Đã bật tiết kiệm năng lượng CPU Dynamic Tick mặc định."));
                    }

                    if (hags)
                    {
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2, RegistryValueKind.DWord);
                        Dispatcher.Invoke(() => AppendLog("  -> Đã bật Lập lịch tăng tốc GPU bằng phần cứng (HAGS)."));
                    }
                    else
                    {
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 1, RegistryValueKind.DWord);
                        Dispatcher.Invoke(() => AppendLog("  -> Đã tắt Lập lịch tăng tốc GPU HAGS mặc định."));
                    }

                    foreach (var proc in Process.GetProcessesByName("explorer"))
                    {
                        proc.Kill();
                    }
                    Dispatcher.Invoke(() => AppendLog("  -> Đã khởi động lại Windows Explorer để áp dụng hiệu ứng đồ họa mới."));
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => AppendLog($"[Lỗi] Áp dụng tinh chỉnh: {ex.Message}"));
                }
            });

            await RefreshPerformanceStatusAsync();

            ProgBar.IsIndeterminate = false;
            ProgBar.Value = 100;
            TxtPercentage.Text = "100%";
            TxtStatus.Text = forceDefaultAll ? "Khôi phục cấu hình mặc định thành công!" : "Áp dụng cấu hình tối ưu thành công!";
            AppendLog(forceDefaultAll ? "=== KHÔI PHỤC MẶC ĐỊNH HOÀN TẤT ===" : "=== ÁP DỤNG TINH CHỈNH HOÀN TẤT ===");
        }

        private async void BtnDisableHvci_Click(object sender, RoutedEventArgs e)
        {
            ProgBar.IsIndeterminate = true;
            TxtStatus.Text = "Đang tắt Memory Integrity (HVCI)...";
            AppendLog("Tắt bảo mật Memory Integrity qua registry để tăng tốc CPU...");

            await Task.Run(() =>
            {
                try
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 0, RegistryValueKind.DWord);
                    Dispatcher.Invoke(() => AppendLog("[Thành công] Đã tắt HVCI. Vui lòng khởi động lại máy tính để thay đổi có hiệu lực."));
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => AppendLog($"[Lỗi] Tắt HVCI: {ex.Message}"));
                }
            });

            ProgBar.IsIndeterminate = false;
            ProgBar.Value = 100;
            TxtPercentage.Text = "100%";
            TxtStatus.Text = "Đã tắt HVCI (Yêu cầu khởi động lại máy)!";
            await RefreshHvciStatusAsync();
        }

        private async void BtnEnableHvci_Click(object sender, RoutedEventArgs e)
        {
            ProgBar.IsIndeterminate = true;
            TxtStatus.Text = "Đang bật Memory Integrity (HVCI)...";
            AppendLog("Bật bảo mật Memory Integrity qua registry...");

            await Task.Run(() =>
            {
                try
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 1, RegistryValueKind.DWord);
                    Dispatcher.Invoke(() => AppendLog("[Thành công] Đã bật HVCI. Vui lòng khởi động lại máy tính để thay đổi có hiệu lực."));
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => AppendLog($"[Lỗi] Bật HVCI: {ex.Message}"));
                }
            });

            ProgBar.IsIndeterminate = false;
            ProgBar.Value = 100;
            TxtPercentage.Text = "100%";
            TxtStatus.Text = "Đã bật HVCI (Yêu cầu khởi động lại máy)!";
            await RefreshHvciStatusAsync();
        }

        private void BtnApplyStartup_Click(object sender, RoutedEventArgs e)
        {
            var itemsToDisable = new List<Tuple<string, string>>();
            foreach (var child in PnlStartupList.Children)
            {
                if (child is Border border && border.Child is Grid grid)
                {
                    foreach (var sub in grid.Children)
                    {
                        if (sub is CheckBox chk && chk.IsChecked == true && chk.Tag is string tag)
                        {
                            string[] parts = tag.Split('|');
                            if (parts.Length == 2)
                            {
                                itemsToDisable.Add(Tuple.Create(parts[0], parts[1]));
                            }
                        }
                    }
                }
            }

            if (itemsToDisable.Count == 0)
            {
                MessageBox.Show("Vui lòng tích chọn ứng dụng khởi động bạn muốn tắt.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AppendLog("=== BẮT ĐẦU TẮT ỨNG DỤNG KHỞI ĐỘNG ===");
            int count = 0;

            foreach (var item in itemsToDisable)
            {
                string keyLabel = item.Item1;
                string valueName = item.Item2;
                count++;

                try
                {
                    if (keyLabel == "HKCU")
                    {
                        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                        if (key != null)
                        {
                            key.DeleteValue(valueName, false);
                            AppendLog($"[{count}] Đã xóa ứng dụng khởi động (HKCU): {valueName}");
                        }
                    }
                    else if (keyLabel == "HKLM")
                    {
                        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                        if (key != null)
                        {
                            key.DeleteValue(valueName, false);
                            AppendLog($"[{count}] Đã xóa ứng dụng khởi động (HKLM): {valueName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"[{count}] Lỗi khi xóa mục {valueName}: {ex.Message}");
                }
            }

            AppendLog("Tắt ứng dụng khởi động hoàn tất.");
            AppendLog("======================================");

            RefreshStartupApps();
        }

        private void BtnRefreshStartup_Click(object sender, RoutedEventArgs e)
        {
            RefreshStartupApps();
        }

        private void SetAllCheckboxes(Panel panel, bool isChecked)
        {
            foreach (var child in panel.Children)
            {
                if (child is Border border && border.Child is Grid grid)
                {
                    foreach (var sub in grid.Children)
                    {
                        if (sub is CheckBox chk)
                        {
                            chk.IsChecked = isChecked;
                        }
                    }
                }
            }
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
