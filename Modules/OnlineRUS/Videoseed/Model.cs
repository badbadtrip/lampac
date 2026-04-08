using System.Collections.Generic;

namespace Videoseed
{
    public class Root
    {
        public string status { get; set; }

        public List<Data> data { get; set; }
    }

    public class Data
    {
        public string iframe { get; set; }

        public Dictionary<string, Season> seasons { get; set; }
    }

    public class Season
    {
        public Dictionary<string, Episode> videos { get; set; }
    }

    public class Episode
    {
        public string iframe { get; set; }
    }
}
