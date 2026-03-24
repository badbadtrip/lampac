namespace Online.Models.Kinoflix
{
    public class SearchModel
    {
        public string link { get; set; }

        public SimilarTpl similar { get; set; }
    }

    public class CardModel
    {
        public string file { get; set; }

        public List<Season> seasons { get; set; }
    }

    public class Season
    {
        public string title { get; set; }

        public List<Folder> folder { get; set; }
    }

    public class Folder
    {
        public string title { get; set; }

        public string file { get; set; }

        public string id { get; set; }

        public string subtitle { get; set; }
    }
}
