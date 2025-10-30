﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Data.Browsing;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using LMFS.Db;
using LMFS.Engine;
using LMFS.Models;
using LMFS.Services;
using LMFS.Views.Pages;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;

namespace LMFS.ViewModels.Pages
{
    public partial class LandMoveFlowViewModel : ObservableObject
    {
        public static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        
        [ObservableProperty] private List<SidoCode> _umdList;
        [ObservableProperty] private List<SidoCode> _riList;
        [ObservableProperty] private SidoCode _selectedUmd;
        [ObservableProperty] private SidoCode _selectedRi;
        [ObservableProperty] private bool _isSan;
        [ObservableProperty] private string _bobn;
        [ObservableProperty] private string _bubn;
        [ObservableProperty] private bool _jimokChg;//vm.JimokChg
        [ObservableProperty] private bool _portrait;//vm.Portrait
        //[ObservableProperty] private string _currentPnu;
        [ObservableProperty] private List<LandMoveInfo> _gridDataSource;
        [ObservableProperty] private List<LandMoveInfoCategory> _gridCategoryDataSource;
        //[ObservableProperty] private string _landMoveFlowData;
        [ObservableProperty] private MemoryStream _landMoveFlowData;

        private string _currentPnu;

        private bool _isOwnName;
        public bool IsOwnName
        {
            get => _isOwnName;
            set { _isOwnName = value; OnPropertyChanged(); }
        }

        private bool _isJimok;
        public bool IsJimok
        {
            get => _isJimok;
            set { _isJimok = value; OnPropertyChanged(); }
        }

        private bool _isArea;
        public bool IsArea
        {
            get => _isArea;
            set { _isArea = value; OnPropertyChanged(); }
        }

        //public event PropertyChangedEventHandler PropertyChanged;
        //protected void OnPropertyChanged(string propName)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        //}


        [RelayCommand]
        private void OnSearch()
        {
            // 1. PNU 구성
            // 2. 그리드 데이터 조회
            SearchLandMoveData();

            // 3. 그리드 데이터 처리
            UpdateFlowXml();
        }

        [RelayCommand]
        private void OnEnter()
        {
            this.OnSearch();
        }


        [RelayCommand]
        private void OnFlowDataDoubleClick(MouseButtonEventArgs arg)
        {
            var currentItem = (arg.Source as GridControl)?.CurrentItem;
            var item = currentItem as LandMoveInfo;


            System.Diagnostics.Debug.WriteLine($"행이 더블클릭됨: {item}");
        }

        [RelayCommand]
        //CommandParameter로 선택된 행의 데이터 받기
        private void OnRowClick(LandMoveInfoCategory cate)
        {
            if (cate != null)
            {
                var page = new LandMoveDetailPage(GridDataSource.ToList(), cate.regDt, cate.rsn);//생성자에 값 전달
                Window window = new Window
                {
                    Content = page,
                    Title = "토지이동흐름도 일자/종목별 필지정보(상세)",
                    Width = 800,
                    Height = 320,
                    Owner = Application.Current.MainWindow
                };
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;//부모화면의 가운데 표시
                window.ShowDialog();//부모화면 제어 불가
            }
        }

        [RelayCommand]
        //Diagram Color 설정화면으로 이동//
        private void OnSettingColor()
        {
            var page = new LandMoveSettingPage();
            Window window = new Window
            {
                Content = page,
                Title = "토지이동흐름도 다이어그램 색상 설정",
                Width = 340,
                Height = 240,
                Owner = Application.Current.MainWindow,

                //[닫기]버튼만 남긴다.
                WindowStyle = WindowStyle.SingleBorderWindow,
                ResizeMode = ResizeMode.NoResize
            };
            window.WindowStartupLocation = WindowStartupLocation.Manual;//부모화면의 임의위치로 지정

            // 부모(Window)의 실제 스크린 위치 좌표 구하기
            var parent = Application.Current.MainWindow;
            // 부모창의 스크린 좌표를 가져오기
            var parentTopLeft = parent.PointToScreen(new System.Windows.Point(0, 0));

            // 부모창의 우측 상단 위치에 새 창을 붙이기
            window.Left = parentTopLeft.X + parent.ActualWidth - window.Width - 10;
            window.Top = parentTopLeft.Y + 100;

            window.ShowDialog();//부모화면 제어 불가
        }

        [RelayCommand]
        //Diagram Color 설정화면으로 이동//
        private void OnLoadCsvFileData()
        {
            var page = new CsvUploaderPage();
            Window window = new Window
            {
                Content = page,
                Title = "토지이동흐름도 자료(CSV) 업로드",
                Width = 800,
                Height = 320,
                Owner = Application.Current.MainWindow,

                //[닫기]버튼만 남긴다.
                WindowStyle = WindowStyle.SingleBorderWindow,
                ResizeMode = ResizeMode.NoResize
            };
            //window.WindowStartupLocation = WindowStartupLocation.Manual;//부모화면의 임의위치로 지정

            //// 부모(Window)의 실제 스크린 위치 좌표 구하기
            //var parent = Application.Current.MainWindow;
            //// 부모창의 스크린 좌표를 가져오기
            //var parentTopLeft = parent.PointToScreen(new System.Windows.Point(0, 0));

            //// 부모창의 우측 상단 위치에 새 창을 붙이기
            //window.Left = parentTopLeft.X + parent.ActualWidth - window.Width - 10;
            //window.Top = parentTopLeft.Y + 100;

            window.ShowDialog();//부모화면 제어 불가
        }

