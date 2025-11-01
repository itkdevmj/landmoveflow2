﻿using Dapper;
using DevExpress.XtraCharts.Native;
using LMFS.Models;
using LMFS.Services;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Windows;

namespace LMFS.Db
{
    public static class DBService
    {
        #region 코드성 데이터 조회
        // 시도코드 조회
        public static List<SidoCode> ListSidoCode(string sidoSggCd)
        {
            using (var connection = MariaDBConnection.connectDB())
            {
                string query =
                    $"""
                     SELECT sidosgg_cd  AS sidoSggCd
                          , umd_cd      AS umdCd
                          , ri_cd       AS riCd
                          , sido_nm     AS sidoNm
                          , sgg_nm      AS sggNm
                          , umd_nm      AS umdNm
                          , ri_nm       AS riNm
                       FROM sido_code
                      WHERE sidosgg_cd = '{sidoSggCd}'
                     ORDER BY sidosgg_cd, umd_cd, ri_cd 
                     """;
                return connection.Query<SidoCode>(query).ToList();
            }
        }

        // 공통코드(지목) 조회
        public static Dictionary<string, string> GetJimokDictionary(string grpCd)
        {
            using (var connection = MariaDBConnection.connectDB())
            {
                string query =
                    $"""
                     SELECT substring(comm_cd, 5, 2)  AS jimokCd
                          , comm_nm  AS jimokNm
                       FROM comn_cd
                      WHERE comm_prnt = '{grpCd}'
                     ORDER BY comm_cd, comm_ord
                     """;

                var list = connection.Query<JimokCode>(query, new { GrpCd = grpCd }).ToList();
                return list.ToDictionary(x => x.jimokCd, x => x.jimokNm);
            }
        }

        // 공통코드(이동종목) 조회
        public static Dictionary<string, string> GetReasonDictionary(string grpCd)
        {
            using (var connection = MariaDBConnection.connectDB())
            {
                string query =
                    $"""
                     SELECT substring(comm_cd, 5, 2)  AS rsnCd
                          , comm_nm  AS rsnNm
                       FROM comn_cd
                      WHERE comm_prnt = '{grpCd}'
                     ORDER BY comm_cd, comm_ord
                     """;

                var list = connection.Query<ReasonCode>(query, new { GrpCd = grpCd }).ToList();
                return list.ToDictionary(x => x.rsnCd, x => x.rsnNm);
            }
        }

        // test
        public static List<LandMoveInfo> ListLandMoveInfo(string pnu)
        {
            using (var connection = MariaDBConnection.connectDB())
            {
                string query =
                    $"""
                     SELECT g_seq       AS gSeq
                          , idx         AS idx
                          , bf_pnu      AS bfPnu
                          , af_pnu      AS afPnu
                          , rsn         AS rsn
                          , reg_dt      AS regDt
                          , bf_jimok    AS bfJimok
                          , bf_area     AS bfArea
                          , af_jimok    AS afJimok
                          , af_area     AS afArea
                          , own_name    AS ownName
                          , p_seq       AS pSeq
                       FROM landmove_info
                      WHERE (bf_pnu = '{pnu}' OR af_pnu = '{pnu}'
                     """;
                return connection.Query<LandMoveInfo>(query).ToList();
            }
        }
        #endregion

        #region [토지이동] 데이터 조회
        // 프로그램 버전정보 조회
        public static VersionInfo SelectLastAppVersion()
        {
            using (var connection = MariaDBConnection.connectDB())
            {
                string query =
                    $"""
                     SELECT version AS Version, upd_url AS Url, rlz_note AS ReleaseNotes FROM version_info 
                     ORDER BY version DESC LIMIT 1
                     """;
                return connection.QuerySingle<VersionInfo>(query);
            }
        }
        
        // 토지이동연혁조회 (pnu)
        public static List<LandMoveInfo> ListLandMoveHistory(string pnu)
        {
            using (var connection = MariaDBConnection.connectDB())
            {
                string query =
                    $"""
                     SELECT g_seq       AS gSeq
                          , idx
                          , bf_pnu      AS bfPnu
                          , af_pnu      AS afPnu
                          , rsn
                          , reg_dt      AS regDt
                          , bf_jimok    AS bfJimok
                          , bf_area     AS bfArea
                          , af_jimok    AS afJimok
                          , af_area     AS afArea
                          , own_name    AS ownName
                          , p_seq       AS pSeq
                       FROM landmove_info
                      WHERE g_seq = ( SELECT g_seq
                                        FROM landmove_info
                                       WHERE af_pnu = '{pnu}' OR af_pnu = '{pnu}'
                                      LIMIT 1
                                    )
                     ORDER BY idx 
                     """;
                return connection.Query<LandMoveInfo>(query).ToList();
                //ORDER BY reg_dt, rsn, bf_pnu, af_pnu => idx로 정정
            }
        }

