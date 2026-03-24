namespace Online.Models.Lumex
{
    public class Datum
    {
        public long kp_id { get; set; }

        public string imdb_id { get; set; }

        public string title { get; set; }

        public string orig_title { get; set; }

        public string add { get; set; }

        public long id { get; set; }

        public string content_type { get; set; }
    }

    public class DatumDB
    {
        public long id { get; set; }

        public long kinopoisk_id { get; set; }

        public string imdb_id { get; set; }

        public string ru_title { get; set; }

        public string orig_title { get; set; }

        public string content_type { get; set; }

        public string year { get; set; }
    }

    public class EmbedModel
    {
        public string csrf { get; set; }

        public string tag_url { get; set; }

        public string content_type { get; set; }

        public Medium[] media { get; set; }
    }

    public class Episode
    {
        public int episode_id { get; set; }

        public Medium[] media { get; set; }
    }

    public class Medium
    {
        public int translation_id { get; set; }

        public string translation_name { get; set; }

        public int? max_quality { get; set; }

        public string playlist { get; set; }

        public string[] subtitles { get; set; }

        public Track[] tracks { get; set; }



        public int season_id { get; set; }

        public Episode[] episodes { get; set; }
    }

    public class SearchRoot
    {
        public Datum[] data { get; set; }
    }

    public class Track
    {
        public string src { get; set; }
        public string srlang { get; set; }
        public string label { get; set; }
    }
}
