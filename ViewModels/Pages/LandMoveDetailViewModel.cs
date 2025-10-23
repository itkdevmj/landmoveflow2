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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace LMFS.ViewModels.Pages
{
    public partial class LandMoveDetailViewModel : ObservableObject
    {
        public string ItemName { get; set; }
        public string RegDate { get; set; }
        public string Description { get; set; }

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

    }
}
