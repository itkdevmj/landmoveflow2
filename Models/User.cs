namespace LMFS.Models
{
    public class User
    {
        public string name { get; set; }
        public string areaCd { get; set; }
        public string token { get; set; }
        public bool isAuthenticated { get; set; }
    }
}
