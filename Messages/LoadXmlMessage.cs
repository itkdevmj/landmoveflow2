
using LMFS.Models;

namespace LMFS.Messages
{
    public class LoadXmlMessage(string path)
    {
        public string filePath { get; set; } = path;
    }
}
