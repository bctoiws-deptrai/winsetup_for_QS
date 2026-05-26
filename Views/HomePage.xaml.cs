using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace BctWinsetup.Views
{
    public partial class HomePage : UserControl
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
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        public HomePage()
        {
            InitializeComponent();
            Loaded += HomePage_Loaded;
        }

        private void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                
                TxtOsVersion.Text = GetOsEdition();
                TxtOsArchitecture.Text = Environment.Is64BitOperatingSystem ? "Hệ điều hành 64-bit (x64)" : "Hệ điều hành 32-bit (x86)";

                
                TxtMachineName.Text = Environment.MachineName;
                TxtUserName.Text = $"Tài khoản hiện tại: {Environment.UserName}";

                
                TxtCpuName.Text = GetCpuName();
                TxtCpuCores.Text = $"Số lõi xử lý (Logical Cores): {Environment.ProcessorCount} cores";

                
                TxtRamSize.Text = GetRamSizeGb();

                
                TxtMainboard.Text = GetMainboardInfo();

                
                TxtGpuName.Text = string.Join("\n", GetGpuNames());

                
                bool isAdmin = IsRunningAsAdmin();
                if (isAdmin)
                {
                    TxtPrivilege.Text = "ADMINISTRATOR (CAO NHẤT)";
                    TxtPrivilege.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113)); 
                    ElpBadgeStatus.Fill = new SolidColorBrush(Color.FromRgb(46, 204, 113));
                    BrdBadge.Background = new SolidColorBrush(Color.FromArgb(30, 46, 204, 113));
                    BrdBadge.BorderBrush = new SolidColorBrush(Color.FromArgb(70, 46, 204, 113));
                }
                else
                {
                    TxtPrivilege.Text = "STANDARD USER (HẠN CHẾ)";
                    TxtPrivilege.Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34)); 
                    ElpBadgeStatus.Fill = new SolidColorBrush(Color.FromRgb(230, 126, 34));
                    BrdBadge.Background = new SolidColorBrush(Color.FromArgb(30, 230, 126, 34));
                    BrdBadge.BorderBrush = new SolidColorBrush(Color.FromArgb(70, 230, 126, 34));
                }
            }
            catch
            {
                
                TxtOsVersion.Text = Environment.OSVersion.VersionString;
                TxtCpuName.Text = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "N/A";
                TxtRamSize.Text = "N/A";
                TxtMainboard.Text = "N/A";
                TxtGpuName.Text = "N/A";
            }
        }

        private bool IsRunningAsAdmin()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        private string GetOsEdition()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        var productName = key.GetValue("ProductName") as string;
                        var displayVersion = key.GetValue("DisplayVersion") as string;
                        var currentBuild = key.GetValue("CurrentBuild") as string;

                        string edition = productName ?? "Windows";
                        
                        
                        if (Environment.OSVersion.Version.Major == 10 && Environment.OSVersion.Version.Build >= 22000)
                        {
                            edition = edition.Replace("Windows 10", "Windows 11");
                        }

                        if (!string.IsNullOrEmpty(displayVersion))
                        {
                            edition += $" - {displayVersion}";
                        }
                        if (!string.IsNullOrEmpty(currentBuild))
                        {
                            edition += $" (Build {currentBuild})";
                        }
                        return edition;
                    }
                }
            }
            catch { }
            return Environment.OSVersion.VersionString;
        }

        private string GetCpuName()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                {
                    if (key != null)
                    {
                        var name = key.GetValue("ProcessorNameString") as string;
                        if (!string.IsNullOrEmpty(name))
                        {
                            return name.Trim();
                        }
                    }
                }
            }
            catch { }
            return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Không xác định";
        }

        private string GetRamSizeGb()
        {
            try
            {
                MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    double totalGb = memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                    return $"{Math.Round(totalGb, 1)} GB RAM";
                }
            }
            catch { }
            return "N/A";
        }

        private string GetMainboardInfo()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS"))
                {
                    if (key != null)
                    {
                        var manufacturer = key.GetValue("SystemManufacturer") as string;
                        var product = key.GetValue("SystemProductName") as string;
                        if (!string.IsNullOrEmpty(manufacturer) && !string.IsNullOrEmpty(product))
                        {
                            return $"{manufacturer.Trim()} - {product.Trim()}";
                        }
                    }
                }
            }
            catch { }
            return "Không xác định";
        }

        private List<string> GetGpuNames()
        {
            var gpus = new List<string>();
            try
            {
                using (var classKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"))
                {
                    if (classKey != null)
                    {
                        foreach (string subkeyName in classKey.GetSubKeyNames())
                        {
                            if (int.TryParse(subkeyName, out _))
                            {
                                using (var subkey = classKey.OpenSubKey(subkeyName))
                                {
                                    var desc = subkey?.GetValue("DriverDesc") as string;
                                    if (!string.IsNullOrEmpty(desc) && !gpus.Contains(desc))
                                    {
                                        gpus.Add(desc);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            if (gpus.Count == 0)
            {
                gpus.Add("Không nhận diện được GPU (Có thể đang dùng Microsoft Basic Display Adapter)");
            }
            return gpus;
        }

        private void CardBeforeWin_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.SetupNavigationGroup(isBeforeWin: true);
        }

        private void CardAfterWin_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.SetupNavigationGroup(isBeforeWin: false);
        }

        private void CardDeepBoot_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.SetupNavigationGroupDeepBoot();
        }
    }
}
