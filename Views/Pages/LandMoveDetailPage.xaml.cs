using CommunityToolkit.Mvvm.Messaging;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.Grid;
using LMFS.Engine;
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
                GridControl.MaxHeight = calculatedMaxHeight; // GridDetail → gridControl로 수정
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

        // TableView_ShowingEditor 이벤트 핸들러 추가
        private void TableView_ShowingEditor(object sender, ShowingEditorEventArgs e)
        {
            //// '추가' 버튼 컬럼은 편집 불가
            //if (e.Column?.FieldName == "AddButton")
            //{
            //    e.Cancel = true;
            //    return;
            //}

            //기존행(AllowEditing == false)인 경우 편집을 막기
            // 필요시 특정 조건에서만 편집 허용
            // 예: 새로운 행만 편집 가능하도록
            var item = e.Row as LMFS.Engine.GridDetailItem;
            if (item != null && !item.IsNewRow /*&& !item.AllowEditing*/)
            {
                // 기존 행은 편집 제한 (필요에 따라 조정)
                e.Cancel = true;
            }
        }
    }
}