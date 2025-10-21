using LMFS.ViewModels.Pages;
using System.Windows.Controls;
using LMFS.Services;

namespace LMFS.Views.Pages
{
    /// <summary>
    /// SettingsPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            var service = new WpfMessageBoxService();
            DataContext = new SettingsViewModel(service);
        }
    }
}
