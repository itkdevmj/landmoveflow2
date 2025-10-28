using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Data.Browsing;
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

        public Color Color1_1_1 { get; set; }
        public Color Color1_1_2 { get; set; }
        public Color Color1_1_3 { get; set; }
        public Color Color1_2_1 { get; set; }
        public Color Color1_2_2 { get; set; }
        public Color Color1_2_3 { get; set; }
        public Color Color2_1_1 { get; set; }
        public Color Color2_1_2 { get; set; }
        public Color Color2_1_3 { get; set; }
        public Color Color2_2_1 { get; set; }
        public Color Color2_2_2 { get; set; }
        public Color Color2_2_3 { get; set; }
        public Color Color3_1_1 { get; set; }
        public Color Color3_2_1 { get; set; }


        public DiagramControl DiagramControl { get; set; }

        public Action CloseAction { get; set; }
        public ICommand CloseCommand { get; }

        public LandMoveSettingViewModel()
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


        //251027//private => public : Converter에서 참조//
        public void SettingDefaultColor()
        {
            Color1_1_1 = Color.FromArgb(255, 165, 165, 165); // #FFA5A5A5
            Color1_1_2 = Color.FromArgb(255, 200, 200, 200);
            Color1_1_3 = Colors.White;
            Color1_2_1 = Color.FromArgb(255, 237, 125, 49);
            Color1_2_2 = Color.FromArgb(255, 200, 200, 200);
            Color1_2_3 = Colors.White;
            Color2_1_1 = Colors.White;
            Color2_1_2 = Colors.Black;
            Color2_1_3 = Colors.Black;
            Color2_2_1 = Colors.White;
            Color2_2_2 = Colors.Green;
            Color2_2_3 = Colors.Green;
            Color3_1_1 = Color.FromArgb(255, 91, 155, 213);
            Color3_2_1 = Color.FromArgb(255, 237, 125, 49);

            JibunColors = new Color[,]
            { 
                { Color1_1_1, Color1_1_2, Color1_1_3}, 
                { Color1_2_1, Color1_2_2, Color1_2_3} 
            };

            LabelColors = new Color[,]
            {
                { Color2_1_1, Color2_1_2, Color2_1_3},
                { Color2_2_1, Color2_2_2, Color2_2_3}
            };

            connectorColors = new Color[,]
            {
                { Color3_1_1},
                { Color3_2_1}
            };

        }

        [RelayCommand]
        //Diagram Color 변경내용 저장//
        private void SaveColor()
        {
            //색상 저장 로직
            //...
            //XML 생성 및 저장
            new LandMoveFlowConverter(this).MakeXmlData();
        }
    }
}
