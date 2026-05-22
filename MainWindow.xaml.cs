using System.Windows;
using BctWinsetup.Views;

namespace BctWinsetup
{
    public partial class MainWindow : Window
    {
        private HomePage? _homePage;
        private DriverBackupPage? _driverBackupPage;
        private DriverRestorePage? _driverRestorePage;
        private IdentityPage? _identityPage;
        private SoftwareInstallPage? _softwareInstallPage;
        private FontInstallPage? _fontInstallPage;
        private ClearLogsPage? _clearLogsPage;
        private GuiOptimizePage? _guiOptimizePage;
        private SystemOptimizePage? _systemOptimizePage;
        private DeepCleanupPage? _deepCleanupPage;

        public MainWindow()
        {
            InitializeComponent();
            
            NavigateToHome();
        }

        private void Navigation_Checked(object sender, RoutedEventArgs e)
        {
            
            if (MainContentArea == null) return;

            if (sender == RadHome)
            {
                NavigateToHome();
            }
            else if (sender == RadDriverBackup)
            {
                NavigateToDriverBackup();
            }
            else if (sender == RadDriverRestore)
            {
                NavigateToDriverRestore();
            }
            else if (sender == RadIdentity)
            {
                NavigateToIdentity();
            }
            else if (sender == RadSoftwareInstall)
            {
                NavigateToSoftwareInstall();
            }
            else if (sender == RadFontInstall)
            {
                NavigateToFontInstall();
            }
            else if (sender == RadClearLogs)
            {
                NavigateToClearLogs();
            }
            else if (sender == RadGuiOptimize)
            {
                NavigateToGuiOptimize();
            }
            else if (sender == RadSystemOptimize)
            {
                NavigateToSystemOptimize();
            }
            else if (sender == RadDeepCleanup)
            {
                NavigateToDeepCleanup();
            }
        }

        private void NavigateToHome()
        {
            _homePage ??= new HomePage();
            MainContentArea.Content = _homePage;
        }

        private void NavigateToDriverBackup()
        {
            _driverBackupPage ??= new DriverBackupPage();
            MainContentArea.Content = _driverBackupPage;
        }

        private void NavigateToDriverRestore()
        {
            _driverRestorePage ??= new DriverRestorePage();
            MainContentArea.Content = _driverRestorePage;
        }

        private void NavigateToIdentity()
        {
            _identityPage ??= new IdentityPage();
            MainContentArea.Content = _identityPage;
        }

        private void NavigateToSoftwareInstall()
        {
            _softwareInstallPage ??= new SoftwareInstallPage();
            MainContentArea.Content = _softwareInstallPage;
        }

        private void NavigateToFontInstall()
        {
            _fontInstallPage ??= new FontInstallPage();
            MainContentArea.Content = _fontInstallPage;
        }

        private void NavigateToClearLogs()
        {
            _clearLogsPage ??= new ClearLogsPage();
            MainContentArea.Content = _clearLogsPage;
        }

        private void NavigateToGuiOptimize()
        {
            _guiOptimizePage ??= new GuiOptimizePage();
            MainContentArea.Content = _guiOptimizePage;
        }

        private void NavigateToSystemOptimize()
        {
            _systemOptimizePage ??= new SystemOptimizePage();
            MainContentArea.Content = _systemOptimizePage;
        }

        private void NavigateToDeepCleanup()
        {
            _deepCleanupPage ??= new DeepCleanupPage();
            MainContentArea.Content = _deepCleanupPage;
        }
    }
}