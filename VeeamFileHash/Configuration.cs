using System.Configuration;

namespace VeeamFileHash
{
    public class Configuration
    {
        public int OptimalThreadCount => int.Parse(ConfigurationManager.AppSettings["OptimalThreadCount"]);
        public int OptimalReadBlockSize => int.Parse(ConfigurationManager.AppSettings["OptimalReadBlockSize"]);
    }
}