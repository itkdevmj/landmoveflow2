using System.Collections.Generic;
using LMFS.Models;

namespace LMFS.Services;

public sealed class GlobalDataManager
{
    private static readonly GlobalDataManager instance = new GlobalDataManager();
    
    public List<SidoCode> sidoCodeList { get; set; }
    public Dictionary<string, string> JimokCode { get; set; }
    public Dictionary<string, string> ReasonCode { get; set; }

    public User loginUser { get; set; }
    
    private GlobalDataManager() { }
    
    public static GlobalDataManager Instance
    {
        get { return instance; }
    }
}