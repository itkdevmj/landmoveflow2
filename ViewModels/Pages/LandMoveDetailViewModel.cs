using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Data.Browsing;
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

        private string _regDt;
        public string RegDt
        {
            get => _regDt;
            set { _regDt = value; OnPropertyChanged(nameof(RegDt)); }
        }

        private string _rsn;
        public string Rsn
        {
            get => _rsn;
            set { _rsn = value; OnPropertyChanged(nameof(Rsn)); }
        }

        //public event PropertyChangedEventHandler PropertyChanged;
        //protected void OnPropertyChanged(string name)
        //    => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));


        public Action CloseAction { get; set; }
        public ICommand CloseCommand { get; }

        public LandMoveDetailViewModel()
        {
            CloseCommand = new RelayCommand(ExecuteClose);
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

    }
}
