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
    public partial class LandMoveDetailPage : Page
    {
        //[ObservableProperty](CommunityToolkit.Mvvm)는
        //기본적으로 [ObservableObject] 또는 INotifyPropertyChanged를
        //상속/특성이 적용된 클래스에서만 동작합니다.
        public LandMoveDetailViewModel DetailVM { get; private set; }

        //regDt : 정리일
        //rsn : 이동종목
        public LandMoveDetailPage(LandMoveFlowViewModel flowVM, List<LandMoveInfo> sourceList, string regDt, string rsn)
        {
            InitializeComponent();

            DetailVM = new LandMoveDetailViewModel(flowVM);

            // 필수! 아래처럼 할당
            // Page에서는 이렇게!
            DetailVM.CloseAction = () => this.NavigationService?.GoBack();

            this.DataContext = DetailVM;
            DetailVM.FilterDetail(sourceList, regDt, rsn);
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

            //합병 & 이동후, 분할 & 이동전 컬럼 편집 불가
            if (DetailVM.Rsn == "합병" && e.Column?.FieldName == "AfPnu") e.Cancel = true;
            else if (DetailVM.Rsn == "분할" && e.Column?.FieldName == "BfPnu") e.Cancel = true;

        }
    }
}