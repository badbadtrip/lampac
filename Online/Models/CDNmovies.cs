namespace Online.Models.CDNmovies
{
    public class Voice
    {
        public string title { get; set; }

        public Season[] folder { get; set; }
    }

    public class Season
    {
        public string title { get; set; }

        public Episode[] folder { get; set; }
    }

    public class Episode
    {
        public string title { get; set; }

        public string file { get; set; }
    }

}
