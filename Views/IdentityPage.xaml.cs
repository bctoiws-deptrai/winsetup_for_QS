using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace BctWinsetup.Views
{
    public partial class IdentityPage : UserControl
    {
        public IdentityPage()
        {
            InitializeComponent();
            Loaded += IdentityPage_Loaded;
        }

        private void IdentityPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtCurrentPCName.Text = Environment.MachineName;
                TxtCurrentUserName.Text = Environment.UserName;
            }
            catch
            {
                TxtCurrentPCName.Text = "N/A";
                TxtCurrentUserName.Text = "N/A";
            }
        }

        private void BtnRenamePC_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                
                Process.Start(new ProcessStartInfo
                {
                    FileName = "sysdm.cpl",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở bảng thiết lập tên máy tính: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRenameUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                
                Process.Start(new ProcessStartInfo
                {
                    FileName = "netplwiz.exe",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở bảng quản lý tài khoản người dùng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
