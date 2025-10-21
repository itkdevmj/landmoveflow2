using DevExpress.CodeParser;
using DevExpress.Diagram.Core.Native;
using DevExpress.Xpf.CodeView;
using DevExpress.Xpf.Diagram;
using DevExpress.XtraSpreadsheet.Model;
using LMFS.Models;
using LMFS.Services;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Ink;
using System.Windows.Media;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

namespace LMFS.Engine;

public class LandMoveFlowConverter
{
    public static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    
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
    // 코드 조회 결과 저장용
    // -----------------------------------------------------------
    //private Dictionary<string, string> _listLawd = new();
    private List<SidoCode> _listLawd = new();
    private Dictionary<string, string> _listJimok = new();
    private Dictionary<string, string> _listMovrsn = new();

    // -----------------------------------------------------------
    // DataFrame 대신 사용할 DataTable
    // -----------------------------------------------------------
    private DataTable _dfXml = new();
    private DataTable _dfPnu = new();

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
    private bool _isJimokChgShow;//Vit.G//조회 필지코드(19자리)
    private bool _isPortrait;//[세로형 그리기]

    // -----------------------------------------------------------
    // DBMS 조회 데이터 및 그룹 정보
    // -----------------------------------------------------------
    private List<int> _groupList = new();
    private int _currentGroupNo;
    private List<string> _dbLines = new();

    // -----------------------------------------------------------
    // XML 노드 구성용 변수
    // -----------------------------------------------------------
    private XElement? _root;
    private XElement? _children;
    private XDocument _xdoc;
    
    #endregion

    
    #region 데이터 조회 및 가공

    private void ReadCodeTables()
    {
        _listLawd = GlobalDataManager.Instance.sidoCodeList;
    }

    private string GetCodeValue(int opt, string find)
    {
        return opt switch
        {
            //1 => _listLawd.GetValueOrDefault(find, ""), // LAWD
            2 => _listJimok.GetValueOrDefault(find, ""), // JIMOK
            3 => _listMovrsn.GetValueOrDefault(find, ""), // LAND_MOV_RSN
            _ => ""
        };
    }

