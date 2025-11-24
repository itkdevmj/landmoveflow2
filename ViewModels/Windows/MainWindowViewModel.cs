using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DevExpress.Data.Browsing;
using DevExpress.Dialogs.Core.View;
using DevExpress.Xpf;
using DevExpress.Xpf.Diagram;
using DevExpress.Xpf.Printing;
using DevExpress.Xpf.Printing.Native;
using DevExpress.XtraPrinting;
using LMFS.Messages;
using LMFS.ViewModels;    // 혹은 프로젝트에서 MainWindowViewModel이 정의된 네임스페이스
using LMFS.ViewModels.Pages;
using LMFS.Views;
using LMFS.Views.Pages;
using Microsoft.Win32;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AboutWindow = LMFS.Views.AboutWindow;


namespace LMFS.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly Frame _frame;

        public LandMoveFlowPage FlowPage { get; set; }
        public LandMoveSettingViewModel SettingVM { get; set; }


        [ObservableProperty]private string _applicationInfo;

        //[토지이동흐름도] 메뉴 숨김(Hide)
        [ObservableProperty] private bool _isLandMoveFlowVisible = false;
        //public bool IsLandMoveFlowVisible
        //{
        //    get => _isLandMoveFlowVisible;
        //    set => SetProperty(ref _isLandMoveFlowVisible, value);
        //}


        #region 생성자
        public MainWindowViewModel(Frame frame)
        {
            //Title에 버전 표시
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            ApplicationInfo = @$"토지이동흐름도 관리시스템 [{version}]";

            _frame = frame;

            // 색상설정 VM 셋팅
            AssignSettingVM();

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
            FlowPage = new LandMoveFlowPage(this.SettingVM);
            _frame.Navigate(FlowPage);
        }

        [RelayCommand]
        private void NavigateToLandMoveFlow()
        {
            FlowPage = new LandMoveFlowPage(this.SettingVM);
            _frame.Navigate(FlowPage);
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            //팝업으로 변경//_frame.Navigate(new SettingsPage());
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = Application.Current.MainWindow; // 메인 프로그램(Window)을 owner로 지정
            settingsWindow.ShowDialog();
        }

        [RelayCommand]
        private void NavigateToAbout()
        {
            //팝업으로 변경//_frame.Navigate(new AboutPage());
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = Application.Current.MainWindow; // 메인 프로그램(Window)을 owner로 지정
            aboutWindow.ShowDialog();
        }

        //[색상설정]
        [RelayCommand]
        private void AssignSettingVM()
        {
            SettingVM = new LandMoveSettingViewModel();
            //기본 색상 (or 사용자 정의 색상) 가져오기
            SettingVM.SettingDefaultColor();
            SettingVM.GetSettingColor();
        }

        //[주의사항] 메뉴가 MainWindow.xaml에 존재하는데
        //DiagramControl은 LandMoveFlowPage에 존재해서 함수 정의는 LandMoveFlowViewModel에 존재합니다.
    }
}
