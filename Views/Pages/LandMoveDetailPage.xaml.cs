using CommunityToolkit.Mvvm.Messaging;
using DevExpress.Xpf.Core.Native;
using LMFS.Messages;
using LMFS.Models;
using LMFS.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace LMFS.Views.Pages
{
    /// <summary>
    /// ProductsPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LandMoveDetailPage : Page
    {
        //regDt : 정리일
        //rsn : 이동종목
        public LandMoveDetailPage(List<LandMoveInfo> sourceList, string regDt, string rsn)
        {
            InitializeComponent();
            var vm = new LandMoveDetailViewModel();
            this.DataContext = vm;
            vm.FilterDetail(sourceList, regDt, rsn);
        }

        private void UpdateDataGridMaxHeight()
        {
            double windowHeight = this.ActualHeight;
            double otherControlsHeight = 30 + 10 + 10;
            double systemUiHeight = 40;
            double calculatedMaxHeight = windowHeight - otherControlsHeight - systemUiHeight;

            if (calculatedMaxHeight > 0)
            {
                GridDetail.MaxHeight = calculatedMaxHeight;
            }
        }

        private void LandMoveDetailPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateDataGridMaxHeight();
        }

        private void LandMoveDetailPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDataGridMaxHeight();
        }        
     
    }
}
