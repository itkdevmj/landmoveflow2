using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Data.Browsing;
using DevExpress.Dialogs.Core.View;
using DevExpress.Xpf.Diagram;
using DevExpress.Xpf.Grid;
using LMFS.Db;
using LMFS.Engine;
using LMFS.Models;
using LMFS.Services;
using LMFS.Views.Pages;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;// RoutedEventArgs가 여기 포함됨
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace LMFS.ViewModels.Pages
{
    public partial class LandMoveSettingViewModel : ObservableObject
    {
        [ObservableProperty] private Color[,] jibunColors = new Color[2, 3];//basic, query : foreground, background, textcolor        
        [ObservableProperty] private Color[,] labelColors = new Color[2, 3];//basic, query : foreground, background, textcolor
        [ObservableProperty] private Color[,] connectorColors = new Color[2, 1];//basic, query
        
        [ObservableProperty] private LandMoveFlowViewModel _flowVM;
        [ObservableProperty] private Color color1_1_1;
        [ObservableProperty] private Color color1_1_2;
        [ObservableProperty] private Color color1_1_3;
        [ObservableProperty] private Color color1_2_1;
        [ObservableProperty] private Color color1_2_2;
        [ObservableProperty] private Color color1_2_3;
        [ObservableProperty] private Color color2_1_1;
        [ObservableProperty] private Color color2_1_2;
        [ObservableProperty] private Color color2_1_3;
        [ObservableProperty] private Color color2_2_1;
        [ObservableProperty] private Color color2_2_2;
        [ObservableProperty] private Color color2_2_3;
        [ObservableProperty] private Color color3_1_1;
        [ObservableProperty] private Color color3_2_1;

        public DiagramControl DiagramControl { get; set; }

        public Action CloseAction { get; set; }
        public ICommand CloseCommand { get; }

        public LandMoveSettingViewModel(/*LandMoveFlowViewModel flowViewModel*/)
        {
            CloseCommand = new RelayCommand(ExecuteClose);
            //FlowVM = flowViewModel;

            //Color 변수 생성
            //InitSettingColor();
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


        //251027//private => public : Converter에서 참조//
        public void SettingDefaultColor()
        {
            Color1_1_1 = Color.FromArgb(255, 165, 165, 165); // #FFA5A5A5
            Color1_1_2 = Colors.LightGray; //251110//Exception//Color1_1_2 = Color.FromArgb(255, 200, 200, 200);
            Color1_1_3 = Colors.White;
            Color1_2_1 = Color.FromArgb(255, 237, 125, 49);
            Color1_2_2 = Colors.LightGray; //251110//Exception//Color.FromArgb(255, 200, 200, 200);
            Color1_2_3 = Colors.White;
            Color2_1_1 = Colors.White;
            Color2_1_2 = Colors.Black;
            Color2_1_3 = Colors.Black;
            Color2_2_1 = Colors.White;
            Color2_2_2 = Colors.Green;
            Color2_2_3 = Colors.Green;
            Color3_1_1 = Color.FromArgb(255, 91, 155, 213);
            Color3_2_1 = Color.FromArgb(255, 237, 125, 49);
        }

        public void InitSettingColor()
        {
            //JibunColors = new Color[2, 3];
            //LabelColors = new Color[2, 3];
            //ConnectorColors = new Color[2, 1];
        }

        public void GetSettingColor()
        {            
            JibunColors[0, 0] = Color1_1_1;
            JibunColors[0, 1] = Color1_1_2;
            JibunColors[0, 2] = Color1_1_3;
            JibunColors[1, 0] = Color1_2_1;
            JibunColors[1, 1] = Color1_2_2;
            JibunColors[1, 2] = Color1_2_3;
            LabelColors[0, 0] = Color2_1_1;
            LabelColors[0, 1] = Color2_1_2;
            LabelColors[0, 2] = Color2_1_3;
            LabelColors[1, 0] = Color2_2_1;
            LabelColors[1, 1] = Color2_2_2;
            LabelColors[1, 2] = Color2_2_3;
            ConnectorColors[0, 0] = Color3_1_1;
            ConnectorColors[1, 0] = Color3_2_1;
        }

        [RelayCommand]
        //Diagram Color 변경내용 저장//
        private void OnSaveColor()
        {
            //색상 저장 로직
            // 개별 Color 프로퍼티들의 값을 색상 배열에 반영
            GetSettingColor();

            //// Converter 등이 최신 Setting 값을 참조/반영
            FlowVM?.Converter?.UpdateWithNewSetting(FlowVM.SettingVM);
            FlowVM?.Converter?.MakeXmlData(); // 다이어그램 다시 그림


            // FlowVM 등 다른 참조 객체들에 값 변경 알림
            //FlowVM?.UpdateColorFromSetting(); // 추가 메서드 구현 (아래 예시)

            //XML 생성 및 저장
            //251117//new LandMoveFlowConverter(this).MakeXmlData();
            //FlowVM.Converter.MakeXmlData();
        }
    }
}
