using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper;
using CsvHelper.Configuration;
using DevExpress.CodeParser;
using DevExpress.Diagram.Core.Native;
using DevExpress.Mvvm.Native;
using DevExpress.Office.Utils;
using DevExpress.Xpf.CodeView;
using DevExpress.Xpf.Diagram;
using DevExpress.Xpo;
using DevExpress.XtraCharts.Native;
using DevExpress.XtraPrinting.XamlExport;
using DevExpress.XtraScheduler.Drawing;
using DevExpress.XtraSpreadsheet.DocumentFormats.Xlsb;
using DevExpress.XtraSpreadsheet.Model;
using LMFS.Db;
using LMFS.Models;
using LMFS.Services;
using LMFS.ViewModels.Pages;
using LMFS.Views.Pages;
using MySqlConnector;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;


namespace LMFS.Engine;

public class CsvUploader
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


    // CSV 원본 → List<LandMoveCsv> records
    public static List<LandMoveInfo> RecordsMove;  // DF_MOVE 역할 (MVVM에서는 ObservableProperty로 뺄 수도 있음)
    public static ObservableCollection<LandMovePnuList> PnuListAll;
    public static ObservableCollection<LandMovePnuList> PnuList;

    // [디버깅용]
    public static ObservableCollection<LandMovePnuList> PnuListDebug;
    public static List<LandMoveInfo> RecordsMoveDebug;


    // -----------------------------------------------------------
    // DBMS 조회 데이터 및 그룹 정보
    // -----------------------------------------------------------
    public static int _groupSeqno = 0;

    // -----------------------------------------------------------
    // DBMS 조회 데이터 및 그룹 정보
    // -----------------------------------------------------------
    //private List<int> _groupList = new();
    //private int _currentGroupNo;
    //private List<string> _dbLines = new();


    // -----------------------------------------------------------
    // 임시 파일 관련 정보
    // -----------------------------------------------------------
    private static string _tempDir;

    #endregion


    #region 생성자     ----------------------------------------

    //Page를 직접 접근하지 않고, ViewModel을 통해서 접근하기//
    public CsvUploader()
    {
        // 임시 파일 저장할 경로 설정//
        _tempDir = SetTempDir();
    }
    #endregion 생성자  ----------------------------------------



    #region 디버깅용-파일 저장
    //임시 파일 저장할 폴더 생성
    public static string SetTempDir()
    {
        string exePath = Assembly.GetExecutingAssembly().Location;
        // 상대경로 폴더명 지정
        return Path.Combine(Path.GetDirectoryName(exePath), "_tempdir");
    }

    public static void SavePnuListToCsv(IEnumerable<LandMovePnuList> pnuList, string filePath)
    {
        using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
        {
            // Header
            writer.WriteLine("pnu,bChecked");
            foreach (var item in pnuList)
            {
                writer.WriteLine($"{item.pnu},{item.bChecked}");
            }
        }
    }
    public static void SaveRecordMoveToCsv(IEnumerable<LandMoveInfo> records, string filePath)
    {
        using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
        {
            // Header
            writer.WriteLine("bfPnu,afPnu,regDt,rsn,ownName,ownAddr");
            foreach (var r in records)
            {
                writer.WriteLine($"{r.bfPnu},{r.afPnu},{r.regDt},{r.rsn},{r.ownName}");
            }
        }
    }
    #endregion

    #region 공통코드
    // func_get_code_value 역할 함수 (list_MOVRSN은 Dictionary<string, string>)
    static string GetCodeValue(string find)
    {
        return GlobalDataManager.Instance.ReasonCode.FirstOrDefault(kv => kv.Value == find).Key;
    }
    #endregion

    #region 공통함수
    // 숫자 천 단위 콤마(,) 표기
    private static string ConvertNumberFormat(string num)
    {
        if (decimal.TryParse(num?.ToString(), out decimal number))
        {
            return number.ToString("#,##0");
        }
        return num;
        ;
    }
    #endregion


    #region 데이터 가져오기
    //1. func_loadcsv_dir_files
    //(여러 디렉터리의 CSV 파일 로드)
    public static void LoadLandMoveCsvFiles(string folderPath, Action<string, string, string, int, int> onFileRoad = null /* Callback 추가 */)
    {
        // 임시 파일 저장할 경로 설정//
        _tempDir = SetTempDir();



        var allRecords = new List<LandMoveInfo>();
        foreach (var file in Directory.GetFiles(folderPath, "토지이동정리현황*.csv", SearchOption.AllDirectories))
        {
            using (var reader = new StreamReader(file, Encoding.GetEncoding("euc-kr"))) // 한글 파일(euc-kr 인코딩)
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ",",//구분자 지정(필요 시 ;,\t 등으로 변경)
                    HeaderValidated = null, //헤더 검증 예외 무시
                    MissingFieldFound = null//없는 필드 예외 무시
                };
                using (var csv = new CsvReader(reader, config))
                {
                    try
                    {
                        var records = csv.GetRecords<LandMoveCsv>().ToList();
                        int recCnt = records.Count;//(가공전) 원본 레코드 개수
                        string sttDt = string.Empty;//(가공후) 레코드 시작일자
                        string lstDt = string.Empty;//(가공후) 레코드 종료일자

                        // -------- 여기가 데이터 가공(정제, 컬럼 변환) 처리 구간 --------
                        foreach (var r in records)
                        {
                            // [-- 전처리 --] 문자열 시작의 ' ' 공백 제거 - 문자열 속성을 모두 처리
                            if (!string.IsNullOrEmpty(r.rsn)) r.rsn = r.rsn.TrimStart();
                            if (!string.IsNullOrEmpty(r.seq)) r.seq = r.seq.TrimStart();
                            if (!string.IsNullOrEmpty(r.regDt)) r.regDt = r.regDt.TrimStart();
                            if (!string.IsNullOrEmpty(r.lawd)) r.lawd = r.lawd.TrimStart();
                            if (!string.IsNullOrEmpty(r.lndGbn)) r.lndGbn = r.lndGbn.TrimStart();
                            if (!string.IsNullOrEmpty(r.bfJibun)) r.bfJibun = r.bfJibun.TrimStart();
                            if (!string.IsNullOrEmpty(r.bfJimok)) r.bfJimok = r.bfJimok.TrimStart();
                            if (!string.IsNullOrEmpty(r.bfArea)) r.bfArea = r.bfArea.TrimStart();
                            if (!string.IsNullOrEmpty(r.afJibun)) r.afJibun = r.afJibun.TrimStart();
                            if (!string.IsNullOrEmpty(r.afJimok)) r.afJimok = r.afJimok.TrimStart();
                            if (!string.IsNullOrEmpty(r.afArea)) r.afArea = r.afArea.TrimStart();
                            if (!string.IsNullOrEmpty(r.ownName)) r.ownName = r.ownName.TrimStart();
                            if (!string.IsNullOrEmpty(r.ownAddr)) r.ownAddr = r.ownAddr.TrimStart();
                            if (!string.IsNullOrEmpty(r.bfPnu)) r.bfPnu = r.bfPnu.TrimStart();
                            if (!string.IsNullOrEmpty(r.afPnu)) r.afPnu = r.afPnu.TrimStart();
                            /*
                            방법 2: 리플렉션으로 모든 string 프로퍼티 공통 처리
                            csharp
                            using System.Reflection;
                            foreach (var r in allRecords)
                            {
                                var properties = r.GetType().GetProperties()
                                    .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite);

                                foreach (var prop in properties)
                                {
                                    var val = prop.GetValue(r) as string;
                                    if (!string.IsNullOrEmpty(val))
                                        prop.SetValue(r, val.TrimStart());
                                }
                            }
                            이 코드는 모든 string 타입 public property를 자동으로 TrimStart, 즉 문자열 앞 공백 제거합니다.
                            실무에서는 주요 속성에만 쓰는 게 안전하지만,
                            "데이터가 항상 모두 문자열"이거나 "모든 string에 적용이 무방"하다면 위 방식이 상당히 편리합니다.
                            */


                            // [-- 전처리 --] 지목코드 2자리 추출 (BF, AF)
                            if (!string.IsNullOrEmpty(r.bfJimok) && r.bfJimok.Length > 2)
                                r.bfJimok = r.bfJimok.Substring(0, 2);
                            if (!string.IsNullOrEmpty(r.afJimok) && r.afJimok.Length > 2)
                                r.afJimok = r.afJimok.Substring(0, 2);

                            // [-- 전처리 --] 불필요한 텍스트 제거
                            if (!string.IsNullOrEmpty(r.rsn))
                            {
                                r.rsn = r.rsn.Replace("(토지대장)", "");
                                r.rsn = r.rsn.Replace("(임야대장)", "");
                            }


                            // [-- 전처리 --] 문자열 조합 => bfPnu, afPnu 생성
                            string jibun = "";
                            string bobn = "";
                            string bubn = "";

                            jibun = r.bfJibun ?? ""; // Null 안전 처리
                            bobn = jibun.Length >= 4 ? jibun.Substring(0, 4) : "";
                            bubn = jibun.Length >= 9 ? jibun.Substring(5, 4) : "";
                            r.bfPnu = (r.lawd?.ToString() ?? "")
                                        + (r.lndGbn?.ToString() ?? "")
                                        + bobn
                                        + bubn;

                            jibun = r.afJibun ?? ""; // Null 안전 처리
                            bobn = jibun.Length >= 4 ? jibun.Substring(0, 4) : "";
                            bubn = jibun.Length >= 9 ? jibun.Substring(5, 4) : "";
                            r.afPnu = (r.lawd?.ToString() ?? "")
                                        + (r.rsn == "등록전환" ? "1" : r.lndGbn)
                                        + bobn
                                        + bubn;

                            // 필요시 코드 변환 딕셔너리 활용, 날짜 파싱, 기타 로직 (함수로 분리하여 작업)
                        }

                        // [-- 전처리 --] 문자열 길이로 레코드 필터링
                        records = records.Where(r => !string.IsNullOrEmpty(r.bfPnu) && r.bfPnu.Length == 19).ToList();
                        records = records.Where(r => !string.IsNullOrEmpty(r.afPnu) && r.afPnu.Length == 19).ToList();

                        // [-- 전처리 --] 중복·NaN 레코드 제거
                        records = records
                            .GroupBy(r => new { /* 중복 판단 기준 */ r.regDt, r.rsn, r.bfPnu, r.afPnu })
                            .Select(g => g.First())
                            .Where(r => /* NaN 제거: 주요 필드 null/빈값 필터 */
                                !string.IsNullOrEmpty(r.bfPnu)
                                && !string.IsNullOrEmpty(r.afPnu))
                            .ToList();


                        // [-- 전처리 --] 명칭 => 코드 변환
                        // records 리스트의 각 요소에 대해 [토지이동종목]을 변환
                        foreach (var record in records)
                        {
                            record.rsn = GetCodeValue(record.rsn);
                        }

                        // DataTable: DF_MOVE
                        // 컬럼명: "정리일자" (DateTime 타입이라고 가정)
                        sttDt = records.Min(r => r.regDt);
                        lstDt = records.Max(r => r.regDt);


                        // ----------------------------------------------------------
                        // 파일명 콜백 전달
                        //[디버깅용]
                        //MessageBox.Show($"콜백 호출 준비: {file}, {sttDt}, {lstDt}, {recCnt.ToString()}, {records.Count.ToString()}");
                        onFileRoad?.Invoke(Path.GetFileName(file), sttDt, lstDt, recCnt, records.Count);

                        // ----------------------------------------------------------
                        //allRecords.AddRange(records);
                        // <LandMoveCsv> => <LandMoveInfo>
                        allRecords.AddRange(ConvertDataCsvToInfo(records));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                }
            }
        }

        // <LandMoveInfo>
        RecordsMove = allRecords;
    }


    public static void UploadLandMoveToDB(Action<int, int, string> onProgress = null)
    {
        // <LandMoveInfo>RecordsMove

        // records가 IEnumerable<T> 또는 List<T> 타입일 때
        //records가 정말 null로 나오는 경우를 방지하려면, 위처럼 ?.Count() 또는 .ToList().Count 사용
        int totalCount = RecordsMove?.Count() ?? 0; // .Count() 확장메서드 사용, null 방지

        if (totalCount == 0)
        {
            MessageBox.Show("업로드할 데이터가 없습니다!");
            return;
        }

        onProgress?.Invoke(0, totalCount, "업로드 준비 중...");

        try
        {
            //------------------------------------
            // [1] PNU 합침 (bfPnu, afPnu)
            //------------------------------------
            onProgress?.Invoke(0, totalCount, "PNU 목록 생성 중...");
            UpdatePnuList();


            //------------------------------------
            // [2] 기존자료 백업 및 작업 테이블 준비
            //------------------------------------
            onProgress?.Invoke(0, totalCount, "기존 자료 백업 중...");
            if (DBService.BackupLandMoveInfoOrg() <= 0)
            {
                MessageBox.Show("기존 자료 백업에 문제가 발생했습니다. 사업수행자에게 문의하세요.");
                return;
            }
            if (DBService.CreateLandMoveInfoUser() <= 0)
            {
                MessageBox.Show("기존 자료 백업에 문제가 발생했습니다. 사업수행자에게 문의하세요.");
                return;
            }


            //------------------------------------
            // [3] 현재 DB g_seq 최대값을 가져온다. (새로 추가할 데이터는 g_seq를 새로 count할 것이므로)
            //------------------------------------
            onProgress?.Invoke(0, totalCount, "그룹 번호 확인 중...");
            _groupSeqno = DBService.GetMaxGroupSeqno();


            //------------------------------------
            // [4] 업로드 한 데이터 기준으로
            // 1. 그룹핑 >
            // 2. 기존 필지 찾기 >
            // 3. Merge(new+old) >
            // 4. 중복제거 >
            // 5. 기존 데이터 레코드 delete >
            // 6. 머지된 데이터 insert
            //------------------------------------
            onProgress?.Invoke(0, totalCount, "데이터 업로드 중...");
            GetLandMoveAll(onProgress, totalCount);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"업로드 중 오류 발생: {ex.Message}");
        }
        finally
        {
            onProgress?.Invoke(totalCount, totalCount, "업로드 완료!");
            MessageBox.Show("업로드가 완료되었습니다!");
        }
    }
    #endregion




    #region PNU 합침
    public static void UpdatePnuList()
    {
        // records는 List<LandMoveCsv> 또는 IEnumerable<LandMoveCsv>
        var listA = RecordsMove
            .Select(r => r.bfPnu)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
        // records는 List<LandMoveCsv> 또는 IEnumerable<LandMoveCsv>
        var listB = RecordsMove
            .Select(r => r.bfPnu)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        //
        var merged = CsvUploader
            .MergePnuAll(listA, listB)
            .Distinct()
            .OrderBy(p => p);

        //
        PnuListAll = new ObservableCollection<LandMovePnuList>();
        PnuListAll.Clear();
        foreach (var pnu in merged)
            PnuListAll.Add(new LandMovePnuList { pnu = pnu, bChecked = false });

        // [디버깅용]
        PnuListDebug = new ObservableCollection<LandMovePnuList>();
        PnuListDebug.Clear();
        RecordsMoveDebug = new List<LandMoveInfo>();
        RecordsMoveDebug.Clear();
    }
    #endregion


    #region 데이터 그룹핑
    //2. func_merge_pnuall
    //(PNU 리스트 병합)
    //ViewModel에서 함수명 바로 호출하려면 static 필요
    //아니면 
    //ViewModel에서 var uploader = new CsvUploader();
    //var mergedPnu = uploader.MergePnuAll(listA, listB) ...처럼 사용
    public static List<string> MergePnuAll(List<string> listA, List<string> listB)
    {
        return listA.Union(listB).Distinct().ToList();
    }

    //List<string> 구조를 LandMovePnuList 구조로 변경
    public static List<LandMovePnuList> ConvertToLandMovePnuList(List<string> mergedPnu)
    {
        return mergedPnu
            .Select(p => new LandMovePnuList { pnu = p, bChecked = false })
            .ToList();
    }


    //(py)func_query_pnulist
    // 1. 연관 필지 찾기 >
    // 2. bfPnu, afPnu => Pnu로 합침(MergePnu) //
    public static bool QueryPnuList(string landcd)
    {
        var filtered = RecordsMove
            .Where(r => r.bfPnu == landcd || r.afPnu == landcd)
            .Select(r => new LandMovePnuPair { bfPnu = r.bfPnu, afPnu = r.afPnu })
            .ToList();

        if (filtered.Count == 0) return false;

        return MergePnu(filtered, landcd);
    }

    //(py)func_merge_pnu
    // bfPnu, afPnu => Pnu로 합침 //
    public static bool MergePnu(List<LandMovePnuPair> filtered, string landcd)
    {
        // 1. 빈 컬렉션 생성
        var tempList = new List<LandMovePnuList>();

        // 2. bfPnu/afPnu concat & checked 설정
        foreach (var r in filtered)
        {
            tempList.Add(new LandMovePnuList
            {
                pnu = r.bfPnu,
                bChecked = (r.bfPnu == landcd)
            });
            tempList.Add(new LandMovePnuList
            {
                pnu = r.afPnu,
                bChecked = (r.afPnu == landcd)
            });
        }
        // 중복 제거 & NaN 제거
        tempList = tempList
            .Where(x => !string.IsNullOrWhiteSpace(x.pnu))
            .GroupBy(x => x.pnu)
            .Select(g => new LandMovePnuList
            {
                pnu = g.Key,
                bChecked = g.Any(y => y.bChecked)
            })
            .OrderBy(x => x.pnu)
            .ToList();

        // 기존 값 병합 (DF_PNU 역할)
        foreach (var item in tempList)
        {
            var exist = PnuList.FirstOrDefault(x => x.pnu == item.pnu);
            if (exist == null)
                PnuList.Add(item);
            else if (item.bChecked)
                exist.bChecked = true;
        }

        return true;
    }

    //(py)func_drop_duplicates
    // 중복 제거//
    public static List<LandMovePnuList> DropDuplicatesPnu(IEnumerable<LandMovePnuList> src)
    {
        return src
            .Where(x => !string.IsNullOrWhiteSpace(x.pnu))
            .GroupBy(x => x.pnu)
            .Select(g => new LandMovePnuList { pnu = g.Key, bChecked = g.Any(y => y.bChecked) })
            .ToList();
    }

    public static List<LandMoveInfo> DropDuplicatesInfo(IEnumerable<LandMoveInfo> src)
    {
        return src
                .GroupBy(x => new { x.regDt, x.rsn, x.bfPnu, x.afPnu })
                .Select(g => g.First())
                .ToList();
    }

    //(py)func_query_movedata
    // drop: 조회된 landcd 레코드 삭제 여부
    public static List<LandMoveInfo> QueryMoveData(string landcd, bool drop)
    {
        var filtered = RecordsMove
            .Where(r => r.bfPnu == landcd || r.afPnu == landcd)
            .OrderBy(r => r.regDt)
            .ThenBy(r => r.rsn)  // "토지이동종목"
            .ThenBy(r => r.bfPnu)
            .ThenBy(r => r.afPnu)
            .ToList();

        if (drop)
        {
            RecordsMove = RecordsMove
                .Where(r => !(r.bfPnu == landcd || r.afPnu == landcd))
                .ToList();
        }
        return filtered;
    }

    //(py)func_get_landmove_partial 
    //[Important("이 함수는 핵심 로직입니다")]
    // 현재 필지 기준 => 연관 필지 Recursive 조회
    public static void GetLandMovePartial(string landcd)
    {
        if (!QueryPnuList(landcd))// 연관 필지 찾기//
            return;
        if (PnuList.Count == 0)
            return;




        // [디버깅용]
        bool exists = PnuList.Any(item => item.pnu == "4420010100101910010");
        if(exists)
        {
            int a = 1;
        }

        



        //----------------------------------------
        // 연관 필지 조회(확인) : 시작 //
        int idx = 1;
        while (idx < PnuList.Count)
        {
            if (PnuList[idx].bChecked)
            {
                idx++;
                continue;
            }
            var pnu = PnuList[idx].pnu;
            if (!QueryPnuList(pnu))// 연관 필지 찾기//
            {
                idx++;
                continue;
            }
            idx++;
        }
        // 연관 필지 조회(확인) : 끝 //
        //----------------------------------------


        //----------------------------------------
        // 개별 필지별 [이동정리현황] 데이터 가져오기 (from. RecordsMove)
        var resultData = new List<LandMoveInfo>();
        foreach (var pnulist in PnuList)
        {
            //RecordsMove에서 데이터 가져오고 RecordsMove에서 삭제
            var qry = QueryMoveData(pnulist.pnu, true);
            resultData.AddRange(qry);
        }


        //----------------------------------------
        // (신규) 최종 레코드
        if (resultData.Count > 0)
        {
            // (1) 중복제거: 예시로, PNU, 정리일자, MoveType로 중복 제거
            resultData = DropDuplicatesInfo(resultData);

            // (2) 필요없는 컬럼 제거(예시: LandOwnerAddress)
            // -> 클래스 정의에서 제외하면 됨 (혹은 csv 저장 시 제외)

            // (3) 정렬
            resultData = resultData
                .OrderBy(x => x.regDt)
                .ThenBy(x => x.rsn)
                .ThenBy(x => x.bfPnu)
                .ThenBy(x => x.afPnu)
                .ToList();

            // (4) GROUP_SEQNO 부여 (전역 static int 사용)
            _groupSeqno += 1;

            // (5) DB에서 기존 그룹 데이터 가져와서 병합
            // 예시:
            var dbFlowList = SqlQueryFlowList(); // List<LandMoveInfo>
            if (dbFlowList.Count > 0)
            {
                // 'SEQ' 컬럼 제거는 클래스에서 속성 제외
                resultData.AddRange(dbFlowList);
                // 다시 중복 제거
                resultData = DropDuplicatesInfo(resultData);
            }


            // (6) 그룹번호 재부여
            foreach (var item in resultData) item.gSeq = _groupSeqno;

            // (7) PNU_SEQ 그룹순번 부여 (예를 들어, AfPnu 기준 그룹화)
            var grouped = resultData.GroupBy(x => x.afPnu).ToList();
            int groupSeq = 0;
            foreach (var group in grouped)
            {
                foreach (var row in group)
                {
                    row.pSeq = groupSeq;
                }
                groupSeq++;
            }

            // (8) DB 에 <기존+신규 레코드> Insert
            DBService.InsertLandMoveUpload(resultData);


            // [디버깅용] 최종 CSV 저장 등
            //string pathcsv = Path.Combine(_tempDir, $"ResultMove_{_groupSeqno}.csv");
            //SaveRecordMoveToCsv(resultData, pathcsv);
            // [디버깅용] 
            foreach (var item in PnuList)
            {
                PnuListDebug.Add(item);
            }
            foreach (var item in resultData)
            {
                RecordsMoveDebug.Add(item);
            }

        }
        //----------------------------------------


        //// [디버깅용]
        //pathcsv = Path.Combine(_tempDir, $"PnuListAll.csv");
        //SavePnuListToCsv(PnuListDebug, pathcsv);
        //pathcsv = Path.Combine(_tempDir, $"RecordMoveAll.csv");
        //SaveRecordMoveToCsv(RecordsMoveDebug, pathcsv);
    }

    //(py)func_get_landmove_all 
    //------------------------------------
    // [4] 업로드 한 데이터 기준으로
    // 1. 그룹핑 >
    // 2. 기존 필지 찾기 >
    // 3. Merge(new+old) >
    // 4. 중복제거 >
    // 5. 기존 데이터 레코드 delete >
    // 6. 머지된 데이터 insert
    //------------------------------------
    // GetLandMoveAll에 진행률 콜백 추가
    public static void GetLandMoveAll(Action<int, int, string> onProgress = null, int totalCount = 0)
    {
        // 기존 로직에 진행률 업데이트 추가
        // 예: 처리된 레코드 수를 추적
        int processedCount = 0;

        // 실제 처리 로직...
        // 중간중간 진행률 업데이트
        onProgress?.Invoke(processedCount, totalCount, $"처리 중... {processedCount}/{totalCount}");

        // 전체 PNU 기준
        for (int idx = 0; idx < PnuListAll.Count; idx++)
        {
            if (PnuListAll[idx].bChecked)
                continue;

            var pnu = PnuListAll[idx].pnu;

            PnuList = new ObservableCollection<LandMovePnuList>();
            PnuList.Clear();

            //------------------------------------
            // [★★★] 현재 필지 기준 => 연관 필지 Recursive 조회
            //------------------------------------
            GetLandMovePartial(pnu);

            // 처리된 필지로 반영
            foreach (var checkedPnu in PnuList.Where(x => x.bChecked))
                PnuListAll.Where(x => x.pnu == checkedPnu.pnu).ToList().ForEach(x => x.bChecked = true);

            // 필지 리스트 초기화
            PnuList.Clear();

            // 실제 처리 로직...
            // 중간중간 진행률 업데이트
            processedCount = totalCount - RecordsMove.Count;
            if (processedCount % 100 == 0 || processedCount == totalCount)
                onProgress?.Invoke(processedCount, totalCount, $"처리 중... {ConvertNumberFormat(processedCount.ToString())}/{ConvertNumberFormat(totalCount.ToString())}");

        }


        // [디버깅용]
        string pathcsv = Path.Combine(_tempDir, $"PnuListAll.csv");
        SavePnuListToCsv(PnuListDebug, pathcsv);
        pathcsv = Path.Combine(_tempDir, $"RecordMoveAll.csv");
        SaveRecordMoveToCsv(RecordsMoveDebug, pathcsv);

    }

    public static List<LandMoveInfo> ConvertDataCsvToInfo(List<LandMoveCsv> listCsv)
    {
        // 확장 메서드 사용
        List<LandMoveInfo> listInfo = listCsv
            .Select(csv => csv.ToLandMoveInfo())
            .ToList();

        return listInfo;
    }
    #endregion



    #region 그룹핑한 데이터 + 기존 DB 데이터 Merge => DB Update
    //5. func_sql_query_flowlist
    //(Flowlist DB 쿼리)
    public static List<LandMoveInfo> SqlQueryFlowList()
    {
        List<LandMoveInfo> listOldDBResult = new List<LandMoveInfo>();
        if (PnuList == null || PnuList.Count == 0)
            return listOldDBResult;

        List<LandMovePnuList> list = new List<LandMovePnuList>();
        //ObservableCollection<T>을 List<T>로 직접 할당할 수 없기 때문에 발생합니다.
        //두 타입은 상속 관계가 아니므로 암시적 변환이 불가능합니다.
        //ToList() 사용(권장)
        list = PnuList.ToList();
        List<int> listGrp = DBService.QueryGroupListFromDB(list);// 그룹목록 조회
        if (listGrp.Count > 0)
        {
            listOldDBResult = DBService.QueryFlowListFromDB(listGrp);//그룹목록에 해당하는 레코드 조회

            if (listOldDBResult.Count > 0)
            {
                DBService.DeleteGroupListFromDB(listGrp);//그룹목록에 해당하는 레코드 삭제
            }
        }

        return listOldDBResult;
    }

    //6. func_merge_dfmoveall
    //(두 리스트 데이터 합치고 중복제거)
    public List<LandMoveCsv> MergeDfMoveAll(List<LandMoveCsv> oldRecords, List<LandMoveCsv> newRecords)
    {
        var merged = oldRecords.Concat(newRecords)
            .GroupBy(r => new { r.bfPnu, r.afPnu }) // 키 기준(예시)
            .Select(g => g.First())
            .ToList();
        return merged;
    }

    //7. func_sql_write_by_group2
    //(Grouping 결과 DB Insert)
    public void SqlWriteByGroup2(MySqlConnection conn, List<LandMoveCsv> groupRecords)
    {
        string query = @"
      INSERT INTO group_landmoveflow_his (GROUPSEQNO, SEQ, bfPnu, afPnu, ... )
      VALUES (@groupseqno, @seq, @bfpnu, @afpnu, ... )";
        foreach (var rec in groupRecords)
        {
            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@groupseqno", rec.gSeq);
                cmd.Parameters.AddWithValue("@seq", rec.idx);
                cmd.Parameters.AddWithValue("@bfpnu", rec.bfPnu);
                cmd.Parameters.AddWithValue("@afpnu", rec.afPnu);
                // ... 모든 파라미터
                cmd.ExecuteNonQuery();
            }
        }
    }

    //8. func_get_landmove_all
    //(전체 이동 필지 데이터 필터/정렬)
    public List<LandMoveCsv> GetLandMoveAll(List<LandMoveCsv> records)
    {
        // 예: bfPnu/afPnu 기준 정렬 및 가공
        return records.OrderBy(r => r.bfPnu).ThenBy(r => r.afPnu).ToList();
    }
    #endregion



    // ===========================================================
    // 메인 실행 로직
    // ===========================================================

}