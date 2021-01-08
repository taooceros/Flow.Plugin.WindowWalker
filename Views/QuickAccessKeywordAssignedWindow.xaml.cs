using Microsoft.Plugin.WindowWalker.Components;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Microsoft.Plugin.WindowWalker.Views
{
    /// <summary>
    /// QuickAccessKeywordAssignedWindow.xaml 的交互逻辑
    /// </summary>
    public partial class QuickAccessKeywordAssignedWindow : System.Windows.Window
    {
        private string ActionKeyword;
        private Components.Window window;

        public QuickAccessKeywordAssignedWindow(Components.Window window)
        {
            this.window = window;
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ActionKeyword = (sender as TextBox).Text;
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            keywordBox.Focus();
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
