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
    //ViewModel은 "UI와 데이터 바인딩·상태만 관리"하는 역할이 가장 좋음.
    public partial class CsvUploaderViewModel : ObservableObject
    {
        //CommunityToolkit의 [ObservableProperty]를 사용하면,
        //"_gridFileList" 필드는 숨기고, 자동 생성된 "GridFileList" 속성을 사용해야 합니다.
        //List<T> 대신 ObservableCollection<T>를 사용해야 함
        //XAML에서 데이터 바인딩 시, 리스트에 변화가 생길 때 UI가 자동 갱신되는 타입은 ObservableCollection<T>입니다.
        [ObservableProperty] private ObservableCollection<LandMoveFileList> _gridFileList;


        //public ICommand OpenFolderCommand { get; }

        public Action CloseAction { get; set; }
        public ICommand CloseCommand { get; }

        public CsvUploaderViewModel()
        {
            CloseCommand = new RelayCommand(ExecuteClose);
            _gridFileList = new ObservableCollection<LandMoveFileList>();//초기화를 해줘야 GridFileList.Add 시 예외가 발생하지 않음//
            //OpenFolderCommand = new RelayCommand(OnOpenFolder);
        }

        private void ExecuteClose()
        {
            throw new NotImplementedException();
        }


#region 바인딩 영역
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
                    CsvUploader.LoadLandMoveCsvFiles(folderPath, 
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
            //
        }
        #endregion


        #region 내부 함수 영역

        #endregion
    }
}
