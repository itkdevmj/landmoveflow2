using LMFS.ViewModels.Pages;
using LMFS.Views.Pages;
using System.Windows;

namespace LMFS.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            var page = new AboutPage();
            MainFrame.Content = page;  // Page를 Window의 컨텐츠로
        }
    }
}
