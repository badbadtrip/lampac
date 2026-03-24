namespace Online.Models
{
    public class ApiModel
    {
        public string title { get; set; }

        public string stream_url { get; set; }

        //public List<ApiModelStream> streams { get; set; } = new List<ApiModelStream>();

        //public List<ApiModel> submenu { get; set; }

        ///// <summary>
        ///// voice
        ///// season
        ///// episode
        ///// </summary>
        //public string type { get; set; }
    }

    public class ApiModelStream
    {
        public string link { get; set; }

        public string quality { get; set; }
    }
}