        partial void OnSelectedUmdChanged(SidoCode value)
        {
            RiList = GenerateRiList(value!.umdCd);
            SelectedRi = RiList.FirstOrDefault()!;
        }

        public LandMoveFlowViewModel()
        {
            GetSidoCodeList();
            GetJimokCodeDictionary();
            GetReasonCodeDictionary();
        }
        
        private void GetSidoCodeList()
        {
            List<SidoCode> list = DBService.ListSidoCode(GlobalDataManager.Instance.loginUser.areaCd);
            GlobalDataManager.Instance.sidoCodeList = list;
            UmdList = GenerateUmdList();
            SelectedUmd = UmdList.FirstOrDefault()!;
        }


        private List<SidoCode> GenerateUmdList()
        {
            List<SidoCode> list = null;
            try
            {
                list = GlobalDataManager.Instance.sidoCodeList
                    .Where(x => x.riCd == "00" && x.umdNm != "").
                    OrderBy(x => x.sidoSggCd + x.umdCd + x.riCd).ToList();
            }
            catch (Exception e)
            {
                _logger.Debug(e.Message);
            }
            return list;
        }

        private List<SidoCode> GenerateRiList(string umdCd)
        {
            //Vit.G//
            Bobn = string.Empty;
            Bubn = string.Empty;


            List<SidoCode> list = null;
            try
            {
                list = GlobalDataManager.Instance.sidoCodeList
                    .Where(x => x.umdCd == umdCd && x.riCd != "00")
                    .OrderBy(x => x.sidoSggCd + x.umdCd + x.riCd).ToList();
            }
            catch (Exception ex)
            {
                _logger.Debug(ex.Message);
            }
            return list;
        }

        private void GetJimokCodeDictionary()
        {
            Dictionary<string, string> dict = DBService.GetJimokDictionary("CD01");
            GlobalDataManager.Instance.JimokCode = dict;
        }


        private void GetReasonCodeDictionary()
        {
            Dictionary<string, string> dict = DBService.GetReasonDictionary("CD02");
            GlobalDataManager.Instance.ReasonCode = dict;
        }


        partial void OnJimokChgChanged(bool value)
        {
            //// 체크박스 값이 변경될 때마다 실행
            //UpdateFlowXml();

            // 검색 로직 실행
            OnSearch();   // 또는 SearchCommand.Execute(null);
        }
        
        partial void OnPortraitChanged(bool value)
        {
            //// 체크박스 값이 변경될 때마다 실행
            //UpdateFlowXml();

            // 검색 로직 실행
            OnSearch();   // 또는 SearchCommand.Execute(null);
        }

        private string BuildPnu()
        {
            string umdCd = SelectedUmd.umdCd;
            string riCd = SelectedRi == null ? "00" : SelectedRi.riCd;
            string gbn = IsSan == true ? "2" : "1";
            string bobn = Bobn != null ? Bobn.PadLeft(4, '0') : "0000";
            string bubn = Bubn != null ? Bubn.PadLeft(4, '0') : "0000";

            string pnu = SelectedUmd.sidoSggCd + umdCd + riCd + gbn + bobn + bubn;

            try
            {
                if (pnu.Length != 19)
                {
                    _logger.Debug($"지번 오류 {pnu}");
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"{pnu} : {ex.Message}");
            }
            return pnu;
        }

        private void SearchLandMoveData()
        {
            _currentPnu = BuildPnu(); // 기존 방식
            GridDataSource = DBService.ListLandMoveHistory(_currentPnu);
            GridCategoryDataSource = DBService.ListLandMoveCategory(_currentPnu);
        }

        private void UpdateFlowXml()
        {
            //251027//[색상설정 - 사용자정의]
            var viewModel = new LandMoveSettingViewModel();

            var converter = new LandMoveFlowConverter(viewModel);

            var filteredList = GridDataSource;
            var categoryList = GridCategoryDataSource;
            if (!JimokChg)
            {
                filteredList = GridDataSource.Where(item => item.rsn != "40").ToList();
                GridDataSource = filteredList;
                categoryList = GridCategoryDataSource.Where(item => item.rsn != "40").ToList();
                GridCategoryDataSource = categoryList;
            }

            ////Vit.G//TEST// 3th argu //
            if (filteredList.Count > 0)
            {                
                XDocument rtnXml = converter.Run(filteredList, this, categoryList, _currentPnu);

                // ... 이하 xml 스트림 처리
                string str = rtnXml.ToString();

                byte[] xmlBytes;
                using (var ms = new MemoryStream())
                {
                    rtnXml.Save(ms);
                    xmlBytes = ms.ToArray();
                }
                using (var stream = new MemoryStream(xmlBytes))
                {
                    LandMoveFlowData = stream;
                }
            }//if (filteredList.Count > 0)
        }

    }
}
