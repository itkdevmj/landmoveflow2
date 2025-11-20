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
        [ObservableProperty] private bool _isCommiting = false;
        [ObservableProperty] private bool isUploadReady = false;
        [ObservableProperty] private bool _isUploadCompleted = false;
        [ObservableProperty] private bool _isCommitCompleted = false;

        public bool ShowCommitButton => IsUploadCompleted && !IsCommitCompleted;

        //[ObservableProperty] 특성을 사용하면 자동으로 프로퍼티가 생성되므로 수동으로 프로퍼티를 만들면 충돌이 발생합니다.
        //해결 방법: partial void 메서드를 사용
        //[ObservableProperty] 를 그대로 두고, OnProgressValueChanged partial 메서드를 추가하세요:
        // ProgressValue가 변경될 때 자동 호출되는 메서드
        partial void OnProgressValueChanged(int value)
        {
            // Progress가 Max에 도달하면 자동으로 완료 처리
            if (value >= ProgressMax && ProgressMax > 0)
            {
                _isUploadCompleted = true;
            }
        }


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


                    // 파일 로드 완료 후 알림 표시
                    if (GridFileList.Count > 0)
                    {
                        ShowUploadReadyNotification();
                    }
                }
            }
        }


        [RelayCommand]
        //[DB 업로드]//
        //async void 메서드는 예외 처리와 테스트에 문제가 있어서, [RelayCommand] 특성이 붙은 메서드에서는 async Task 타입을 반환하도록 변경하는 것이 바람직합니다.
        private async Task OnUpload()
        {
            if (string.IsNullOrEmpty(FolderPath))
            {
                MessageBox.Show("먼저 폴더를 선택하세요.");
                return;
            }

                    // 업로드 시작 시 준비 완료 메시지 숨김
            IsUploadReady = false;

            IsUploading = true;//표시 설정
            ProgressValue = 0;
            ProgressText = "업로드 준비 중...";

            try
            {
                await Task.Run(() =>
                {
                    CsvUploader.UploadLandMoveToDB((current, total, message) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ProgressMax = total;
                            ProgressValue = current;
                            ProgressText = $"{message}";
                        });
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("업로드 중 오류 발생: " + ex.Message);
            }
            finally
            {
                // 업로드 완료 시 (ProgressValue == ProgressMax)
                IsUploadCompleted = true;
                IsCommitCompleted = false;
                IsUploading = false;//표시 해제
            }
        }


        [RelayCommand]
        //[DB 최종 업로드]//
        //async void 메서드는 예외 처리와 테스트에 문제가 있어서, [RelayCommand] 특성이 붙은 메서드에서는 async Task 타입을 반환하도록 변경하는 것이 바람직합니다.
        private async Task OnCommit()
        {
            IsCommiting = true;
            
            var message = $"최종 DB에 적용하시겠습니까?\n\n[업데이트 후 원복 불가]";
            var result = MessageBox.Show(message, "알림", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
            {
                await Task.Run(() =>
                {
                    int uploadCount = DBService.CommitLandMoveInfoOrg();
                    
                    //[TODO] 업로드 완료됐다는 확인을 어떤 항목으로 할지 정해야 함                    
                    if(uploadCount > 0)
                    {
                        MessageBox.Show("최종 DB에 업로드 완료했습니다!");
                    }
                });
            }

            // 커밋 완료 시
            IsCommitCompleted = true;
            IsCommiting = false;
        }


        // 날짜 포맷 변환 메서드
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


        // 업로드 준비 알림 표시
        private void ShowUploadReadyNotification()
        {
            IsUploadReady = true;

                    // 3초 후 자동으로 숨김
            //Task.Delay(3000).ContinueWith(_ =>
            //{
            //    Application.Current.Dispatcher.Invoke(() =>
            //    {
            //        IsUploadReady = false;
            //    });
            //});
        }

        partial void OnIsUploadCompletedChanged(bool value)
        {
            OnPropertyChanged(nameof(ShowCommitButton));
        }
        partial void OnIsCommitCompletedChanged(bool value)
        {
            OnPropertyChanged(nameof(ShowCommitButton));
        }
        #endregion
    }
}
