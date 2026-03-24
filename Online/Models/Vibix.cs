namespace Online.Models.Vibix
{
    public class EmbedModel
    {
        public Seasons[] playlist { get; set; }
    }

    public class Seasons
    {
        public string title { get; set; }
        public Seasons[] folder { get; set; }
        public string file { get; set; }
    }

    public class Video
    {
        public string iframe_url { get; set; }

        public string type { get; set; }
    }
}
