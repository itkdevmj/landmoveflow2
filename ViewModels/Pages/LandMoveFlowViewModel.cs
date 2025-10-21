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
        [ObservableProperty] private bool _jimokChgShow;
        [ObservableProperty] private bool _portrait;
        [ObservableProperty] private List<LandMoveInfo> _gridDataSource;
        //[ObservableProperty] private string _landMoveFlowData;
        [ObservableProperty] private MemoryStream _landMoveFlowData;

        [RelayCommand]
        private void OnSearch()
        {
            string pnu = "";
            // string pnu = "4420035028104190033";
            
            try
            {
                string umdCd = SelectedUmd.umdCd;
                string riCd = SelectedRi == null ? "00" : SelectedRi.riCd;
                string gbn = IsSan == true ? "2" : "1";
                string bobn = Bobn != null ? Bobn.PadLeft(4, '0') : "0000";
                string bubn = Bubn != null ? Bubn.PadLeft(4, '0') : "0000";
                bool jimokChgShow = JimokChgShow;//Vit.G//TEST// ;
                bool portrait = Portrait;//Vit.G//TEST// ;

                pnu = SelectedUmd.sidoSggCd + umdCd + riCd + gbn + bobn + bubn;

                if (pnu.Length != 19)
                {
                    _logger.Debug($"지번 오류 {pnu}");
                }
                else
                {
                    // 그리드 데이터 조회
                    List<LandMoveInfo> rtnList = DBService.ListLandMoveHistory(pnu);
                    //Org//GridDataSource = rtnList;
                    var filteredList = rtnList;
                    if(!jimokChgShow)
                        filteredList = rtnList.Where(item => item.rsn != "40").ToList();//40=>지목변경
                    GridDataSource = filteredList;
                    
                    // 흐름도 데이터 조회
                    var converter = new LandMoveFlowConverter();
                    
                    //Vit.G//TEST// 3th argu //
                    XDocument rtnXml = converter.Run(rtnList, pnu, jimokChgShow, portrait);//Vit.G//[add]pnu, jimokChgShow, portrait
                    string str = rtnXml.ToString();
                    
                    
                    byte[] xmlBytes;
                    using (var ms = new MemoryStream())
                    {
                        rtnXml.Save(ms);
                        xmlBytes = ms.ToArray();
                    }
                    using (var stream = new MemoryStream(xmlBytes)) {
                        LandMoveFlowData = stream;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug(pnu);    
            }
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

        

        partial void OnSelectedUmdChanged(SidoCode? data)
        {
            RiList = GenerateRiList(data!.umdCd);
            SelectedRi = RiList.FirstOrDefault()!;
        }

        public LandMoveFlowViewModel()
        {
            GetSidoCodeList();
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

        [RelayCommand]
        private void OnJimokChecked()
        {
            this.OnSearch();
        }

        public void ExecuteSearchCommand() => SearchCommand.Execute(null);
    }
}