        public static List<LandMoveInfoCategory> ListLandMoveCategory(string pnu)
        {
            using (var connection = MariaDBConnection.connectDB())
            {
                string query =
                    $"""
                     SELECT distinct rsn
                                   , reg_dt      AS regDt                          
                       FROM landmove_info
                      WHERE g_seq = ( SELECT g_seq
                                        FROM landmove_info
                                       WHERE af_pnu = '{pnu}' OR af_pnu = '{pnu}'
                                      LIMIT 1
                                    )
                     GROUP BY rsn, regDt
                     ORDER BY idx 
                     """;
                return connection.Query<LandMoveInfoCategory>(query).ToList();
            }
        }

        // [테이블]landmove_info 의 MAX(g_seq) 가져오기
        public static int GetMaxGroupSeqno()
        {
            using (var connection = MariaDBConnection.connectDB())
            {
                string query = 
                    $"""
                    SELECT max(g_seq) 
                     FROM landmove_info;
                    """;                    
                connection.Execute(query);
                return connection.QuerySingle<int>(query);
            }
        }
        #endregion

        #region [토지이동] 테이블 생성 - 기존자료 백업, 테이블명 되돌리기
        public static int BackupLandMoveInfoOrg()
        {
            using (var connection = MariaDBConnection.connectDB())
            {
                string today = DateTime.Today.ToString("yyyyMMdd");
                string query = "";
                int retValue = 0;

                //--------------------------------
                // 1. 업로드 하기 전 원본 자료 백업 - 업로드 완료 후 원래 테이블명(landmove_info)로 옮길 것임 
                //--------------------------------
                query =
                    $"""
                    DROP TABLE IF EXISTS landmove_info_backup_{today};
                    """;
                try
                {
                    retValue = connection.Execute(query);
                    // ret == 0이면 정상 실행되었으나 복사된 데이터가 없음
                }
                catch (Exception ex)
                {
                    MessageBox.Show("[2]DB 데이터 백업 중 오류가 발생했습니다. " + ex.ToString());
                    // 쿼리 오류 등 비정상 상황
                    // ex.Message, ex.ToString() 등으로 오류 정보 확인
                }

                //--------------------------------
                // 2. 업로드 작업 테이블이 존재한다면 DROP
                // (동일사용자가 같은 날짜에 여러번 업로드 한다는 가정)
                //--------------------------------
                query = 
                    $"""
                    CREATE TABLE landmove_info_backup_{today}
                    ENGINE=MyISAM 
                    AS 
                    SELECT * FROM landmove_info;
                    """;
                try
                {
                    retValue = connection.Execute(query);
                    // ret == 0이면 정상 실행되었으나 복사된 데이터가 없음
                }
                catch (Exception ex)
                {
                    MessageBox.Show("[1]DB 데이터 백업 중 오류가 발생했습니다. " + ex.ToString());
                    // 쿼리 오류 등 비정상 상황
                    // ex.Message, ex.ToString() 등으로 오류 정보 확인
                }               

                return retValue;
            }
        }

        public static int CreateLandMoveInfoUser()
        {
            using (var connection = MariaDBConnection.connectDB())
            {
                string today = DateTime.Today.ToString("yyyyMMdd");
                string query = "";
                int retValue = 0;

                //--------------------------------
                // 1. 업로드 작업 테이블이 존재한다면 DROP
                // (동일사용자가 같은 날짜에 여러번 업로드 한다는 가정)
                //--------------------------------
                query =
                    $"""
                    DROP TABLE IF EXISTS landmove_info_{DbConInfo.id}_{today};
                    """;
                try
                {
                    retValue = connection.Execute(query);
                    // ret == 0이면 정상 실행되었으나 복사된 데이터가 없음
                }
                catch (Exception ex)
                {
                    MessageBox.Show("[2]DB 데이터 백업 중 오류가 발생했습니다. " + ex.ToString());
                    // 쿼리 오류 등 비정상 상황
                    // ex.Message, ex.ToString() 등으로 오류 정보 확인
                }

                //--------------------------------
                // 3. 업로드 작업 테이블 생성
                //--------------------------------
                query =
                    $"""
                    CREATE TABLE landmove_info_{DbConInfo.id}_{today}
                    ENGINE=MyISAM 
                    AS
                    SELECT * FROM landmove_info;
                    """;
                try
                {
                    retValue = connection.Execute(query);
                    // ret == 0이면 정상 실행되었으나 복사된 데이터가 없음
                }
                catch (Exception ex)
                {
                    MessageBox.Show("[3]DB 데이터 백업 중 오류가 발생했습니다. " + ex.ToString());
                    // 쿼리 오류 등 비정상 상황
                    // ex.Message, ex.ToString() 등으로 오류 정보 확인
                }

                return retValue;
            }
        }

