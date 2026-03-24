namespace Online.Models.Ashdi
{
    public class EmbedModel
    {
        public bool IsEmpty { get; set; }

        public string content { get; set; }

        public Voice[] serial { get; set; }

        public List<Similar> similars { get; set; }
    }

    public class Similar
    {
        public string title { get; set; }

        public string year { get; set; }

        public string href { get; set; }

        public string img { get; set; }
    }

    public class Season
    {
        public string title { get; set; }

        public Series[] folder { get; set; }
    }

    public class Series
    {
        public string title { get; set; }

        public string file { get; set; }

        public string subtitle { get; set; }
    }

    public class Voice
    {
        public string title { get; set; }

        public Season[] folder { get; set; }
    }
}
