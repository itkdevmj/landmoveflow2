﻿using CommunityToolkit.Mvvm.Messaging;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.Diagram;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Editors.Internal;
using LMFS.Messages;
using LMFS.Models;
using LMFS.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LMFS.Views.Pages
{
    /// <summary>
    /// ProductsPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LandMoveSettingPage : Page
    {
        private readonly LandMoveSettingViewModel _viewModel;
        public LandMoveSettingViewModel ViewModel => _viewModel;
        public LandMoveSettingPage()
        {
            InitializeComponent();

            _viewModel = new LandMoveSettingViewModel();
            this.DataContext = _viewModel;
        }

        private void UpdateDataGridMaxHeight()
        {
            double windowHeight = this.ActualHeight;
            double otherControlsHeight = 30 + 10 + 10;
            double systemUiHeight = 40;
            double calculatedMaxHeight = windowHeight - otherControlsHeight - systemUiHeight;

            if (calculatedMaxHeight > 0)
            {
                //[TODO]//SettingGrid.MaxHeight = calculatedMaxHeight;
            }
        }

        public void SettingDefaultColor()
        {
            colorEdit1_1_1.EditValue = Color.FromArgb(255, 165, 165, 165); // #FFA5A5A5
            colorEdit1_1_2.EditValue = Color.FromArgb(255, 200, 200, 200);
            colorEdit1_1_3.EditValue = Colors.White;
            colorEdit1_2_1.EditValue = Color.FromArgb(255, 237, 125, 49);
            colorEdit1_2_2.EditValue = Color.FromArgb(255, 200, 200, 200);
            colorEdit1_2_3.EditValue = Colors.White;
            colorEdit2_1_1.EditValue = Colors.White;
            colorEdit2_1_2.EditValue = Colors.Black;
            colorEdit2_1_3.EditValue = Colors.Black;
            colorEdit2_2_1.EditValue = Colors.White;
            colorEdit2_2_2.EditValue = Colors.Green;
            colorEdit2_2_3.EditValue = Colors.Green;
            colorEdit3_1_1.EditValue = Color.FromArgb(255, 91, 155, 213);
            colorEdit3_2_1.EditValue = Color.FromArgb(255, 237, 125, 49);
        }


        private void LandMoveSettingPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateDataGridMaxHeight();

            //기본 색상 설정
            SettingDefaultColor();
        }

        private void LandMoveSettingPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDataGridMaxHeight();
        }

        //XAML 이벤트 직접 연결(EditValueChanged= "OnDiagramColorChanged") → Page.cs(코드비하인드) 에 정의!
        private void OnDiagramColorChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var popup = sender as PopupColorEdit;
            if (popup == null) return;

            Color selectedColor = popup.Color;

            // Name에서 row, col 추출: 예시 - "colorEdit2_4" → row=2, col=4
            var name = popup.Name;
            // 정규표현식 또는 Split 사용
            // "colorEdit2_4" -> ["colorEdit2", "4"], ["colorEdit", "2", "4"]
            int itm = 0, row = 0, col = 0;
            var parts = name.Replace("colorEdit", "").Split('_');
            if (parts.Length == 3)
            {
                int.TryParse(parts[0], out itm);
                int.TryParse(parts[1], out row);
                int.TryParse(parts[2], out col);
            }

            // ViewModel의 속성에 저장
            var vm = this.DataContext as LandMoveSettingViewModel;
            if (vm != null)
            {
                if (itm == 1)
                    vm.JibunColors[row - 1, col - 1] = selectedColor;
                else if (itm == 2)
                    vm.LabelColors[row - 1, col - 1] = selectedColor;
                else if (itm == 3)
                    vm.ConnectorColors[row - 1, col - 1] = selectedColor;
            }
        }

    }
}
