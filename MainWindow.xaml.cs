using System.Windows;
using BctWinsetup.Views;

namespace BctWinsetup
{
    public partial class MainWindow : Window
    {
        private HomePage? _homePage;
        private DriverBackupPage? _driverBackupPage;
        private BeforeWinBackupPage? _beforeWinBackupPage;
        private DriverRestorePage? _driverRestorePage;
        private IdentityPage? _identityPage;
        private SoftwareInstallPage? _softwareInstallPage;
        private FontInstallPage? _fontInstallPage;
        private ClearLogsPage? _clearLogsPage;
        private GuiOptimizePage? _guiOptimizePage;
        private SystemOptimizePage? _systemOptimizePage;
        private DeepCleanupPage? _deepCleanupPage;
        private RunScriptPage? _runScriptPage;

        public MainWindow()
        {
            InitializeComponent();
            NavigateToHome();
        }

        public void SetupNavigationGroup(bool isBeforeWin)
        {
            ColSidebar.Width = new GridLength(230);
            SidebarGrid.Visibility = Visibility.Visible;
            SidebarBorder.Visibility = Visibility.Visible;

            if (isBeforeWin)
            {
                PnlBeforeWinNav.Visibility = Visibility.Visible;
                PnlAfterWinNav.Visibility = Visibility.Collapsed;
                RadDriverBackup.IsChecked = true;
                NavigateToDriverBackup();
            }
            else
            {
                PnlBeforeWinNav.Visibility = Visibility.Collapsed;
                PnlAfterWinNav.Visibility = Visibility.Visible;
                RadDriverRestore.IsChecked = true;
                NavigateToDriverRestore();
            }
        }

        private void Navigation_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContentArea == null) return;

            if (sender == RadDriverBackup)
            {
                NavigateToDriverBackup();
            }
            else if (sender == RadDataBackup)
            {
                NavigateToDataBackup();
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
            else if (sender == RadRunScript)
            {
                NavigateToRunScript();
            }
        }

        private void RadBackToHome_Checked(object sender, RoutedEventArgs e)
        {
            NavigateToHome();
        }

        public void NavigateToHome()
        {
            ColSidebar.Width = new GridLength(0);
            SidebarGrid.Visibility = Visibility.Collapsed;
            SidebarBorder.Visibility = Visibility.Collapsed;

            _homePage ??= new HomePage();
            MainContentArea.Content = _homePage;
        }

        private void NavigateToDriverBackup()
        {
            _driverBackupPage ??= new DriverBackupPage();
            MainContentArea.Content = _driverBackupPage;
        }

        private void NavigateToDataBackup()
        {
            _beforeWinBackupPage ??= new BeforeWinBackupPage();
            MainContentArea.Content = _beforeWinBackupPage;
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

        private void NavigateToRunScript()
        {
            _runScriptPage ??= new RunScriptPage();
            MainContentArea.Content = _runScriptPage;
        }
    }
}