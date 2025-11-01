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
using System.Threading.Tasks;
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
        [ObservableProperty] private string _folderPath = string.Empty;
        [ObservableProperty] private int _progressValue = 0;
        [ObservableProperty] private int _progressMax = 100;
        [ObservableProperty] private string _progressText = "대기 중";
        [ObservableProperty] private bool _isUploading = false;

        [ObservableProperty]
        private bool isUploadReady = false;


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
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "CSV 파일이 있는 폴더를 선택하세요";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // 선택한 경로를 속성에 저장
                    FolderPath = dialog.SelectedPath;

                    // GridFileList 초기화 및 파일 로드
                    GridFileList.Clear();

                    //CommunityToolkit의 [ObservableProperty]를 사용하면,
                    //"_gridFileList" 필드는 숨기고, 자동 생성된 "GridFileList" 속성을 사용해야 합니다.                                       
                    // 이후 선택 경로 활용 (예: CSV 파일 읽기 함수 호출 등)
                    CsvUploader.LoadLandMoveCsvFiles(FolderPath,
                            (fName, sttDt, lstDt, recCnt, upCnt)
                            =>
                            {
                                GridFileList.Add(new LandMoveFileList
                                {
                                    fileName = fName,
                                    startDt = ConvertDateFormat(sttDt),
                                    lastDt = ConvertDateFormat(lstDt),
                                    recordCnt = recCnt,
                                    uploadCnt = upCnt
                                });
                            } /* 파일명 추가 */
                            );
                }
            }
        }


        [RelayCommand]
        //Diagram Color 설정화면으로 이동//
        private async void OnUploadCommand()
        {
            if (string.IsNullOrEmpty(FolderPath))
            {
                MessageBox.Show("먼저 폴더를 선택하세요.");
                return;
            }

            IsUploading = true;
            ProgressValue = 0;
            ProgressText = "업로드 준비 중...";

            await Task.Run(() =>
            {
                CsvUploader.UploadLandMoveToDB((current, total, message) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProgressMax = total;
                        ProgressValue = current;
                        ProgressText = $"{message} ({current}/{total})";
                    });
                });
            });

            IsUploading = false;
        }


        // ✅ 날짜 포맷 변환 메서드
        private string ConvertDateFormat(string yyyymmdd)
        {
            if (!string.IsNullOrEmpty(yyyymmdd) && yyyymmdd.Length == 8)
            {
                if (DateTime.TryParseExact(yyyymmdd, "yyyyMMdd",
                    CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None,
                    out DateTime dt))
                {
                    return dt.ToString("yyyy-MM-dd");
                }
            }
            return yyyymmdd;
        }

        // ✅ 업로드 준비 알림 표시
        private void ShowUploadReadyNotification()
        {
            IsUploadReady = true;

            // 3초 후 자동으로 숨김
            Task.Delay(3000).ContinueWith(_ =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsUploadReady = false;
                });
            });
        }
        #endregion
    }
}
