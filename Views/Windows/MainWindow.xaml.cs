using System.ComponentModel;
using System.Windows;
using DevExpress.Xpf.Core;
using LMFS.ViewModels;

namespace LMFS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ThemedWindow
    {

        public MainWindowViewModel ViewModel { get; private set; } = null;

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = ViewModel = new MainWindowViewModel(MainFrame);
        }


        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
