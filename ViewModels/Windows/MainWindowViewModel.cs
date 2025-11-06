using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DevExpress.Dialogs.Core.View;
using DevExpress.Xpf.Diagram;
using DevExpress.Xpf.Printing;
using DevExpress.Xpf.Printing.Native;
using DevExpress.XtraPrinting;
using LMFS.Messages;
using LMFS.ViewModels.Pages;
using LMFS.Views.Pages;
using Microsoft.Win32;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LMFS.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly Frame _frame;

        [ObservableProperty]
        private string _applicationInfo;

        #region 생성자
        public MainWindowViewModel(Frame frame)
        {
            //Title에 버전 표시
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            ApplicationInfo = @$"토지이동흐름도 관리시스템 [{version}]";

            _frame = frame;
            // 시작 페이지를 Home으로 설정
            NavigateToHome();
        }
        #endregion


        #region 파일-읽기
        
        private void LoadData()
        {
            // 파일 탐색기를 열어서 xml 파일 읽음
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "XML Files (*.xml)|*.xml";
            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;
            
            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                WeakReferenceMessenger.Default.Send(new Messages.LoadXmlMessage(filePath));
            }
        }
        #endregion


        [RelayCommand]
        private void NavigateToHome()
        {
            _frame.Navigate(new LandMoveFlowPage());
        }

        [RelayCommand]
        private void NavigateToLandMoveFlow()
        {
            _frame.Navigate(new LandMoveFlowPage());
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            _frame.Navigate(new SettingsPage());
        }

        [RelayCommand]
        private void NavigateToAbout()
        {
            _frame.Navigate(new AboutPage());
        }


        //[주의사항] 메뉴가 MainWindow.xaml에 존재하는데
        //DiagramControl은 LandMoveFlowPage에 존재해서 함수 정의는 LandMoveFlowViewModel에 존재합니다.
        //이미 프로젝트에서 Messenger 패턴을 사용 중 (LoadXmlMessage)
        //ViewModel 간 결합도가 낮음
        //코드 유지보수가 쉬움
        //다른 페이지에서도 같은 명령을 쉽게 사용 가능
        // Command Handler 추가
        [RelayCommand]
        private void Print()
        {
            // LandMoveFlowViewModel의 OnExportPdf를 실행하도록 메시지 전송
            WeakReferenceMessenger.Default.Send(new PrintDiagramMessage());
        }

        [RelayCommand]
        private void PrintPreview()
        {
            WeakReferenceMessenger.Default.Send(new PrintPreviewDiagramMessage());
        }

        [RelayCommand]
        private void ExportPdf()
        {
            WeakReferenceMessenger.Default.Send(new ExportPdfDiagramMessage());
        }

        [RelayCommand]
        private void ExportJpg()
        {
            WeakReferenceMessenger.Default.Send(new ExportJpgDiagramMessage());
        }

        [RelayCommand]
        private void ExportPng()
        {
            WeakReferenceMessenger.Default.Send(new ExportPngDiagramMessage());
        }

    }
}
