namespace Online.Models.VideoCDN
{
    public class Datum
    {
        public int kp_id { get; set; }

        public string imdb_id { get; set; }

        public string title { get; set; }

        public string orig_title { get; set; }

        public string add { get; set; }

        public string content_type { get; set; }
    }

    public class EmbedModel
    {
        public string type { get; set; } = null!;

        public Dictionary<string, string> voices { get; set; }

        public Dictionary<string, HashSet<int>> voiceSeasons { get; set; }

        public Dictionary<string, List<Season>> serial { get; set; }

        public Dictionary<string, string> movie { get; set; }

        public string quality { get; set; }
    }

    public class Folder
    {
        public string id { get; set; }

        public string file { get; set; }
    }

    public class SearchRoot
    {
        public Datum[] data { get; set; }
    }

    public class Season
    {
        public int id { get; set; }

        public Folder[] folder { get; set; }
    }

    public class VCDNSettings
    {
        public VCDNSettings(string apihost, string token, string cdnhost, bool useproxy)
        {
            this.apihost = apihost;
            this.token = token;
            this.cdnhost = cdnhost;
            this.useproxy = useproxy;
        }


        public string apihost { get; set; }

        public string token { get; set; }

        public string cdnhost { get; set; }

        public bool useproxy { get; set; }

        public bool streamproxy { get; set; }
    }
}
