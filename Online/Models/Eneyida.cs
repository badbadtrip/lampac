namespace Online.Models.Eneyida
{
    public class EmbedModel
    {
        public bool IsEmpty { get; set; }

        public string source_type { get; set; }

        public string content { get; set; }

        public string quel { get; set; }

        public Tortuga.Voice[] serial { get; set; }

        public Ashdi.Voice[] serial_ashdi { get; set; }

        public List<Similar> similars { get; set; }
    }

    public class Similar
    {
        public string title { get; set; }

        public string year { get; set; }

        public string href { get; set; }

        public string img { get; set; }
    }
}
