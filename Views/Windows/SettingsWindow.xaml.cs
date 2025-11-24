using LMFS.ViewModels.Pages;
using LMFS.Views.Pages;
using System.Windows;

namespace LMFS.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            var page = new SettingsPage();
            RootGrid.Children.Add(page);  // Page를 Window의 컨텐츠로
        }
    }
}
