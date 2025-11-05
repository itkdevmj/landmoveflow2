using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DevExpress.Xpf.Diagram;
using DevExpress.Xpf.Printing;
using DevExpress.Xpf.Printing.Native;
using DevExpress.XtraPrinting;
using LMFS.Views.Pages;
using Microsoft.Win32;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

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
    }
}
