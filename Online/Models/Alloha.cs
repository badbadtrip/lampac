namespace Online.Models.Alloha
{
    public class Episode
    {
        public int episode { get; set; }

        public Dictionary<string, Translation> translation { get; set; }
    }

    public class FileQ
    {
        public string h264 { get; set; }

        public string av1 { get; set; }
    }

    public class Translation
    {
        public string translation { get; set; }
    }
}
