namespace Online.Models.VDBmovies
{
    public class EmbedModel
    {
        public Episode[] movies { get; set; }

        public CDNmovies.Voice[] serial { get; set; }

        public string quality { get; set; }
    }

    public class Episode
    {
        public string title { get; set; }

        public string file { get; set; }

        public string subtitle { get; set; }

        public string subtitles { get; set; }
    }

    public class Movie
    {
        public string iframe_src { get; set; }
    }

    public class MovieDB
    {
        public string id { get; set; }
        public string ru_title { get; set; }
        public string orig_title { get; set; }
        public string imdb_id { get; set; }
        public long? kinopoisk_id { get; set; }
        public int year { get; set; }
    }

    public class RootObject
    {
        public Dictionary<string, Movie> data { get; set; }
    }
}