    //option (1)지역명(동 or 리)
    //       (2)지번만
    private string GetJibun(string landCd, int option)
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
                    //lawdNm = item.umdNm + " " + item.riNm;
                    lawdNm = item.riCd == "00" ? item.umdNm : item.riNm;//Vit.G//[add]'동지역'
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
        return $"{lawdNm} {jibun}";
        //return $"{jibun}";
    }

    private void ProcessLandMoveFlow(List<LandMoveInfo> rtnList)
    {
        // 각종 변수 초기화
        InitializeForNewGroup();

        // 데이터 분석 > XML 구성 > XML 저장
        AnalyzeData(rtnList);
    }
    #endregion
    

    #region 데이터 분석 및 처리
    private void InitializeForNewGroup()
    {
        _pnuList.Clear();
        _jibunList.Clear();
        _labelList.Clear();
        _depthList.Clear();
        _itemList.Clear();
        _labelTuples.Clear();
        
        _shapeCount = 0;
        _labelCount = 0;
        _depthCount = 0;

        _dfXml = new DataTable();
        _dfXml.Columns.Add("PNU", typeof(string));

        _dfPnu = new DataTable();
        _dfPnu.Columns.Add("PNU", typeof(string));
        _dfPnu.Columns.Add("ITEM_NO", typeof(int));
        _dfPnu.Columns.Add("DEPTH", typeof(int));
        
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

    private void AnalyzeData(List<LandMoveInfo> flowList)
    {
        foreach (var row in flowList)
        {
            row.bfJibun = GetJibun(row.bfPnu, 1);//'읍면' 명칭 포함
            row.afJibun = GetJibun(row.afPnu, 1);//'읍면' 명칭 포함
            row.bfPnu = GetJibun(row.bfPnu, 2);//'읍면' 명칭 제거
            row.afPnu = GetJibun(row.afPnu, 2);//'읍면' 명칭 제거
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
            //Vit.G//[TODO]코드테이블매칭작업필요    
            if (row.rsn == "01")
                row.rsn = "신규";
            else if (row.rsn == "10")
                row.rsn = "등록전환";
            else if (row.rsn == "20")
                row.rsn = "분할";
            else if (row.rsn == "30")
                row.rsn = "합병";
            else if (row.rsn == "40")
                row.rsn = "지목변경";
            else if (row.rsn == "55")
                row.rsn = "지적재조사완료";
            else if (row.rsn == "55")
                row.rsn = "등록사항정정";

            rsnNew = row.rsn;
            dtNew = row.regDt;
            bfJimok = row.bfJimok;
            bfArea = row.bfArea;
            afJimok = row.afJimok;
            afArea = row.afArea;
            ownName = row.ownName;

            //Vit.G//목록 : 지번만 표시, Diagram : (동 or 리) + 지번
            var bfPnu = row.bfJibun;//row.bfPnu;
            var afPnu = row.afJibun;//row.afPnu;
            var label = $"{rsnNew} {dtNew}";


            //Vit.G//[TODO]
            ////- [Label]에 표시
            ////if (IsName)//[소유자명] 체크
            //    label = $"{label}&#xD;&#xA;[{ownName}]";

            ////- [Jibun]에 표시
            //if (IsJimok && IsArea)//[지목] && [면적] 체크
            //{
            //    bfPnu = $"{bfPnu}&#xD;&#xA;[{bfJimok}/{bfArea}]";
            //    bfPnu = $"{bfPnu}&#xD;&#xA;[{afJimok}/{afArea}]";
            //}
            //else if (IsJimok)//Vit.G//[지목] 체크
            //{
            //    bfPnu = $"{bfPnu}&#xD;&#xA;[{bfJimok}]";
            //    afPnu = $"{afPnu}&#xD;&#xA;[{afJimok}]";
            //}
            //else if (IsArea)//Vit.G//[면적] 체크
            //{
            //    bfPnu = $"{bfPnu}&#xD;&#xA;[{bfArea}]";
            //    bfPnu = $"{bfPnu}&#xD;&#xA;[{afArea}]";
            //}

            //Vit.G//251015 : [지목변경] 표시 - 체크박스에 따른 필터링
            if (!_isJimokChgShow && rsnNew == "지목변경")
            {
                //'지목변경' 데이터 필터링//
                continue;    
            }

            pnuNew = rsnNew == "분할" ? bfPnu : afPnu;
            
            if (!rsnNew.Equals(rsnOld) || !dtNew.Equals(dtOld)) // New Depth
            {
                subIdx = 0;
                AddDepthToList(label);
                if (!_jibunList.Contains(bfPnu))
                {
                    AddShapeToList(bfPnu);
                }
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

            if (pnu.Equals("음봉면 신휴리 419-33"))
            {
                int x = 0;
            }
            
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
                if (_dfXml.Rows.Count > pnuIdx)
                {
                    existingRow = _dfXml.Rows[pnuIdx];
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
                    existingRow[dep0Itm] = "0";
                    existingRow[subCol] = _labelCount.ToString();
                    existingRow[tmbCol] = (pnu == afPnu) ? "thumb" : "";
                    existingRow[pnuCol] = (pnu == afPnu) ? afPnu : "";
                    existingRow[itmCol] = (subIdx == 0) ? "0" : "";
                }
                else
                {
                    existingRow["PNU"] = pnu;
                    existingRow[dep0Itm] = "0";
                    existingRow[subCol] = _labelCount.ToString();
                    existingRow[tmbCol] = (pnu == bfPnu) ? "thumb" : "";
                    existingRow[pnuCol] = afPnu;
                    existingRow[itmCol] = "0";
                }

                if (isExist == false)
                {
                    _dfXml.Rows.Add(existingRow);
                }
            }
        }

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

    private void MakeXmlData()
    {
        int begin = 0;
        int end = 0;
        int bfDepth = 0;
        int thumbidx = -1;
        bool focus = false;//Vit.G//251014 : 조회 필지 => BackgroundId 속성 추가

        //Vit.G//251014 : 조회 필지코드 => 지번명으로 변경
        _pnu = GetJibun(_pnu, 1);

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
                        if (string.IsNullOrEmpty(pnu))
                        {
                            pnu = row.Field<string>("PNU");
                        }

                        //Vit.G//251014 : 조회 필지 => BackgroundId 속성 추가
                        if (pnu == _pnu)
                            focus = true;
                        else
                            focus = false;


                        //var isThumb = row.Field<string>(tmbColName) == "thumb";
                        var thumb = row.Field<string>(tmbColName);

                        bfDepth = depIdx;

                        if (thumb.Equals("thumb") || rsn.Equals("합병"))
                        {
                            var lastRow = _dfPnu
                                .AsEnumerable()
                                .LastOrDefault(r => r.Field<string>("PNU") == pnu);
                            
                            // lastRow의 갯수 확인
                            if (lastRow != null)
                            {
                                begin = lastRow.Field<int>("ITEM_NO");
                                bfDepth = lastRow.Field<int>("DEPTH");
                            }
                            else
                            {
                                begin = MakeXmlJibun(pnu, depIdx + 1, false, rowIdx, focus);//Vit.G//[add]focus
                            }

                            if (thumb.Equals("thumb"))
                            {
                                end = MakeXmlJibun(pnu, depIdx + 1, true, rowIdx, focus);//Vit.G//[add]focus
                                MakeXmlConnector(bfDepth, depIdx + 1, begin - 1, end - 1, rowIdx, rowIdx, focus);//Vit.G//[add]focus
                                MakeXmlLabel(label, depIdx + 1, rowIdx, focus);//Vit.G//[add]focus
                                thumbidx = rowIdx;
                            }
                            else
                            {
                                MakeXmlConnector(bfDepth, depIdx + 1, begin - 1, end - 1, rowIdx, thumbidx, focus);//Vit.G//[add]focus
                            }
                        }
                        else
                        {
                            end = MakeXmlJibun(pnu, depIdx + 1, true, rowIdx, focus);//Vit.G//[add]focus
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
        foreach (var (x, y, label, bfocus) in _labelTuples)//Vit.G//[add]focus
        {
            var item = new XElement($"Item{_itemList.Count + 1}",
                new XAttribute("Position", $"{x},{y}"),
                new XAttribute("Size", $"{LabelW},{LabelH}"),
                new XAttribute("FontSize", FontSize.ToString()),
                new XAttribute("ThemeStyleId", "Variant2"),
                //Vit.G//251014 : 조회 필지 => ForegroundId, StrokeId 속성 추가
                bfocus ? new XAttribute("ForegroundId", "Accent5") : null,
                bfocus ? new XAttribute("StrokeId", "Accent5") : null,
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
        string exePath = Assembly.GetExecutingAssembly().Location;
        string exeDir = Path.GetDirectoryName(exePath);
        // 상대경로 폴더명 지정
        string folderPath = Path.Combine(exeDir, "_tempdir");
        String pathxml = folderPath + @"\" + _pnu + ".xml";
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
            focus ? new XAttribute("BackgroundId", "Accent5") : null,//Vit.G//251014 : 조회 필지 => BackgroundId 속성 추가
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
                focus ? new XAttribute("StrokeId", "Accent5") : null,//Vit.G//251014 : 조회 필지 => StrokeId 속성 추가
                new XAttribute("Points", "(Empty)"),
                new XAttribute("ItemKind", "DiagramConnector"),
                new XAttribute("BeginPoint", $"{x},{y}"),
                _isPortrait ? new XAttribute("EndPoint", $"{x},{y2}") : new XAttribute("EndPoint", $"{x2},{y}"),
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
                    focus ? new XAttribute("StrokeId", "Accent5") : null,//Vit.G//251014 : 조회 필지 => StrokeId 속성 추가
                    _isPortrait ? new XAttribute("BeginItemPointIndex", "2") : new XAttribute("BeginItemPointIndex", "1"),
                    _isPortrait ? new XAttribute("EndItemPointIndex", "0") : new XAttribute("EndItemPointIndex", "3"),
                    new XAttribute("Points", points),
                    new XAttribute("ItemKind", "DiagramConnector"),
                    new XAttribute("BeginItem", begin),
                    new XAttribute("EndItem", end),
                    _isPortrait ? new XAttribute("BeginPoint", $"{x},{y}") : new XAttribute("BeginPoint", $"{x},{y2}"),
                    new XAttribute("EndPoint", $"{x2},{y2}"),
                    new XAttribute("KeepMiddlePoints", "true")
                );
        }

        _children?.Add(item);
        _itemList.Add(item.ToString());
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
    
    // ===========================================================
    // 메인 실행 로직
    // ===========================================================
    public XDocument Run(List<LandMoveInfo> flowList, string pnu, bool isChecked, bool portrait)//Vit.G//[add]pnu, isChecked[지목변경 표시], portrait[세로형 그리기]
    {
        try
        {
            // 시군구 코드 등 공통 코드 조회
            ReadCodeTables();

            //Vit.G//조회 필지코드(19자리)
            _pnu = pnu;
            _isJimokChgShow = isChecked;
            _isPortrait = portrait;//Vit.G//[TODO]


            ProcessLandMoveFlow(flowList);
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