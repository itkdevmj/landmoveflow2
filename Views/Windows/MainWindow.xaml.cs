using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DevExpress.Xpf.Core;
using LMFS.Messages;
using LMFS.ViewModels;
using LMFS.ViewModels.Pages;
using LMFS.Views.Pages;
using System.ComponentModel;
using System.Text;
using System.Windows;

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

            // .NET Core/5/6 이상의 환경에서 반드시 필요
            //StreamReader(file, Encoding.GetEncoding("euc-kr"))) // 한글 파일(euc-kr 인코딩)  
            //위해서 반드시 필요
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }


        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        [RelayCommand]
        private void OnPrint()
        {
            WeakReferenceMessenger.Default.Send(new PrintDiagramMessage());
        }
        [RelayCommand]
        private void OnPrintPreview()
        {
            WeakReferenceMessenger.Default.Send(new PrintPreviewDiagramMessage());
        }
        [RelayCommand]
        private void OnExportGrid()
        {
            int a = 1;
        }
    }
}
