using CommunityToolkit.Mvvm.Messaging;
using DevExpress.Xpf.Core.Native;
using LMFS.Messages;
using LMFS.Models;
using LMFS.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;//ToList()
using CsvHelper;//NuGet 패키지 설치 Must //Install-Package CsvHelper
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace LMFS.Views.Pages
{
    /// <summary>
    /// ProductsPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CsvUploaderPage : Page
    {
        // 폴더 선택 다이얼로그 코드 (이벤트 혹은 Main 함수 등에서 사용)
        string folderPath = string.Empty;


        public CsvUploaderPage(LandMoveFlowViewModel flowVM)
        {
            InitializeComponent();
            var vm = new CsvUploaderViewModel(flowVM);//Binding OpenFolderCommand => OnOpenFolder()로 연결//
            this.DataContext = vm;
        }

        private void UpdateDataGridMaxHeight()
        {
            double windowHeight = this.ActualHeight;
            double otherControlsHeight = 30 + 10 + 10;
            double systemUiHeight = 40;
            double calculatedMaxHeight = windowHeight - otherControlsHeight - systemUiHeight;

            if (calculatedMaxHeight > 0)
            {
                GridFileList.MaxHeight = calculatedMaxHeight;
            }
        }

        private void CsvUploaderPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateDataGridMaxHeight();
        }

        private void CsvUploaderPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDataGridMaxHeight();
        }

    }
}
