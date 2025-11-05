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
        //기존
        //[ObservableProperty] private List<LandMoveInfo> _gridDetailDataSource;
        // 변경 (UI 바인딩과 동적 추가/삭제에 최적)
        [ObservableProperty] private ObservableCollection<GridDetailItem> _gridDetailDataSource;
        [ObservableProperty] private string _regDt;
        [ObservableProperty] private string _rsn;


        // 저장 버튼 표시 여부 (처음에는 false)
        [ObservableProperty] private bool _isSaveButtonVisible = false;


        public Action CloseAction { get; set; }
        public ICommand CloseCommand { get; }

        public ICommand RowDoubleClickCommand { get; private set; }
        public ICommand AddRowCommand { get; private set; }
        // 저장 Command 추가
        public ICommand SaveChangesCommand { get; private set; }

        public LandMoveDetailViewModel()
        {
            CloseCommand = new RelayCommand(ExecuteClose);
            RowDoubleClickCommand = new DelegateCommand<MouseButtonEventArgs>(OnRowDoubleClick);
            AddRowCommand = new DelegateCommand(OnAddRow);

            // 저장 Command 초기화
            SaveChangesCommand = new DelegateCommand(OnSave);
        }

        private void ExecuteClose()
        {
            CloseAction?.Invoke();
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
        }

        private void OnRowDoubleClick(MouseButtonEventArgs e)
        {
            // 기존 더블클릭 로직
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

            GridDetailDataSource.Add(newItem);

            // 필지 추가 시 저장 버튼 표시
            IsSaveButtonVisible = true;
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
