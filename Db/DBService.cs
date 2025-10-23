using System.Collections.Generic;
using System.Linq;
using Dapper;
using LMFS.Models;

namespace LMFS.Db
{
    public static class DBService
    {
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



    }
}
