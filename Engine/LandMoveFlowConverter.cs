using DevExpress.CodeParser;
using DevExpress.DataAccess.Native.Data;
using DevExpress.Diagram.Core.Native;
using DevExpress.Xpf.CodeView;
using DevExpress.Xpf.Diagram;
using DevExpress.Xpo;
using DevExpress.XtraPrinting.XamlExport;
using DevExpress.XtraScheduler.Drawing;
using DevExpress.XtraSpreadsheet.DocumentFormats.Xlsb;
using DevExpress.XtraSpreadsheet.Model;
using LMFS.Models;
using LMFS.Services;
using LMFS.ViewModels.Pages;
using LMFS.Views.Pages;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

namespace LMFS.Engine;

public class LandMoveFlowConverter
{
    public static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    public static LandMoveSettingViewModel _settingVM;//251027//


    // ===========================================================
    // 개요
    // 데이터 원본 : DBMS
    // 작업 결과 파일 종류 : XML (GroupNumber.xml)
    // 작업 결과물 저장 위치 : RESULT/XML
    // '그룹화된 토지이동연혁' 정보 중 그룹 목록 조회
    // 그룹 목록을 한 그룹씩 세부 목록을 조회
    // 세부 목록 기준으로 XML 구성 후 저장
    // ===========================================================

    #region 변수 선언 (Fields)

    // -----------------------------------------------------------
    // 코드 조회 결과 저장용 (내부)
    // -----------------------------------------------------------
    //private Dictionary<string, string> _listLawd = new();
    private List<SidoCode> _listLawd = new();
    private Dictionary<string, string> _listJimok = new();
    private Dictionary<string, string> _listMovRsn = new();

    // -----------------------------------------------------------
    // 코드 조회 결과 저장용 (외부)
    // -----------------------------------------------------------
    public Dictionary<string, string> ListMovRsn => _listMovRsn;

    // -----------------------------------------------------------
    // DataFrame 대신 사용할 DataTable
    // -----------------------------------------------------------
    private System.Data.DataTable _dfXml = new();
    private System.Data.DataTable _dfPnu = new();

    // -----------------------------------------------------------
    // XML 아이템 노드 구성용 변수
    // -----------------------------------------------------------
    //[가로형 그리기]
    private const int StartX = 30;
    private const int StartY = 60;
    private const int ShapeW = 130;
    private const int ShapeH = 40;
    private const int ShapeGap = 10;
    private const int labelGap = ShapeGap;
    private const int ConnectorW = ShapeW - 10;
    // private const int ConnectorH = ShapeH + ShapeGap;
    private const int LabelW = ConnectorW - ShapeGap * 2;
    private const int LabelH = ShapeH - ShapeGap;    
    private const int FontSize = 8;
    private readonly int ShapeBet = ShapeH + ShapeGap;

    //[세로형 그리기]
    private const int P_ShapeW = 130;
    private const int P_ShapeH = 40;
    private const int P_ShapeGap = 10;
    private const int P_labelGap = P_ShapeGap;
    private const int P_ConnectorW = P_ShapeW + P_ShapeGap;//130+10
    private const int P_ConnectorH = P_ShapeH + P_ShapeGap * 2;//40+10*2
    private const int P_LabelW = P_ConnectorW - P_ShapeGap * 2;//140-10*2
    private const int P_LabelH = P_ShapeH;//40
    private const int P_FontSize = 8;
    private readonly int P_ShapeBet = P_ConnectorW;// L_ShapeH + L_ShapeGap;//40+10*2


    private List<string> _pnuList = new();
    private List<string> _jibunList = new();
    private List<string> _labelList = new();
    private List<string> _depthList = new();
    private List<string> _itemList = new();

    private List<(int x, int y, string label, bool focus)> _labelTuples = new(); // 라벨을 마지막에 그리기 위해 임시 저장//Vit.G//[add]focus

    private int _shapeCount;
    private int _labelCount;
    private int _depthCount;

    // -----------------------------------------------------------
    // 외부로부터 받은 인수
    // -----------------------------------------------------------
    private string _pnu;//Vit.G//조회 필지코드(19자리)
    private bool _isJimokChg;
    private bool _isPortrait;
    private bool _isOwnName;
    private bool _isJimok;
    private bool _isArea;

    // -----------------------------------------------------------
    // DBMS 조회 데이터 및 그룹 정보
    // -----------------------------------------------------------
    private List<int> _groupList = new();
    private int _currentGroupNo;
    private List<string> _dbLines = new();


    // -----------------------------------------------------------
    // 임시 파일 관련 정보
    // -----------------------------------------------------------
    private static string _tempDir;

    // -----------------------------------------------------------
    // XML 노드 구성용 변수
    // -----------------------------------------------------------
    private XElement _root;
    private XElement _children;
    private XDocument _xdoc;

    #endregion


    #region 생성자     ----------------------------------------

