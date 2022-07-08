using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Flow.Plugin.WindowWalker.Views
{
    /// <summary>
    /// QuickAccessKeywordAssignedWindow.xaml 的交互逻辑
    /// </summary>
    public partial class QuickAccessKeywordAssignedWindow : System.Windows.Window
    {
        public string ActionKeyword { get; set; } = "";
        public string WindowName { get; set; }
        private Components.Window window;

        public QuickAccessKeywordAssignedWindow(Components.Window window)
        {
            this.window = window;
            WindowName = window.Process.Name;
            InitializeComponent();
        }
        
        private void OnLoad(object sender, RoutedEventArgs e)
        {
            KeywordBox.Focus();
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnDone_OnClick(object sender, RoutedEventArgs e)
        {
            Main.cachedWindows[ActionKeyword] = window;
            Close();
        }

        private void KeywordBoxOnKeywordDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnDone_OnClick(this, new RoutedEventArgs());
            }
        }
    }
}
