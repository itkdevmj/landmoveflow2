using CsvHelper.Configuration.Attributes;

namespace LMFS.Models
{
    public class LandMoveCsv
    {
        //-------------------------------------------------------------------------------
        // CSV에 해당 값이 아예 없거나, 빈 값이 들어올 때
        //int 타입은 기본적으로 null이나 빈 값을 받을 수 없습니다.
        //만약 데이터가 없으면 CsvHelper가 int.Parse(null)을 하다가 예외를 발생시킵니다.
        //-------------------------------------------------------------------------------

        public int? gSeq { get; set; }
        public int? idx { get; set; }
        [Name("토지이동종목")]
        public string rsn { get; set; }
        [Name("일련번호")]
        public string seq { get; set; }
        [Name("정리일자")]
        public string regDt { get; set; }
        [Name("지역코드")]
        public string lawd { get; set; }
        [Name("대장구분")]
        public string lndGbn { get; set; }
        [Name("이동전_지번")]
        public string bfJibun { get; set; }
        [Name("이동전_지목")]
        public string bfJimok { get; set; }
        [Name("이동전_면적")]
        public string bfArea { get; set; }
        [Name("이동후_지번")]
        public string afJibun { get; set; }
        [Name("이동후_지목")]
        public string afJimok { get; set; }
        [Name("이동후_면적")]
        public string afArea { get; set; }
        [Name("현재_소유자명")]
        public string ownName { get; set; }
        [Name("현재_소유자주소")]
        public string ownAddr { get; set; }
        public int pnuSeq { get; set; }
        public string bfPnu { get; set; }
        public string afPnu { get; set; }
    }

    public class LandMovePnuList
    {
        public string pnu { get; set; }
        public bool bChecked { get; set; }        
    }
    public class LandMovePnuPair
    {
        public string bfPnu { get; set; }
        public string afPnu { get; set; }
    }

    public class LandMoveFileList
    {
        public string fileName { get; set; }
        public string startDt { get; set; }
        public string lastDt { get; set; }
        public int recordCnt { get; set; }
        public int uploadCnt { get; set; }
    }

}
