namespace LMFS.Models
{
    public class SidoCode
    {
        public string sidoSggCd { get; set; }
        public string umdCd { get; set; }
        public string riCd { get; set; }
        public string sidoNm { get; set; }
        public string sggNm { get; set; }
        public string umdNm { get; set; }
        public string riNm { get; set; }

        public string umdCdNm => $"[{umdCd}] {umdNm}";
        public string riCdNm => $"[{riCd}] {riNm}";
    }
}
