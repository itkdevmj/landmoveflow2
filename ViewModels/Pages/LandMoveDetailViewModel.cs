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
using System.Windows;
using System.Windows.Input;

namespace LMFS.ViewModels.Pages
{
    public partial class LandMoveDetailViewModel : ObservableObject
    {
        [ObservableProperty] private List<LandMoveInfo> _gridDetailDataSource;
        [ObservableProperty] private string _regDt;
        [ObservableProperty] private string _rsn;


        public Action CloseAction { get; set; }
        public ICommand CloseCommand { get; }
        public ICommand RowDoubleClickCommand { get; private set; }
        public ICommand AddRowCommand { get; private set; }
        public ICommand ConfirmAddCommand { get; private set; }


        public LandMoveDetailViewModel()
        {
            CloseCommand = new RelayCommand(ExecuteClose);

            RowDoubleClickCommand = new DelegateCommand<MouseButtonEventArgs>(OnRowDoubleClick);
            AddRowCommand = new DelegateCommand(OnAddRow);
            ConfirmAddCommand = new DelegateCommand<GridDetailItem>(OnConfirmAdd);

        }

        private void ExecuteClose()
        {
            throw new NotImplementedException();
        }

        //private void ExecuteClose(object obj)
        //{
        //    // View에서 전달된 action을 실행하여 창 닫기
        //    CloseAction?.Invoke();
        //}

        public void FilterDetail(List<LandMoveInfo> sourceList, string regDt, string rsn)
        {
            var filtered = sourceList
                .Where(x => x.regDt == regDt && x.rsn == rsn)
                .ToList();

            DateTime parsedDate = DateTime.ParseExact(regDt, "yyyyMMdd", null);
            RegDt = parsedDate.ToString("yyyy-MM-dd");
            RegDt = $"정리일자 : {RegDt}";
            Rsn = $"이동종목 : {rsn}";

            GridDetailDataSource = filtered;
        }

        private void OnRowDoubleClick(MouseButtonEventArgs e)
        {
            // 기존 더블클릭 로직
        }

        private void OnAddRow()
        {
            // 새 행 추가
            var newItem = new GridDetailItem
            {
                IsNewRow = true,
                BfPnu = "",
                AfPnu = "",
                BfJimok = "",
                BfArea = "",
                AfJimok = "",
                AfArea = "",
                OwnName = ""
            };

            GridDetailDataSource.Add(newItem);
        }

        private void OnConfirmAdd(GridDetailItem item)
        {
            if (item == null) return;

            // 유효성 검사
            if (string.IsNullOrWhiteSpace(item.BfPnu) ||
                string.IsNullOrWhiteSpace(item.AfPnu))
            {
                MessageBox.Show("필수 항목을 입력해주세요.", "알림",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 추가 확정 - IsNewRow를 false로 변경하여 버튼 숨김
            item.IsNewRow = false;

            // 데이터베이스 저장 등 추가 로직
            SaveToDatabase(item);
        }

        private void SaveToDatabase(GridDetailItem item)
        {
            // 실제 저장 로직 구현
        }

    }
}
