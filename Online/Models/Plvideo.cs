namespace Online.Models.Plvideo
{
    public class Item
    {
        public string id { get; set; }

        public string title { get; set; }

        public ItemuploadFile uploadFile { get; set; }

        public string visible { get; set; }
    }

    public class ItemuploadFile
    {
        public long videoDuration { get; set; }
    }

    public class Profile
    {
        public string hls { get; set; }
    }
}
