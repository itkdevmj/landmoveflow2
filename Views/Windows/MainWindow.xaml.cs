using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DevExpress.Dialogs.Core.View;
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


        //public IRelayCommand PrintCommand => new RelayCommand(OnPrint, () => ViewModel.FlowPage.FlowVM.LandMoveFlowData != null);
        //public IRelayCommand PrintPreviewCommand => new RelayCommand(OnPrintPreview, () => ViewModel.FlowPage.FlowVM.LandMoveFlowData != null);
        //public IRelayCommand ExportPdfCommand => new RelayCommand(OnExportPdf, () => ViewModel.FlowPage.FlowVM.LandMoveFlowData != null);
        //public IRelayCommand ExportTpgCommand => new RelayCommand(OnExportJpg, () => ViewModel.FlowPage.FlowVM.LandMoveFlowData != null);
        //public IRelayCommand ExportPngCommand => new RelayCommand(OnExportPng, () => ViewModel.FlowPage.FlowVM.LandMoveFlowData != null);
        //public IRelayCommand ExportGridCommand => new RelayCommand(OnExportGrid, () => ViewModel.FlowPage.FlowVM.LandMoveFlowData != null);

        //public void OnPrint() => ViewModel.FlowPage.FlowVM.OnPrint();
        //private void OnPrintPreview() => ViewModel.FlowPage.FlowVM.OnPrintPreview();
        //private void OnExportPdf() => ViewModel.FlowPage.FlowVM.OnExportPdf();
        //private void OnExportJpg() => ViewModel.FlowPage.FlowVM.OnExportJpg();
        //private void OnExportPng() => ViewModel.FlowPage.FlowVM.OnExportPng();
        //private void OnExportGrid() => ViewModel.FlowPage.FlowVM.OnExportGrid();


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

    }
}