        public static int CommitLandMoveInfoOrg()
        {
            using (var connection = MariaDBConnection.connectDB())
            {
                string today = DateTime.Today.ToString("yyyyMMdd");
                string query = "";
                int retValue = 0;

                //--------------------------------
                // 1. 업로드 완료(Done) => 원본 테이블 삭제
                //--------------------------------
                query = 
                    $"""
                    DROP TABLE landmove_info;
                    """;
                try
                {
                    retValue = connection.Execute(query);
                    // ret == 0이면 정상 실행되었으나 복사된 데이터가 없음
                }
                catch (Exception ex)
                {
                    MessageBox.Show("[1]DB 데이터 이관 중 오류가 발생했습니다. " + ex.ToString());
                    // 쿼리 오류 등 비정상 상황
                    // ex.Message, ex.ToString() 등으로 오류 정보 확인
                }

                //--------------------------------
                // 2. 업로드 완료(Done) => 원래 테이블 삭제 후 원래 테이블명(landmove_info)로 복사
                //--------------------------------
                query = 
                    $"""
                    CREATE TABLE landmove_info 
                    ENGINE=MyISAM 
                    AS 
                    SELECT * FROM landmove_info_{DbConInfo.id}_{today};
                    """;
                try
                {
                    retValue = connection.Execute(query);
                    // ret == 0이면 정상 실행되었으나 복사된 데이터가 없음
                }
                catch (Exception ex)
                {
                    MessageBox.Show("[2]DB 데이터 이관 중 오류가 발생했습니다. " + ex.ToString());
                    // 쿼리 오류 등 비정상 상황
                    // ex.Message, ex.ToString() 등으로 오류 정보 확인
                }

                return retValue;
            }
        }
        #endregion


        #region [토지이동] 기존 데이터 조회(그룹번호 별)

        //func_sql_query_flowlist : _pnulist에 존재하는 필지들이 포함된 g_seq 목록 조회
        public static List<int> QueryGroupListFromDB(List<LandMovePnuList> _pnulist)
        {
            using (var connection = MariaDBConnection.connectDB())
            {
                // PNU 리스트 추출
                List<string> pnuList = _pnulist.Select(p => p.pnu).ToList();
                string today = DateTime.Today.ToString("yyyyMMdd");

                // IN 절을 위한 파라미터 생성
                string qryColumn = "g_seq";
                string bfParams = string.Join(",", pnuList.Select((_, i) => $"@bf_pnu{i}"));
                string afParams = string.Join(",", pnuList.Select((_, i) => $"@af_pnu{i}"));

                // 첫 번째 쿼리: DISTINCT GROUP_SEQNO 조회
                string query =
                    $"""
                    SELECT DISTINCT {qryColumn} 
                    FROM landmove_info_{DbConInfo.id}_{today} 
                    WHERE bf_pnu IN ({bfParams}) 
                        OR af_pnu IN ({afParams}) 
                    GROUP BY {qryColumn}
                    """;

                // 디버그: 쿼리 출력
                //Debug.WriteLine("=== Generated Query ===");
                //Debug.WriteLine(query);


                List<int> listgrp = new List<int>();

                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    // BF_PNU와 AF_PNU 파라미터 추가
                    for (int i = 0; i < pnuList.Count; i++)
                    {
                        //파라미터 이름이 같으면 MySQLCommand에서 중복 등록이 불가능합니다. 그래서 같은 이름을 가진 파라미터를 2번 이상 추가하면 오류 발생.
                        //cmd.Parameters.AddWithValue($"@bf_pnu{i}", pnuList[i]);
                        //cmd.Parameters.AddWithValue($"@af_pnu{i}", pnuList[i]);
                        //Debug.WriteLine($"@bf_pnu{i} = '{pnuList[i]}'");
                        //Debug.WriteLine($"@af_pnu{i} = '{pnuList[i]}'");
                        cmd.Parameters.Add(new MySqlParameter($"@bf_pnu{i}", MySqlDbType.VarChar) { Value = pnuList[i] });
                        cmd.Parameters.Add(new MySqlParameter($"@af_pnu{i}", MySqlDbType.VarChar) { Value = pnuList[i] });

                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listgrp.Add(reader.GetInt32(0));//결과값이 정수일 때//
                        }
                    }

                    Console.WriteLine(query); // 또는 Debug.WriteLine(query);
                }

