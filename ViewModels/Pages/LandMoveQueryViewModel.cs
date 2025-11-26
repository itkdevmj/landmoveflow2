using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Data.Browsing;
using DevExpress.Mvvm;
using DevExpress.Utils.About;
using DevExpress.Xpf.Grid;
using DevExpress.XtraScheduler.Native;
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
    public partial class LandMoveQueryViewModel : ObservableObject
    {
        public static readonly Logger _logger = LogManager.GetCurrentClassLogger();


        // (UI 바인딩과 동적 추가/삭제에 최적)
        [ObservableProperty] private List<UserHist> _gridQueryDataSource;


        public Action CloseAction { get; set; }
        public ICommand CloseCommand { get; }

        public ICommand QueryCommand { get; private set; }

        // 1. 생성자에 상위 VM을 파라미터로 받음
        public LandMoveQueryViewModel()
        {

            CloseCommand = new RelayCommand(ExecuteClose);
            QueryCommand = new DelegateCommand(OnQuery);
        }

        private void ExecuteClose()
        {
            CloseAction?.Invoke();
        }

        private void OnQuery()
        {
            //
        }

        public void FilterQuery()
        {
            GridQueryDataSource = DBService.QueryUserHistoryCurrentUser();

            var converter = new LandMoveFlowConverter();
            GridQueryDataSource = converter.ChangeCodeToNameBatch2(GridQueryDataSource);
        }
    }
}