    //Page를 직접 접근하지 않고, ViewModel을 통해서 접근하기//
    public LandMoveFlowConverter(LandMoveSettingViewModel settingViewModel)
    {
        _settingVM = settingViewModel;// 기존 인스턴스만 보관
        //필요한 데이터는 _settings에서 가져옴
    }

    public LandMoveFlowConverter()
    {
    }
    #endregion 생성자  ----------------------------------------



    #region 데이터 조회 및 가공


    private void ReadCodeTables()
    {
        _listLawd = GlobalDataManager.Instance.sidoCodeList;
        _listJimok = GlobalDataManager.Instance.JimokCode;
        _listMovRsn = GlobalDataManager.Instance.ReasonCode;
    }

    public string GetCodeValue(int opt, string find)//private => private
    {
        return opt switch
        {
            //1 => _listLawd.GetValueOrDefault(find, ""), // LAWD
            //코드(key) => 명칭(value)
            2 => _listJimok.GetValueOrDefault(find, ""), // JIMOK
            3 => _listMovRsn.GetValueOrDefault(find, ""), // LAND_MOV_RSN
            //명칭(value) => 코드(key)
            4 => _listJimok.FirstOrDefault(kvp => kvp.Value == find).Key ?? "", // value => key (LINQ)
            5 => _listMovRsn.FirstOrDefault(kvp => kvp.Value == find).Key ?? "", // value => key (LINQ)
            _ => ""
        };
    }

    private void GetCodeValueCategory(List<LandMoveInfoCategory> categoryList)
    {        
        foreach (var row in categoryList)
        {
            row.rsn = GetCodeValue(3, row.rsn);//이동종목 코드 => 명칭
        }
    }

    //option (1)지역명(동 or 리)
    //       (2)지번만
    public string GetJibun(string landCd, int option)//251106//private => public
    {
        if (landCd.Length < 19) return landCd;

        var lawdCd = landCd.Substring(5, 5);
        string lawdNm = "";

        if (option == 1)//지역명(동 or 리))
        {
            foreach (var item in _listLawd)
            {
                if (item.umdCd + item.riCd == lawdCd)
                {
                    lawdNm = item.riCd == "00" ? item.umdNm + " " : item.riNm + " ";//Vit.G//[add]'동지역'
                    break;
                }
            }
        }
        else if (option == 2)//지역명(동 or 읍면동 + 리))
        {
            foreach (var item in _listLawd)
            {
                if (item.umdCd + item.riCd == lawdCd)
                {
                    lawdNm = item.riCd == "00" ? item.umdNm + " " : item.umdNm + " " + item.riNm + " ";//Vit.G//[add]'동지역'
                    break;
                }
            }
        }

        var bonbun = landCd.Substring(11, 4).TrimStart('0');
        var bubun = landCd.Substring(15, 4).TrimStart('0');
        var jibun = string.IsNullOrEmpty(bubun) ? bonbun : $"{bonbun}-{bubun}";
        if (landCd.Substring(10, 1) == "2")
        {
            jibun = "산 " + jibun;
        }
        return $"{lawdNm}{jibun}";
        //return $"{jibun}";
    }

    private void ProcessLandMoveFlow(List<LandMoveInfo> rtnList, LandMoveFlowViewModel vm)
    {
        // 각종 변수 초기화
        InitializeForNewGroup();

        //LandMoveFlowViewModel에서 가져온 값을 설정
        SetExternalVariables(vm);

        //for Saving _dfXml//
        _currentGroupNo = rtnList.First().gSeq;

        // 데이터 분석 > XML 구성 > XML 저장
        AnalyzeData(rtnList);
    }
    #endregion

    private void SetExternalVariables(LandMoveFlowViewModel vm)
    {
        _isJimokChg = vm.JimokChg;
        _isPortrait = vm.Portrait;
        _isOwnName = vm.IsOwnName;
        _isJimok = vm.IsJimok;
        _isArea = vm.IsArea;
    }


    #region 데이터 분석 및 처리
    public void InitializeForNewGroup()//private => public
    {
        InitializeDataListForNewGroup();//XML구성을 위한 데이터 구조화 작업 관련 초기화
        InitializeXMLForNewGroup();//XML 노드 구성 초기화
    }

    //XML구성을 위한 데이터 구조화 작업 관련 초기화
    public void InitializeDataListForNewGroup()
    {
        _pnuList.Clear();
        _jibunList.Clear();
        _labelList.Clear();
        _depthList.Clear();

        _shapeCount = 0;
        _labelCount = 0;
        _depthCount = 0;

        _dfXml = new System.Data.DataTable();
        _dfXml.Columns.Add("PNU", typeof(string));

        _dfPnu = new System.Data.DataTable();
        _dfPnu.Columns.Add("PNU", typeof(string));
        _dfPnu.Columns.Add("ITEM_NO", typeof(int));
        _dfPnu.Columns.Add("DEPTH", typeof(int));
    }

