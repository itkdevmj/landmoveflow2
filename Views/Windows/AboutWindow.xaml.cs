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
            RootGrid.Children.Add(page);  // Page를 Window의 컨텐츠로
        }
    }
}
