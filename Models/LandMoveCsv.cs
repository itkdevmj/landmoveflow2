using CsvHelper.Configuration.Attributes;

namespace LMFS.Models
{
    public class LandMoveCsv
    {
        public int  gSeq { get; set; }
        public int idx { get; set; }

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

        [Name("이동전_지번")]
        public string ownName { get; set; }

        [Name("이동전_지목")]
        public string ownAddr { get; set; }

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
