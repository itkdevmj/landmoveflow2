using CsvHelper;
using DevExpress.CodeParser;
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
using MySqlConnector;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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

    // -----------------------------------------------------------
    // DBMS 조회 데이터 및 그룹 정보
    // -----------------------------------------------------------
    //private List<int> _groupList = new();
    //private int _currentGroupNo;
    //private List<string> _dbLines = new();


    #endregion


    #region 생성자     ----------------------------------------

    //Page를 직접 접근하지 않고, ViewModel을 통해서 접근하기//
    public CsvUploader()
    {
//
    }
    #endregion 생성자  ----------------------------------------



    #region 데이터 가져오기
    //1. func_loadcsv_dir_files
    //(여러 디렉터리의 CSV 파일 로드)
    public static List<LandMoveCsv> LoadLandMoveCsvFiles(string folderPath, Action<string, string, string, int, int> onFileRoad = null /* Callback 추가 */)
    {
        var allRecords = new List<LandMoveCsv>();
        foreach (var file in Directory.GetFiles(folderPath, "토지이동정리현황*.csv", SearchOption.AllDirectories))
        {
            using (var reader = new StreamReader(file))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<LandMoveCsv>().ToList();
                int recCnt = records.Count;//(가공전) 원본 레코드 개수
                string sttDt = string.Empty;//(가공후) 레코드 시작일자
                string lstDt = string.Empty;//(가공후) 레코드 종료일자

                // -------- 여기가 데이터 가공(정제, 컬럼 변환) 처리 구간 --------
                foreach (var r in records)
                {
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

                    // [-- 전처리 --] 문자열 길이로 레코드 필터링
                    records = records.Where(r => !string.IsNullOrEmpty(r.bfPnu) && r.bfPnu.Length == 19).ToList();
                    records = records.Where(r => !string.IsNullOrEmpty(r.afPnu) && r.afPnu.Length == 19).ToList();

                    // [-- 전처리 --] 중복·NaN 레코드 제거
                    records = records
                        .GroupBy(r => new { /* 중복 판단 기준, 예시: r.BF_PNU, r.afPnu 등 */ r.bfPnu, r.afPnu })
                        .Select(g => g.First())
                        .Where(r => /* NaN 제거: 주요 필드 null/빈값 필터 */
                            !string.IsNullOrEmpty(r.bfPnu)
                            && !string.IsNullOrEmpty(r.afPnu))
                        .ToList();

                    // DataTable: DF_MOVE
                    // 컬럼명: "정리일자" (DateTime 타입이라고 가정)

                    sttDt = records.Min(r => r.regDt);
                    lstDt = records.Max(r => r.regDt);
                    //sttDt = records.AsEnumerable()
                    //    .Min(r => DateTime.ParseExact(r.Field<string>("regDt"), "yyyy-MM-dd", CultureInfo.InvariantCulture)).ToString("yyyy-MM-dd");
                    //lstDt = records.AsEnumerable()
                    //    .Max(r => DateTime.ParseExact(r.Field<string>("regDt"), "yyyy-MM-dd", CultureInfo.InvariantCulture)).ToString("yyyy-MM-dd");


                    // 필요시 코드 변환 딕셔너리 활용, 날짜 파싱, 기타 로직 (함수로 분리하여 작업)
                }

                // ----------------------------------------------------------
                // 파일명 콜백 전달
                onFileRoad?.Invoke(Path.GetFileName(file), sttDt, lstDt, recCnt, records.Count);

                // ----------------------------------------------------------
                allRecords.AddRange(records);
            }
        }
        return allRecords;
    }
    #endregion



    #region 데이터 그룹핑
    //2. func_merge_pnuall
    //(PNU 리스트 병합)
    public List<string> MergePnuAll(List<string> listA, List<string> listB)
    {
        return listA.Union(listB).Distinct().ToList();
    }

    //3. func_get_landmove_partial
    //(부분 필지 이동 데이터 필터링)
    public List<LandMoveCsv> GetLandMovePartial(List<LandMoveCsv> records, Func<LandMoveCsv, bool> filter)
    {
        return records.Where(filter).ToList();
    }

    //4. func_query_movedata
    //(DB 쿼리로 이동 데이터 획득)
    public List<LandMoveCsv> QueryMoveData(MySqlConnection conn, List<string> pnuList)
    {
        var result = new List<LandMoveCsv>();
        var pnuParams = string.Join(",", pnuList.Select(p => $"'{p}'"));
        string query = $"SELECT * FROM group_landmoveflow_his WHERE bfPnu IN ({pnuParams}) OR afPnu IN ({pnuParams})";
        using (var cmd = new MySqlCommand(query, conn))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                //result.Add(ConvertToLandMoveCsv(reader));
            }
        }
        return result;
    }

    //public List<LandMoveCsv> ConvertToLandMoveCsv(MySqlDataReader reader)
    //{
    //    ////// DB 컬럼명과 클래스 프로퍼티명에 맞게 변환
    //    return new LandMoveCsv { }
    //    {
    //        bfPnu = reader["BFPNU"].ToString(),
    //        afPnu = reader["AFPNU"].ToString()
    //        //seq = reader["SEQNO"] is DBNull ? 0 : Convert.ToInt32(reader["SEQNO"]),
    //        //rsn = reader["LANDMOVRSN"]?.ToString()
    //        //regDt = reader["CREYMD"]?.ToString(),
    //        //ownName = reader["OWNNAME"]?.ToString(),
    //        //ownAddr = reader["OWNADDR"]?.ToString(),
    //        //bfJimok = reader["JIMOK"]?.ToString(),
    //        //LANDAREA = reader["LANDAREA"] is DBNull ? 0.0 : Convert.ToDouble(reader["LANDAREA"]),
    //        // ... 기타 필요한 컬럼을 동일하게 추가.
    //    };
    //}
    #endregion



    #region 그룹핑한 데이터 + 기존 DB 데이터 Merge => DB Update
    //5. func_sql_query_flowlist
    //(Flowlist DB 쿼리)
    public List<LandMoveCsv> SqlQueryFlowList(MySqlConnection conn, List<string> pnuList)
    {
        return QueryMoveData(conn, pnuList); // 위와 동일 구조(조건에 맞는 SELECT)
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