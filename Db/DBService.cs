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
                     ORDER BY reg_dt, rsn, bf_pnu, af_pnu
                     """;
                return connection.Query<LandMoveInfo>(query).ToList();
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
