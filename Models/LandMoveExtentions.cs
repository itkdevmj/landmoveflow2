using LMFS.Models;
using System;

namespace LMFS.Models
{
    public static class LandMoveExtensions
    {
        public static LandMoveInfo ToLandMoveInfo(this LandMoveCsv csv)
        {
            return new LandMoveInfo
            {
                areaCd = csv.lawd,
                gSeq = csv.gSeq ?? 0,
                idx = csv.idx ?? 0,
                bfPnu = csv.bfPnu,
                afPnu = csv.afPnu,
                rsn = csv.rsn,
                bfJimok = csv.bfJimok,
                bfArea = double.TryParse(csv.bfArea, out var bfAreaValue) ? bfAreaValue : 0.0,
                afJimok = csv.afJimok,
                afArea = double.TryParse(csv.afArea, out var afAreaValue) ? afAreaValue : 0.0,
                ownName = csv.ownName,
                regDt = csv.regDt,
                pSeq = csv.pnuSeq,
                bfJibun = csv.bfJibun,
                afJibun = csv.afJibun
            };
        }
    }
}