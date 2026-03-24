using Shared.Models.Online.Settings;

namespace Online.Config
{
    public class AnimeConf
    {
        public KodikSettings Kodik { get; set; } = new KodikSettings("Kodik", "41dd95f84c21719b09d6c71182237a25", true)
        {
            rch_access = "apk",
            stream_access = "apk,cors,web",
            apihost = "https://kodik-api.com",
            playerhost = "https://kodikplayer.com",
            linkhost = "https://kodikres.com",
            auto_proxy = true,      // прокси UA в api 
            cdn_is_working = true,  // прокси UA в обычном 
            //geostreamproxy = ["UA"],
            headers = HeadersModel.Init(
                ("referer", "https://anilib.me/")
            ).ToDictionary()
        };


        /// <summary>
        /// move to AniLiberty
        /// </summary>
        public OnlinesSettings AnilibriaOnline { get; set; } = new OnlinesSettings("AnilibriaOnline", "https://api.anilibria.tv", enable: false);

        public OnlinesSettings AniLiberty { get; set; } = new OnlinesSettings("AniLiberty", "https://api.anilibria.app")
        {
            stream_access = "apk,cors,web",
            httpversion = 2
        };


        /// <summary>
        /// https://anilib.me
        /// </summary>
        public OnlinesSettings AnimeLib { get; set; } = new OnlinesSettings("AnimeLib", "https://api.cdnlibs.org", streamproxy: true, stream_access: "apk")
        {
            enable = false,
            rhub_safety = false,
            httpversion = 2,
            headers = HeadersModel.Init(Http.defaultFullHeaders,
                ("origin", "https://animelib.org"),
                ("referer", "https://animelib.org/"),
                ("sec-fetch-dest", "empty"),
                ("sec-fetch-mode", "cors"),
                ("sec-fetch-site", "cross-site")
            ).ToDictionary(),
            headers_stream = HeadersModel.Init(Http.defaultFullHeaders,
                ("accept-encoding", "identity;q=1, *;q=0"),
                ("origin", "https://animelib.org"),
                ("referer", "https://animelib.org/"),
                ("sec-fetch-dest", "video"),
                ("sec-fetch-mode", "cors"),
                ("sec-fetch-site", "same-site")
            ).ToDictionary()
        };


        public OnlinesSettings AniMedia { get; set; } = new OnlinesSettings("AniMedia", "https://amd.online");


        public OnlinesSettings Animevost { get; set; } = new OnlinesSettings("Animevost", "https://animevost.org")
        {
            rch_access = "apk,cors",
            stream_access = "apk,cors",
            rchstreamproxy = "web",
            httptimeout = 10
        };


        public OnlinesSettings MoonAnime { get; set; } = new OnlinesSettings("MoonAnime", "https://api.moonanime.art", enable: true, token: "865fEF-E2e1Bc-2ca431-e6A150-780DFD-737C6B")
        {
            stream_access = "apk",
            rchstreamproxy = "web,cors",
            geo_hide = ["RU", "BY"]
        };


        public OnlinesSettings Dreamerscast { get; set; } = new OnlinesSettings("Dreamerscast", "https://dreamerscast.com")
        {
            rch_access = "apk",
            stream_access = "apk,cors",
            rchstreamproxy = "web"
        };


        public OnlinesSettings Animebesst { get; set; } = new OnlinesSettings("Animebesst", "https://anime1.best")
        {
            rch_access = "apk",
            stream_access = "apk,cors,web",
            httpversion = 2
        };


        public OnlinesSettings AnimeGo { get; set; } = new OnlinesSettings("AnimeGo", "https://animego.me", streamproxy: true, enable: false)
        {
            httpversion = 2,
            headers_stream = HeadersModel.Init(
                ("origin", "https://aniboom.one"),
                ("referer", "https://aniboom.one/")
            ).ToDictionary()
        };
    }
}