    //XML 노드 구성 초기화
    public void InitializeXMLForNewGroup()
    {
        _itemList.Clear();
        _labelTuples.Clear();

        _xdoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));

        // XML 루트 초기화
        _root = new XElement("XtraSerializer", new XAttribute("version", "23.2.3.0"));
        _xdoc.Add(_root);
        
        var items = new XElement("Items");
        var item1 = new XElement("Item1",
            new XAttribute("ItemKind", "DiagramRoot"),
            new XAttribute("PageSize", "800,600"),
            new XAttribute("SelectedStencils", "BasicShapes, BasicFlowchartShapes")
        );
        _children = new XElement("Children");
        item1.Add(_children);
        items.Add(item1);
        _root.Add(items);
    }

    //DB레코드 분석 및 파싱(Jibun, Connector, Label)
    private void AnalyzeData(List<LandMoveInfo> flowList)
    {
        foreach (var row in flowList)
        {
            //CEO.REQ//
            //row.bfJibun = GetJibun(row.bfPnu, 1);//'읍면' 명칭 포함
            //row.afJibun = GetJibun(row.afPnu, 1);//'읍면' 명칭 포함
            row.bfPnu = GetJibun(row.bfPnu, 3);//'읍면' 명칭 제거
            row.afPnu = GetJibun(row.afPnu, 3);//'읍면' 명칭 제거               

            row.bfJimok = GetCodeValue(2, row.bfJimok);//지목 코드 => 명칭
            row.afJimok = GetCodeValue(2, row.afJimok);//지목 코드 => 명칭
            row.rsn = GetCodeValue(3, row.rsn);//이동종목 코드 => 명칭
        }

        var rsnOld = "";
        var rsnNew = "";
        var dtOld = "";
        var dtNew = "";
        var pnuOld = "";
        var pnuNew = "";
        var subIdx = 0;     // 동일 subgrp 안에서 순번
        //Vit.G//
        var bfJimok = "";
        double bfArea = 0;
        var afJimok = "";
        double afArea = 0;
        var ownName = "";

        foreach (var row in flowList)
        {
            rsnNew = row.rsn ?? "";
            dtNew = row.regDt ?? "";
            bfJimok = row.bfJimok ?? "";
            bfArea = row?.bfArea ?? 0.0;
            afJimok = row.afJimok ?? "";
            afArea = row?.afArea ?? 0.0;
            ownName = row.ownName ?? "";

            //Vit.G//목록 : 지번만 표시, Diagram : (동 or 리) + 지번
            var bfPnu = row.bfPnu; //row.bfJibun;//row.bfPnu;
            var afPnu = row.afPnu; //row.afJibun;//row.afPnu;
            var label = $"{rsnNew} {dtNew}";
            var bfAtt = "";//지목+면적 속성
            var afAtt = "";//지목+면적 속성
            var newAtt = "";//지목+면적 속성

            pnuNew = rsnNew == "분할" ? bfPnu : afPnu;

            //Vit.G//251015 : [지목변경] 표시 - 체크박스에 따른 필터링
            if (!_isJimokChg && rsnNew == "지목변경")
            {
                //'지목변경' 데이터 필터링//
                continue;    
            }

            //Vit.G//[TODO]----------------------------------------
              //- [Jibun]에 표시
            if (_isJimok && _isArea)//[지목] && [면적] 체크
            {
                bfAtt = $"[{bfJimok}/{bfArea}]";
                if (_isOwnName)//[소유자명] 체크
                    afAtt = $"[{afJimok}/{afArea}/{ownName}]";
                else
                    afAtt = $"[{afJimok}/{afArea}]";
                
            }
            else if (_isJimok)//Vit.G//[지목] 체크
            {
                bfAtt = $"[{bfJimok}]";
                if (_isOwnName)//[소유자명] 체크
                    afAtt = $"[{afJimok}/{ownName}]";
                else
                    afAtt = $"[{afJimok}]";
            }
            else if (_isArea)//Vit.G//[면적] 체크
            {
                bfAtt = $"[{bfArea}]";
                if (_isOwnName)//[소유자명] 체크
                    afAtt = $"[{afArea}/{ownName}]";
                else
                    afAtt = $"[{afArea}]";
            }
            else if (_isOwnName)//Vit.G//[소유자명] 체크
            {                
                afAtt = $"[{ownName}]";
            }
            newAtt = rsnNew == "분할" ? bfAtt : afAtt;
            //---------------------------------------------

            if (!rsnNew.Equals(rsnOld) || !dtNew.Equals(dtOld)) // New Depth
            {
                subIdx = 0;
                AddDepthToList(label);
                if (!_jibunList.Contains(bfPnu))
                {
                    AddShapeToList(bfPnu);
                }
                //'합병'인 경우 모든 필지가 항상 맨처음 필지를 기준으로 합병되는 것이 아니다. (예, 4420041023.120-4)
                //'합병'인 경우 depth변경 혹은 다른 subgrp 이 되는 경우에만 해당 pnu가 _pnuList에 존재하는지 확인 후 Add할 것//
                if ( !rsnNew.Equals("합병") ) 
                    AddShapeToList(afPnu);
                AddLabelToList(label);
            }
            else // Same Depth
            {
                if (pnuOld.Equals(pnuNew)) // Same Subgroup
                {
                    subIdx += 1;
                    if (rsnNew.Equals("합병"))
                    {
                        if (!_jibunList.Contains(bfPnu)) AddShapeToList(bfPnu);
                    }
                    else if (rsnNew.Equals("분할"))
                    {
                        AddShapeToList(afPnu);
                    }
                }
                else // New Subgroup in Same Depth
                {
                    subIdx = 0;
                    if (rsnNew.Equals("합병") || rsnNew.Equals("분할"))
                    {
                        if (!_jibunList.Contains(pnuNew)) AddShapeToList(pnuNew);
                        AddShapeToList(afPnu);
                    }
                    else
                    {
                        AddShapeToList(afPnu);
                    }
                    AddLabelToList(label);
                }
            }

            rsnOld = rsnNew;
            dtOld = dtNew;
            pnuOld = rsnNew.Equals("분할") ? bfPnu : afPnu;

            var pnu = rsnNew.Equals("합병") ? bfPnu : afPnu;

            //if (pnu.Equals("120") || pnu.Equals("120-4") || pnu.Equals("685-78") || _depthCount == 3)
            //{
            //    int x = 0;
            //}

            var pnuIdx = _pnuList.IndexOf(pnu);
            if (pnuIdx >= 0)
            {
                string dep0Itm = "DEP0_ITM";
                string subCol = $"DEP{_depthCount}_SUB";
                string tmbCol = $"DEP{_depthCount}_TMB";
                string pnuCol = $"DEP{_depthCount}_PNU";
                string itmCol = $"DEP{_depthCount}_ITM";
                
                if (!_dfXml.Columns.Contains(dep0Itm)) _dfXml.Columns.Add(dep0Itm, typeof(string));
                if (!_dfXml.Columns.Contains(subCol)) _dfXml.Columns.Add(subCol, typeof(string));
                if (!_dfXml.Columns.Contains(tmbCol)) _dfXml.Columns.Add(tmbCol, typeof(string));
                if (!_dfXml.Columns.Contains(pnuCol)) _dfXml.Columns.Add(pnuCol, typeof(string));
                if (!_dfXml.Columns.Contains(itmCol)) _dfXml.Columns.Add(itmCol, typeof(string));

                //DataRow existingRow = _dfXml.AsEnumerable().FirstOrDefault(r => r.Field<string>("PNU") == pnu);
                // _dfXml의 인덱스 pnuIdx 값

                bool isExist = true;
                DataRow existingRow;
                var pnuIdx2 = GetIndexDFXML(pnu);//251117//
                if (pnuIdx2 > -1 && _dfXml.Rows.Count > pnuIdx2)
                {
                    existingRow = _dfXml.Rows[pnuIdx2];
                }
                else
                {
                    isExist = false;
                    existingRow = _dfXml.NewRow();
                    existingRow["PNU"] = pnu;
                }

                if (rsnNew.Equals("합병"))
                {
                    existingRow["PNU"] = pnu;
                    //Vit.G//
                    //existingRow[dep0Itm] = "0";
                    if ( !isExist )
                        existingRow[dep0Itm] = bfAtt != "" ? bfAtt : "0";
                    else
                        existingRow[dep0Itm] = afAtt != "" ? afAtt : "0";
                    existingRow[subCol] = _labelCount.ToString();
                    existingRow[tmbCol] = (pnu == afPnu) ? "thumb" : "";
                    existingRow[pnuCol] = (pnu == afPnu) ? afPnu : "";                    
                    existingRow[itmCol] = (subIdx == 0) ? (afAtt != "" ? afAtt : "0") : "";
                }
                else
                {
                    existingRow["PNU"] = pnu;
                    //Vit.G//
                    //existingRow[dep0Itm] = "0";
                    if( !isExist )
                        existingRow[dep0Itm] = afAtt != "" ? afAtt : "0";                    
                    existingRow[subCol] = _labelCount.ToString();
                    existingRow[tmbCol] = (pnu == bfPnu) ? "thumb" : "";
                    existingRow[pnuCol] = afPnu;
                    existingRow[itmCol] = afAtt != "" ? afAtt : "0";
                }

                if (isExist == false)
                {
                    _dfXml.Rows.Add(existingRow);
                }
            }
        }//foreach (var row in flowList)

        //XML 내용 => CSV 저장하기
        string pnuNm = GetJibun(_pnu, 2);
        //String pathcsv = Path.Combine(_tempDir, $"DF_XML_{_currentGroupNo}.csv");
        String pathcsv = Path.Combine(_tempDir, $"DF_XML_{pnuNm}_{_currentGroupNo}.csv");
        SaveDfXmlToCsv(_dfXml, pathcsv);

        //XML 구성하기
        MakeXmlData();
    }
    
    private int AddShapeToList(string pnu)
    {
        _jibunList.Add(pnu);
        _shapeCount++;
        if (!_pnuList.Contains(pnu))
        {
            _pnuList.Add(pnu);
        }
        return _shapeCount;
    }

    private void AddLabelToList(string label)
    {
        _labelList.Add(label);
        _labelCount++;
    }

    private void AddDepthToList(string label)
    {
        _depthList.Add(label);
        _depthCount++;
    }

    #endregion

    #region XML 생성

    //251027//private => public : Converter에서 참조//
    public void MakeXmlData()
    {
        int begin = 0;
        int end = 0;
        int bfDepth = 0;
        int thumbidx = -1;
        bool focus = false;//Vit.G//251014 : 조회 필지 => BackgroundId 속성 추가

        //Vit.G//251014 : 조회 필지코드 => 지번명으로 변경
        _pnu = GetJibun(_pnu, 3);//CEO.REQ//_pnu = GetJibun(_pnu, 1);

        //
        try
        {
            for (int depIdx = 0; depIdx < _depthList.Count; depIdx++)
            {
                var label = _depthList[depIdx];
                var rsn = label.Split(' ')[0];
                //var dt = label.Split(' ')[1];
                
                var subColName = $"DEP{depIdx + 1}_SUB";
                //if (!_dfXml.Columns.Contains(subColName)) continue;
                
                var subGroups = _dfXml.AsEnumerable()
                    .Where(r => !string.IsNullOrEmpty(r.Field<string>(subColName)))
                    .Select(r => r.Field<string>(subColName))
                    .Distinct()
                    .OrderBy(r => r)
                    .ToList();

                foreach (var subGrp in subGroups)
                {
                    var tmbColName = $"DEP{depIdx + 1}_TMB";
                    var pnuColName = $"DEP{depIdx + 1}_PNU";
                    var itmColName = $"DEP{depIdx + 1}_ITM";
                    var attColName = "DEP0_ITM";

                    var filtered = _dfXml.AsEnumerable()
                        .Select((r, i) => new { Row = r, Index = i }) 
                        .Where(item => item.Row.Field<string>(subColName) == subGrp)
                        .OrderByDescending(item => item.Row.Field<string>(tmbColName))
                        .ThenBy(item => item.Row.Field<string>(pnuColName))
                        .ToList();

                    //for (int rowIdx = 0; rowIdx < filtered.Count; rowIdx++)
                    foreach (var item in filtered)
                    {
                        //var row = filtered[rowIdx];
                        var row = item.Row; 
                        int rowIdx = item.Index;
                        
                        var pnu = row.Field<string>(pnuColName);
                        var bfAtt = row.Field<string>(attColName);
                        if (bfAtt == "0") bfAtt = ""; 
                        var afAtt = "";
                        bool bNewPnu = false;

                        if (string.IsNullOrEmpty(pnu))
                        {
                            pnu = row.Field<string>("PNU");
                            bNewPnu = true;
                        }

                        //Vit.G//251014 : 조회 필지 => BackgroundId 속성 추가
                        if (pnu == _pnu)
                            focus = true;
                        else
                            focus = false;

                        //var isThumb = row.Field<string>(tmbColName) == "thumb";
                        var thumb = row.Field<string>(tmbColName);
                        afAtt = row.Field<string>(itmColName);//Vit.G//
                        if (afAtt == "0") afAtt = "";

                        bfDepth = depIdx;


                        //if (pnu == "120")
                        //{
                        //    int a = 1;
                        //}




                        if (thumb.Equals("thumb") || rsn.Equals("합병"))
                        {
                            var lastRow = _dfPnu
                                .AsEnumerable()
                                //.LastOrDefault(r => r.Field<string>("PNU") == pnu)
                                .LastOrDefault(r =>
                                {
                                    var cell = r.Field<string>("PNU");
                                    if (string.IsNullOrEmpty(cell)) return false;
                                    
                                    //개행 문자 전까지만 사용
                                    var trimmed = cell.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                                    return trimmed.Equals(pnu, StringComparison.OrdinalIgnoreCase);
                                });
                            
                            // lastRow의 갯수 확인
                            if (lastRow != null)
                            {
                                begin = lastRow.Field<int>("ITEM_NO");
                                bfDepth = lastRow.Field<int>("DEPTH");
                            }
                            else
                            {
                                if( (bNewPnu) || (thumb.Equals("thumb") && rsn.Equals("합병")) )
                                    begin = MakeXmlJibun(bfAtt != "" ? pnu + "\r\n" + bfAtt : pnu, depIdx + 1, false, rowIdx, focus);//Vit.G//[add]focus
                                else
                                    begin = MakeXmlJibun(afAtt != "" ? pnu + "\r\n" + afAtt : pnu, depIdx + 1, false, rowIdx, focus);//Vit.G//[add]focus
                            }

                            if (thumb.Equals("thumb"))
                            {
                                end = MakeXmlJibun(afAtt != "" ? pnu + "\r\n" + afAtt : pnu, depIdx + 1, true, rowIdx, focus);//Vit.G//[add]focus
                                MakeXmlConnector(bfDepth, depIdx + 1, begin - 1, end - 1, rowIdx, rowIdx, focus);//Vit.G//[add]focus
                                MakeXmlLabel(label, depIdx + 1, rowIdx, focus);//Vit.G//[add]focus
                                thumbidx = rowIdx;
                            }
                            else
                            {
                                if (focus && rowIdx != thumbidx)//조회필지 && 모번지 => 꺽은선에 배경색 속성 추가//
                                    UpdateItem(begin - 1, "BackgroundId", "Accent5");
                                MakeXmlConnector(bfDepth, depIdx + 1, begin - 1, end - 1, rowIdx, thumbidx, focus);//Vit.G//[add]focus
                            }
                        }
                        else
                        {
                            end = MakeXmlJibun(afAtt != "" ? pnu + "\r\n" + afAtt : pnu, depIdx + 1, true, rowIdx, focus);//Vit.G//[add]focus
                            if (focus && rowIdx != thumbidx)//조회필지 && 모번지 => 꺽은선에 배경색 속성 추가//
                            {
                                UpdateLabelTuples(_labelTuples.Count - 1, true);
                                UpdateItem(begin - 1, "BackgroundId", "Accent5");
                            }
                                
                            MakeXmlConnector(bfDepth, depIdx + 1, begin - 1, end - 1, thumbidx, rowIdx, focus);//Vit.G//[add]focus
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Debug(ex.Message);
        }

        // 마지막에 라벨 추가 (Bring to Front 효과)
        var labelCol = _settingVM.LabelColors;
        foreach (var (x, y, label, bfocus) in _labelTuples)//Vit.G//[add]focus
        {
            var item = new XElement($"Item{_itemList.Count + 1}",
                new XAttribute("Position", $"{x},{y}"),
                new XAttribute("Size", $"{LabelW},{LabelH}"),
                new XAttribute("FontSize", FontSize.ToString()),
                new XAttribute("ThemeStyleId", "Variant2"),
                //251027//[색상설정 - 사용자정의]//Vit.G//251014 : 조회 필지 => ForegroundId, StrokeId 속성 추가
                //bfocus ? new XAttribute("Foreground", "#FF008000") : new XAttribute("ForegroundId", "Black"),
                //bfocus ? new XAttribute("Stroke", "FF008000") : new XAttribute("StrokeId", "Black"),
                new XAttribute("Foreground", bfocus ? labelCol[1, 2] : labelCol[0, 2]),//글자색
                new XAttribute("Background", bfocus ? labelCol[1, 0] : labelCol[0, 0]),//배경색
                new XAttribute("Stroke", bfocus ? labelCol[1, 1] : labelCol[0, 1]),//테두리
                new XAttribute("Content", label),
                new XAttribute("ItemKind", "DiagramShape")
            );
            _children?.Add(item);
            _itemList.Add(item.ToString());
        }

        //----------------------------------------
        //Vit.G//XML 파일 저장
        //DiagramControl.SaveFile()
        //SaveDocument(), LoadDocument()
        String pathxml = Path.Combine(_tempDir, $"XML_{_pnu}.xml");
        _xdoc.Save(pathxml);
        //여기에서 오류발생으로 주석처리//--- string str = rtnXml.ToString();
        //String pathpdf = @"D:\MyDiagram.pdf";
        //// XML 파일(혹은 stream)을 직접 PDF로 저장
        //var doc = new Aspose.Pdf.Document(pathxml);
        //doc.Save("output.pdf");
        //----------------------------------------

    }

    private int MakeXmlJibun(string pnu, int depth, bool isAf, int rowIdx, bool focus)//Vit.G//[add]focus
    {
        int x = 0;
        int y = 0;
        var jibunCol = _settingVM.JibunColors;


        if (_isPortrait)//[세로형 그리기]
        {
            x = (rowIdx >= 0) ? (StartX + rowIdx * (P_ShapeW + P_ShapeGap)) : StartX; 
            //int y = StartY + rowIdx * ShapeBet;
            y = isAf ? (StartY + depth * (P_ShapeH + P_ConnectorH)) : (StartY + (depth - 1) * (P_ShapeH + P_ConnectorH));
            //Console.WriteLine($"{pnu} {x} {y} {rowIdx}");
        }
        else//[가로형 그리기]
        {
            x = isAf ? (StartX + depth * (ShapeW + ConnectorW)) : (StartX + (depth - 1) * (ShapeW + ConnectorW));
            //int y = StartY + rowIdx * ShapeBet;
            y = (rowIdx >= 0) ? (StartY + rowIdx * (ShapeH + ShapeGap)) : StartY;
            //Console.WriteLine($"{pnu} {x} {y} {rowIdx}");
        }


        var item = new XElement($"Item{_itemList.Count + 1}",
            new XAttribute("Position", $"{x},{y}"),
            new XAttribute("Size", $"{ShapeW},{ShapeH}"),
            //251027//[색상설정 - 사용자정의]
            //new XAttribute("BackgroundId", focus ? "Accent5" : "White_4"),//Vit.G//251014 : 조회 필지 => BackgroundId 속성 추가
            new XAttribute("Foreground", focus ? jibunCol[1,2] : jibunCol[0,2]),//글자색
            new XAttribute("Background", focus ? jibunCol[1,0] : jibunCol[0,0]),//배경색
            new XAttribute("StrokeId", focus ? jibunCol[1,1] : jibunCol[0,1]),//테두리
            new XAttribute("Content", pnu),
            new XAttribute("ItemKind", "DiagramShape")
        );

        _children?.Add(item);
        _itemList.Add(item.ToString());
        _dfPnu.Rows.Add(pnu, _itemList.Count, depth);
        return _itemList.Count;
    }

    private void MakeXmlLabel(string label, int depth, int rowIdx, bool focus)//Vit.G//[add]focus
    {
        int x = 0;
        int y = 0;


        if (_isPortrait)//[세로형 그리기]
        {            
            x = 0;
            if (rowIdx >= 0)
            {
                x = StartX + rowIdx * (P_ShapeW + P_labelGap) + (int)(P_ShapeW / 2) + P_labelGap;
            }
            else
            {
                x = StartX + (int)(ShapeW / 2) + P_labelGap;
            }
            y = (StartY + P_ShapeH) + (depth - 1) * (P_ShapeH + P_ConnectorH) + P_labelGap;
        }
        else//[가로형 그리기]
        {
            x = (StartX + ShapeW) + (depth - 1) * (ShapeW + ConnectorW) + labelGap;
            y = 0;
            if (rowIdx >= 0)
            {
                y = StartY + rowIdx * ShapeBet + (int)(ShapeH / 2) - (LabelH + labelGap);
            }
            else
            {
                y = StartY + (int)(ShapeH / 2) - (LabelH + labelGap);
            }
        }
        
        _labelTuples.Add((x, y, label, focus));//Vit.G//[add]focus
    }
    
    private void MakeXmlConnector(int bfDepth, int depth, int begin, int end, int startIdx, int endIdx, bool focus)//Vit.G//[add]focus
    {
        int x = 0;
        int y = 0;
        int x2 = 0;
        int y2 = 0;
        int mid = 0;
        var connCol = _settingVM.ConnectorColors;

        if (_isPortrait)//[세로형 그리기]
        {
            x = StartX + startIdx * P_ShapeBet + (int)(P_ShapeW / 2);
            x2 = x + (endIdx - startIdx) * P_ConnectorW;
            y = (StartY + P_ShapeH) + bfDepth * (P_ShapeH + P_ConnectorH); 
            y2 = y + P_ConnectorH + (depth - 1 - bfDepth) * (P_ShapeH + P_ConnectorH); 
            mid = (int)(P_ConnectorH / 2);
        }
        else
        {
            x = (StartX + ShapeW) + bfDepth * (ShapeW + ConnectorW);
            x2 = x + ConnectorW + (depth - 1 - bfDepth) * (ShapeW + ConnectorW);
            y = StartY + startIdx * ShapeBet + (int)(ShapeH / 2);
            y2 = y + (endIdx - startIdx) * ShapeBet;
            mid = (int)(ConnectorW / 2);
        }


        XElement item;
        if (startIdx == endIdx) // 직선
        {
            item = new XElement($"Item{_itemList.Count + 1}",
                //251027//[색상설정 - 사용자정의]
                //focus ? new XAttribute("StrokeId", "Accent5") : null,//Vit.G//251014 : 조회 필지 => StrokeId 속성 추가
                new XAttribute("Stroke", focus ? connCol[1, 0] : connCol[0, 0]),//테두리
                new XAttribute("Points", "(Empty)"),
                new XAttribute("ItemKind", "DiagramConnector"),
                new XAttribute("BeginPoint", $"{x},{y}"),
                new XAttribute("EndPoint", _isPortrait ? $"{x},{y2}" : $"{x2},{y}"),
                new XAttribute("BeginItem", begin),
                new XAttribute("EndItem", end)
            );
        }
        else // 꺾은선
        {
            //int midX = x + (x2 - x) / 2;
            string points = "";
            if (_isPortrait)//[세로형 그리기]
                points = $"{x},{y2 - mid} {x2},{y2 - mid}";
            else
                points = $"{x2 - mid},{y} {x2 - mid},{y2}";

            item = new XElement($"Item{_itemList.Count + 1}",
                    //251027//[색상설정 - 사용자정의]
                    //focus ? new XAttribute("StrokeId", "Accent5") : null,//Vit.G//251014 : 조회 필지 => StrokeId 속성 추가
                    new XAttribute("Stroke", focus ? connCol[1, 0] : connCol[0, 0]),//테두리                    
                    new XAttribute("BeginItemPointIndex", _isPortrait ? "2" : "1"),
                    new XAttribute("EndItemPointIndex", _isPortrait ? "0" : "3"),
                    new XAttribute("Points", points),
                    new XAttribute("ItemKind", "DiagramConnector"),
                    new XAttribute("BeginItem", begin),
                    new XAttribute("EndItem", end),
                    new XAttribute("BeginPoint", _isPortrait ? $"{x},{y}" : $"{x},{y2}"),
                    new XAttribute("EndPoint", $"{x2},{y2}"),
                    new XAttribute("KeepMiddlePoints", "true")
                );
        }

        _children?.Add(item);
        _itemList.Add(item.ToString());
    }

    // 기존 아이템의 속성 수정
    private void UpdateItem(int index, string attrName, string newValue)
    {
        if (index < 0 || index >= _itemList.Count) return;

        // 문자열을 XElement로 변환
        XElement element = XElement.Parse(_itemList[index]);

        // 속성값 변경 (기존 없으면 새로 추가)
        element.SetAttributeValue(attrName, newValue);

        // 다시 string으로 저장
        _itemList[index] = element.ToString();
    }
    private void UpdateLabelTuples(int index, bool newValue)
    {
        if (index < 0 || index >= _labelTuples.Count) return;

        // 문자열을 XElement로 변환
        var item = _labelTuples[index];

        // 다시 string으로 저장
        _labelTuples[index] = (item.x, item.y, item.label, newValue);
    }


    //임시 파일 저장할 폴더 생성
    private string SetTempDir()
    {
        string exePath = Assembly.GetExecutingAssembly().Location;
        // 상대경로 폴더명 지정
        return Path.Combine(Path.GetDirectoryName(exePath), "_tempdir");
    }

    private void SaveDfXmlToCsv(System.Data.DataTable dt, string filePath)
    {
        var lines = dt.AsEnumerable()
            .Select(row => string.Join(",", row.ItemArray.Select(field => field.ToString())));
        var csv = string.Join(Environment.NewLine,
            new[] { string.Join(",", dt.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName)) }.Concat(lines));
        File.WriteAllText(filePath, csv);
    }
    #endregion

    #region 파일 저장
    /*private void EnsureDirectoryExists(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }*/

    /*private void SaveDbLinesToFile()
    {
        var filePath = Path.Combine(ResultPath, DbLinesPath, $"db_lines_{_currentGroupNo}.txt");
        EnsureDirectoryExists(filePath);
        File.WriteAllLines(filePath, _dbLines, Encoding.UTF8);
    }*/

    /*private void SaveDfXmlToCsv()
    {
        var filePath = Path.Combine(ResultPath, DfPath, $"DF_XML_{_currentGroupNo}.csv");
        EnsureDirectoryExists(filePath);
        
        var sb = new StringBuilder();
        var columnNames = _dfXml.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
        sb.AppendLine(string.Join(",", columnNames));

        foreach (DataRow row in _dfXml.Rows)
        {
            var fields = row.ItemArray.Select(field => field?.ToString()?.Replace(",", ";") ?? "");
            sb.AppendLine(string.Join(",", fields));
        }
        
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }*/

    /*private void SaveXmlToFile()
    {
        if (_root == null) return;
        
        var filePath = Path.Combine(ResultPath, XmlPath, $"XML_{_currentGroupNo}.xml");
        EnsureDirectoryExists(filePath);
        
        var xdoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), _root);
        xdoc.Save(filePath);
        //PrintLog($"{filePath} 파일 저장 완료.");
    }*/

    #endregion


    #region _dfXml 에서 찾기
    public int GetIndexDFXML(string findPnu)
    {
        // 전체 데이터 중 PNU 컬럼에 searchString이 포함된 첫 번째 행 인덱스 찾기
        int index = -1;
        for (int i = 0; i < _dfXml.Rows.Count; i++)
        {
            string pnuValue = _dfXml.Rows[i]["PNU"].ToString();
            if (pnuValue.Contains(findPnu))
            {
                index = i;
                break;
            }
        }
        return index;
    }
    #endregion

    #region 색상 설정
    public void UpdateWithNewSetting(LandMoveSettingViewModel settingVM)
    {
        _settingVM = settingVM; // 값이 실제 변경된 새 인스턴스라면 갱신
    }
    #endregion



    // ===========================================================
    // 메인 실행 로직
    // ===========================================================
    public XDocument Run(List<LandMoveInfo> flowList, LandMoveFlowViewModel vm, List<LandMoveInfoCategory> categoryList, string pnu)//Vit.G//[add]pnu, vm
    {
        try
        {
            // 시군구 코드 등 공통 코드 조회
            ReadCodeTables();

            // 임시 파일 저장할 경로 설정//
            _tempDir = SetTempDir();

            //Vit.G//조회 필지코드(19자리)
            _pnu = pnu;

            //정리일+종목=Category 종목코드=>명칭 변경
            GetCodeValueCategory(categoryList);

            ProcessLandMoveFlow(flowList, vm);
            string str = _xdoc.ToString();
            return _xdoc;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        return null;
    }
}