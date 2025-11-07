using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Data.Browsing;
using DevExpress.Mvvm;
using DevExpress.Xpf.Grid;
using LMFS.Db;
using LMFS.Engine;
using LMFS.Models;
using LMFS.Services;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace LMFS.ViewModels.Pages
{
    public partial class LandMoveDetailViewModel : ObservableObject
    {
        #region 기능설명 가이드        
        /*
        1. 기존 행들은 편집 불가, 마지막 컬럼엔 '추가'버튼은 숨기고 빈 컬럼만 표시        
        View (XAML > GridControl.Columns)
        기존 행(= IsNewRow==false, IsModified==false)은 CellTemplate의 Button을 숨김 처리
        AllowEditing 속성을 TableView 전체 또는 기존행엔 False로 설정

        2. 이미 구성된 행들(기존 데이터)는 '추가' 버튼 불필요
        위 XAML에서 IsNewRow==false면 버튼 미노출로 처리됨 (위와 동일)

        3. 우클릭 시 '필지 추가' 팝업/버튼 노출
        View XAML - GridControl 이벤트에 ContextMenu 추가
        ViewModel - AddRowCommand가 새 행(맨 마지막, IsNewRow==true)을 추가

        4. '필지 추가'로 마지막에 새 행 추가
        ViewModel (LandMoveDetailViewModel.cs)의 OnAddRow 구현됨
        추가 시 IsNewRow = true이고, AllowEditing True로 바뀌어야 하며, 그외 기존 행은 AllowEditing = false

        5. 추가된 마지막 행은 '추가'버튼 표시
        이미 XAML의 Button Visibility에서 IsNewRow==true일 때만 표시

        6. '추가'버튼 누르면 DB에서 필지 조회
        ConfirmAddCommand 커맨드를 ViewModel에 구현해야 함(아직 없음)
        Button.Command로 ConfirmAddCommand 바인딩
        ConfirmAddCommand에서 팝업 또는 Dialog로 필요값 입력받아 DB조회 후 갱신(미구현 시 MessageBox/Toast + 저장버튼 활성 처리 로 임시 구현)

        7. 조회된 데이터가 없다면, 상단 우측 '변경사항 저장' 버튼 표시
        ConfirmAddCommand/DB조회에서 결과 없을 시, IsSaveButtonVisible = true 처리

        8. '변경사항 저장' 수행 후 버튼 미표시
        ViewModel의 OnSave가 저장 후 IsSaveButtonVisible = false 처리
         */
        #endregion


        //기존
        //[ObservableProperty] private List<LandMoveInfo> _gridDetailDataSource;
        // 변경 (UI 바인딩과 동적 추가/삭제에 최적)
        [ObservableProperty] private ObservableCollection<GridDetailItem> _gridDetailDataSource;
        [ObservableProperty] private string _regDt;
        [ObservableProperty] private string _rsn;
        // 저장 버튼 표시 여부 (처음에는 false)
        [ObservableProperty] private bool _isSaveButtonVisible = false;
        [ObservableProperty] private bool _isTracking = false;
        


        public Action CloseAction { get; set; }
        public ICommand CloseCommand { get; }

        public ICommand RowDoubleClickCommand { get; private set; }
        public ICommand AddRowCommand { get; private set; }
        public ICommand ConfirmAddCommand { get; }
        // 저장 Command 추가
        public ICommand SaveChangesCommand { get; private set; }



        public LandMoveDetailViewModel()
        {
            CloseCommand = new RelayCommand(ExecuteClose);
            RowDoubleClickCommand = new DelegateCommand<MouseButtonEventArgs>(OnRowDoubleClick);
            AddRowCommand = new DelegateCommand(OnAddRow);

            ConfirmAddCommand = new DelegateCommand<GridDetailItem>(OnConfirmAdd);

            // 저장 Command 초기화
            SaveChangesCommand = new DelegateCommand(OnSave);
        }

        private void ExecuteClose()
        {
            CloseAction?.Invoke();
        }

        // GridDetailItem.cs - PropertyChanged 추적 활성화//새 행 추가 시 변경 추적 활성화
        public void EnableTracking()
        {
            IsTracking = true;
        }

        public void FilterDetail(List<LandMoveInfo> sourceList, string regDt, string rsn)
        {
            var filtered = sourceList
                .Where(x => x.regDt == regDt && x.rsn == rsn)
                .ToList();

            DateTime parsedDate = DateTime.ParseExact(regDt, "yyyyMMdd", null);
            RegDt = parsedDate.ToString("yyyy-MM-dd");
            RegDt = $"정리일자 : {RegDt}";
            Rsn = $"이동종목 : {rsn}";

            //기존
            //GridDetailDataSource = filtered;
            // GridDetailItem으로 변환하여 할당
            GridDetailDataSource = new ObservableCollection<GridDetailItem>(
                filtered.Select(GridDetailItem.FromLandMoveInfo)
            );


            // 기존 데이터는 추적 비활성화 (저장 버튼 표시 안 함)
            foreach (var item in GridDetailDataSource)
            {
                item.PropertyChanged += OnItemPropertyChanged;
            }

            // 저장 버튼 초기 숨김
            IsSaveButtonVisible = false;
        }

        private void OnRowDoubleClick(MouseButtonEventArgs e)
        {
            // 기존 더블클릭 로직
        }

        // 항목 변경 감지
        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(GridDetailItem.IsNewRow) &&
                e.PropertyName != nameof(GridDetailItem.IsModified))
            {
                IsSaveButtonVisible = true;
            }
        }

        //그리드 우클릭 [필지 추가] 선택
        private void OnAddRow()
        {
            // 새 행 추가
            var newItem = new GridDetailItem
            {
                IsNewRow = true,
                BfPnu = "",
                AfPnu = "",
                BfJimok = "",
                BfArea = 0.0,
                AfJimok = "",
                AfArea = 0.0,
                OwnName = ""
            };
            
            newItem.PropertyChanged += OnItemPropertyChanged;
            GridDetailDataSource.Add(newItem);

            // 기존행들은
            foreach (var item in GridDetailDataSource)
            {
                if (!item.IsNewRow)
                {
                    item.AllowEditing = false;
                }
            }

            newItem.EnableTracking(); // 추적 활성화

            IsSaveButtonVisible = true;

            // 필지 추가 시 저장 버튼 표시
            IsSaveButtonVisible = true;
        }

        //그리드 마지막 행 - 마지막 컬럼 [추가] 선택
        private void OnConfirmAdd(GridDetailItem item)
        {
            //[TODO]
            //// DB 조회 로직 수행 (예시)
            //var result = SearchFromDb(item);
            //if (result == null)
            //{
            //    IsSaveButtonVisible = true; // 조회 실패시 저장 버튼 노출
            //    MessageBox.Show("필지 정보 없음, 변경사항 저장 필요", "안내");
            //}
            //else
            //{
            //    // 값 채워넣고 IsSaveButtonVisible 처리
            //}
        }


        // 저장 메서드 구현
        private void OnSave()
        {
            try
            {
                bool hasChanges = false;

                foreach (var item in GridDetailDataSource)
                {
                    if (!item.Validate())
                    {
                        MessageBox.Show("유효성 검사 실패\n필수 항목을 입력해주세요.", "알림",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (item.IsNewRow)
                    {
                        // 신규 추가
                        SaveToDatabase(item);
                        item.IsNewRow = false; // 저장 후 플래그 변경
                        hasChanges = true;
                    }
                    else if (item.IsModified) // GridDetailItem에 IsModified 속성이 있다면
                    {
                        // 기존 데이터 업데이트
                        UpdateDatabase(item);
                        item.IsModified = false;
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    MessageBox.Show("저장 완료", "알림",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }


                // 저장 후 버튼 숨김
                IsSaveButtonVisible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveToDatabase(GridDetailItem item)
        {
            // 실제 DB 저장 로직
            var landMoveInfo = item.ToLandMoveInfo();

            // TODO: DB Insert 코드 구현
            // 예: _service.InsertLandMoveInfo(landMoveInfo);

        }

        private void UpdateDatabase(GridDetailItem item)
        {
            // 실제 DB 업데이트 로직
            var landMoveInfo = item.ToLandMoveInfo();

            // TODO: DB Update 코드 구현
            // 예: _service.UpdateLandMoveInfo(landMoveInfo);
        }

    }
}
