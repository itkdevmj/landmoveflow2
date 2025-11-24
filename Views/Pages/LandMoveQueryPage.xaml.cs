using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.Grid;
using LMFS.Engine;
using LMFS.Messages;
using LMFS.Models;
using LMFS.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace LMFS.Views.Pages
{
    //------------------------------------------------------
    //
    // Page는 View이므로[ObservableProperty]를 쓰지 마세요
    // ViewModel 쪽에서만 [ObservableProperty]를 사용하는 게 일반적입니다.
    //
    //------------------------------------------------------
    public partial class LandMoveQueryPage : Page
    {
        public LandMoveQueryPage()
        {
            InitializeComponent();
        }

        private void UpdateDataGridMaxHeight()
        {
            double windowHeight = this.ActualHeight;
            double otherControlsHeight = 30 + 10 + 10;
            double systemUiHeight = 40;
            double calculatedMaxHeight = windowHeight - otherControlsHeight - systemUiHeight;

            if (calculatedMaxHeight > 0)
            {
                GridControl.MaxHeight = calculatedMaxHeight; // GridQuery → gridControl로 수정
            }
        }

        private void LandMoveQueryPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateDataGridMaxHeight();
        }

        private void LandMoveQueryPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDataGridMaxHeight();
        }

    }
}