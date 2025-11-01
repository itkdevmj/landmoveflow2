namespace LMFS.Models
{
    public class LandMoveInfo
    {
        public string areaCd { get; set; }
        public int gSeq { get; set; }
        public int idx { get; set; }
        public string bfPnu { get; set; }
        public string afPnu { get; set; }
        public string rsn { get; set; }
        public string regDt { get; set; }
        public string bfJimok { get; set; }
        public double bfArea { get; set; }
        public string afJimok { get; set; }
        public double afArea { get; set; }
        public string ownName { get; set; }
        public int pSeq { get; set; }
        public string userId { get; set; }
        public string uploadDt { get; set; }


        public string bfJibun { get; set; }
        public string afJibun { get; set; }

    }

    public class LandMoveInfoCategory
    {        
        public string rsn { get; set; }    
        public string regDt { get; set; }        
    }
}
