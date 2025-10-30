using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CsvHelper;
using DevExpress.Data.Browsing;
using DevExpress.Xpf.Grid;
using LMFS.Db;
using LMFS.Engine;
using LMFS.Models;
using LMFS.Services;
using LMFS.Views.Pages;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace LMFS.ViewModels.Pages
{
    public partial class CsvUploaderViewModel : ObservableObject
    {
        //CommunityToolkit의 [ObservableProperty]를 사용하면,
        //"_gridFileList" 필드는 숨기고, 자동 생성된 "GridFileList" 속성을 사용해야 합니다.                                       
        [ObservableProperty] private List<LandMoveFileList> _gridFileList;

        //public ICommand OpenFolderCommand { get; }

        public Action CloseAction { get; set; }
        public ICommand CloseCommand { get; }

        public CsvUploaderViewModel()
        {
            CloseCommand = new RelayCommand(ExecuteClose);
            //OpenFolderCommand = new RelayCommand(OnOpenFolder);
        }

        private void ExecuteClose()
        {
            throw new NotImplementedException();
        }

        [RelayCommand]
        //Diagram Color 설정화면으로 이동//
        private void OnOpenFolder()
        {
            string folderPath = string.Empty;
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "CSV 파일이 있는 폴더를 선택하세요";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    folderPath = dialog.SelectedPath;

                    //CommunityToolkit의 [ObservableProperty]를 사용하면,
                    //"_gridFileList" 필드는 숨기고, 자동 생성된 "GridFileList" 속성을 사용해야 합니다.                                       
                    // 이후 선택 경로 활용 (예: CSV 파일 읽기 함수 호출 등)
                    var records = CsvUploader.LoadLandMoveCsvFiles(folderPath, 
                            (fName, sttDt, lstDt, recCnt, upCnt) 
                            => {GridFileList.Add(new LandMoveFileList 
                                { 
                                    fileName = fName,
                                    startDt = sttDt,
                                    lastDt = lstDt,
                                    recordCnt = recCnt, 
                                    uploadCnt = upCnt 
                                }); } /* 파일명 추가 */
                            );
                }
            }
        }


        [RelayCommand]
        //Diagram Color 설정화면으로 이동//
        private void OnUploadCommand()
        {
            //var page = new CsvUploaderPage();
            //Window window = new Window
            //{
            //    Content = page,
            //    Title = "토지이동흐름도 자료(CSV) 업로드",
            //    Width = 340,
            //    Height = 240,
            //    Owner = Application.Current.MainWindow,

            //    //[닫기]버튼만 남긴다.
            //    WindowStyle = WindowStyle.SingleBorderWindow,
            //    ResizeMode = ResizeMode.NoResize
            //};
            //window.WindowStartupLocation = WindowStartupLocation.Manual;//부모화면의 임의위치로 지정

            //// 부모(Window)의 실제 스크린 위치 좌표 구하기
            //var parent = Application.Current.MainWindow;
            //// 부모창의 스크린 좌표를 가져오기
            //var parentTopLeft = parent.PointToScreen(new System.Windows.Point(0, 0));

            //// 부모창의 우측 상단 위치에 새 창을 붙이기
            //window.Left = parentTopLeft.X + parent.ActualWidth - window.Width - 10;
            //window.Top = parentTopLeft.Y + 100;

            //window.ShowDialog();//부모화면 제어 불가
        }
    }
}
