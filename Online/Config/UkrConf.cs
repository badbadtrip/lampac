using Shared.Models.Online.Settings;

namespace Online.Config
{
    public class UkrConf
    {
        public OnlinesSettings Ashdi { get; set; } = new OnlinesSettings("Ashdi", "https://base.ashdi.vip")
        {
            rch_access = "apk,cors",
            stream_access = "apk,cors",
            rchstreamproxy = "web",
            geo_hide = ["RU", "BY"]
        };


        public OnlinesSettings Kinoukr { get; set; } = new OnlinesSettings("Kinoukr", "https://kinoukr.com")
        {
            rch_access = "apk,cors",
            stream_access = "apk,cors",
            rchstreamproxy = "web",
            geo_hide = ["RU", "BY"],
            headers = HeadersModel.Init(Http.defaultFullHeaders,
                ("cookie", "legit_user=1;"),
                ("origin", "https://kinoukr.com"),
                ("referer", "https://kinoukr.com/"),
                ("sec-fetch-dest", "document"),
                ("sec-fetch-mode", "navigate"),
                ("sec-fetch-site", "same-origin"),
                ("sec-fetch-user", "?1"),
                ("upgrade-insecure-requests", "1")
            ).ToDictionary()
        };


        public OnlinesSettings Eneyida { get; set; } = new OnlinesSettings("Eneyida", "https://eneyida.tv")
        {
            rch_access = "apk,cors",
            stream_access = "apk,cors,web"
        };
    }
}
