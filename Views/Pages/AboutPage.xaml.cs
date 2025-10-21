using LMFS.ViewModels.Pages;
using System.Windows.Controls;

namespace LMFS.Views.Pages
{
    /// <summary>
    /// AboutPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();
            DataContext = new AboutViewModel();
        }
    }
}
