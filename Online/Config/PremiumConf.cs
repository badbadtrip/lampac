using Shared.Models.Online.Settings;

namespace Online.Config
{
    public class PremiumConf
    {
        /// <summary>
        /// https://iptv.online/ru/dealers/api
        /// </summary>
        public OnlinesSettings IptvOnline { get; set; } = new OnlinesSettings("IptvOnline", "https://iptv.online", enable: false)
        {
            rhub_safety = false
        };


        public AllohaSettings Alloha { get; set; } = new AllohaSettings("Alloha", "https://api.apbugall.org", "https://torso-as.stloadi.live", "", "", true, true)
        {
            rch_access = "apk,cors,web",
            stream_access = "apk,cors,web",
            reserve = true
        };


        public FilmixSettings FilmixPartner { get; set; } = new FilmixSettings("FilmixPartner", "http://5.61.56.18/partner_api", enable: false)
        {
            stream_access = "apk,cors,web"
        };

        /// <summary>
        /// http://filmixapp.cyou
        /// http://filmixapp.vip
        /// http://fxapp.biz
        /// </summary>
        public FilmixSettings Filmix { get; set; } = new FilmixSettings("Filmix", "http://filmixapp.cyou")
        {
            rhub_safety = false,
            rch_access = "apk",
            stream_access = "apk,cors,web",
            reserve = false,
            headers = HeadersModel.Init(
                ("Accept-Encoding", "gzip")
            ).ToDictionary()
        };

        public FilmixSettings FilmixTV { get; set; } = new FilmixSettings("FilmixTV", "https://api.filmix.tv", enable: false)
        {
            httpversion = 2,
            rhub_safety = false,
            pro = true,
            stream_access = "apk,cors,web",
            headers = HeadersModel.Init(
                ("user-agent", "Mozilla/5.0 (SMART-TV; LINUX; Tizen 6.0) AppleWebKit/537.36 (KHTML, like Gecko) 76.0.3809.146/6.0 TV Safari/537.36")
            ).ToDictionary()
        };


        /// <summary>
        /// https://api.srvkp.com - стандартный 
        /// https://cdn32.lol/api- apk
        /// https://cdn4t.store/api - apk
        /// https://kpapp.link/api - smart tv
        /// https://api.service-kp.com - старый
        /// </summary>
        public KinoPubSettings KinoPub { get; set; } = new KinoPubSettings("KinoPub", "https://api.srvkp.com")
        {
            httpversion = 2,
            rhub_safety = false,
            filetype = "hls", // hls | hls4 | mp4
            stream_access = "apk,cors,web",
            headers = HeadersModel.Init(Http.defaultFullHeaders,
                ("sec-fetch-dest", "document"),
                ("sec-fetch-mode", "navigate"),
                ("sec-fetch-site", "none"),
                ("sec-fetch-user", "?1"),
                ("upgrade-insecure-requests", "1")
            ).ToDictionary()
        };


        public RezkaSettings RezkaPrem { get; set; } = new RezkaSettings("RezkaPrem", null)
        {
            enable = false,
            rhub_safety = false,
            stream_access = "apk,cors,web",
            reserve = true,
            hls = true,
            scheme = "http"
        };


        /// <summary>
        /// api.vokino.org
        /// api.vokino.pro
        /// </summary>
        public VokinoSettings VoKino { get; set; } = new VokinoSettings("VoKino", "http://api.vokino.org", streamproxy: false)
        {
            rchstreamproxy = "web",
            rhub_safety = false
        };


        public OnlinesSettings iRemux { get; set; } = new OnlinesSettings("iRemux", "https://megaoblako.com")
        {
            rchstreamproxy = "web",
            geostreamproxy = ["UA"]
        };

        public OnlinesSettings GetsTV { get; set; } = new OnlinesSettings("GetsTV", "https://getstv.com")
        {
            enable = false,
            stream_access = "apk,cors,web",
            rhub_safety = false,
            headers = HeadersModel.Init(
                ("user-agent", "Mozilla/5.0 (Web0S; Linux/SmartTV) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.34 Safari/537.36 WebAppManager")
            ).ToDictionary()
        };
    }
}