                return listgrp;
            }
        }

        //func_sql_query_flowlist : _pnulist에 존재하는 필지들이 포함된 g_seq 목록 기준 landmove_info 레코드 조회
        public static List<LandMoveInfo> QueryFlowListFromDB(List<int> _grplist)
        {
            using (var connection = MariaDBConnection.connectDB())
            {
                // PNU 리스트 추출
                List<int> grpList = _grplist.ToList();
                string today = DateTime.Today.ToString("yyyyMMdd");

                // IN 절을 위한 파라미터 생성
                string qryColumn = "g_seq";
                string paramPlaceholders = string.Join(",", grpList.Select((_, i) => $"@grp{i}"));

                // 첫 번째 쿼리: DISTINCT GROUP_SEQNO 조회
                string query =
                    $"""
                    SELECT *
                    FROM landmove_info_{DbConInfo.id}_{today}  
                    WHERE {qryColumn} IN ({paramPlaceholders})
                    ORDER BY {qryColumn}
                    """;

                List<int> listgrp = new List<int>();

                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    // BF_PNU와 AF_PNU 파라미터 추가
                    for (int i = 0; i < grpList.Count; i++)
                    {
                        //파라미터 이름이 같으면 MySQLCommand에서 중복 등록이 불가능합니다. 그래서 같은 이름을 가진 파라미터를 2번 이상 추가하면 오류 발생.
                        cmd.Parameters.AddWithValue($"@grp{i}", grpList[i]);
                    }
                }

                return connection.Query<LandMoveInfo>(query).ToList();
            }
        }


        //func_sql_delete_flowlist : _pnulist에 존재하는 필지들이 포함된 g_seq 목록에 해당하는 레코드 삭제
        public static void DeleteGroupListFromDB(List<int> _grplist)
        {
            using (var connection = MariaDBConnection.connectDB())
            {
                // PNU 리스트 추출
                List<int> grpList = _grplist.ToList();
                string today = DateTime.Today.ToString("yyyyMMdd");

                // IN 절을 위한 파라미터 생성
                string qryColumn = "g_seq";
                string paramPlaceholders = string.Join(",", grpList.Select((_, i) => $"@grp{i}"));

                // 첫 번째 쿼리: DISTINCT GROUP_SEQNO 조회
                string query =
                    $"""
                    DELETE 
                    FROM landmove_info_{DbConInfo.id}_{today} 
                    WHERE {qryColumn} IN ({paramPlaceholders})
                    """;

                List<int> listgrp = new List<int>();

                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    // 파라미터 추가
                    for (int i = 0; i < grpList.Count; i++)
                    {
                        cmd.Parameters.AddWithValue($"@grp{i}", grpList[i]);
                    }

                    cmd.ExecuteNonQuery();
                }
            }
        }
        #endregion

        #region 데이터 업로드 (기존+신규)
        //func_sql_write_by_group2
        //(Grouping 결과 DB Insert)
        public static void InsertLandMoveUpload(List<LandMoveInfo> groupRecords)
        {
            using (var connection = MariaDBConnection.connectDB())
            {
                string today = DateTime.Today.ToString("yyyyMMdd");

                string query =
                    $"""
                      INSERT INTO landmove_info_{DbConInfo.id}_{today}
                      VALUES (@groupseqno, @seq, @bfpnu, @afpnu, @rsn, @creymd, @bfjimok, @bfarea, @afjimok, @afarea, @ownname, @pnuseq, @areacd, @userid, @uploaddt)
                      """;

                int idx = 0;
                foreach (var rec in groupRecords)
                {
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@groupseqno", rec.gSeq);
                        cmd.Parameters.AddWithValue("@seq", idx);
                        cmd.Parameters.AddWithValue("@bfpnu", rec.bfPnu);
                        cmd.Parameters.AddWithValue("@afpnu", rec.afPnu);
                        cmd.Parameters.AddWithValue("@rsn", rec.rsn);
                        cmd.Parameters.AddWithValue("@creymd", rec.regDt);
                        cmd.Parameters.AddWithValue("@bfjimok", rec.bfJimok);
                        cmd.Parameters.AddWithValue("@bfarea", rec.bfArea);
                        cmd.Parameters.AddWithValue("@afjimok", rec.afJimok);
                        cmd.Parameters.AddWithValue("@afarea", rec.afArea);
                        cmd.Parameters.AddWithValue("@ownname", rec.ownName);
                        cmd.Parameters.AddWithValue("@pnuseq", rec.pSeq);
                        cmd.Parameters.AddWithValue("@areacd", GlobalDataManager.Instance.loginUser.areaCd);
                        cmd.Parameters.AddWithValue("@userid", DbConInfo.id);
                        cmd.Parameters.AddWithValue("@uploaddt", today);
                        //
                        cmd.ExecuteNonQuery();
                    }
                    idx++;
                }

            }
        }
        #endregion

    }
}
