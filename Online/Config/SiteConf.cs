using Shared.Models.Online.Settings;

namespace Online.Config
{
    public class SiteConf
    {
        /// <summary>
        /// https://veoveo.io
        /// </summary>
        public OnlinesSettings VeoVeo { get; set; } = new OnlinesSettings("VeoVeo", "https://api.rstprgapipt.com")
        {
            httpversion = 2,
            stream_access = "apk,cors,web"
        };


        public RezkaSettings Rezka { get; set; } = new RezkaSettings("Rezka", "https://hdrezka.me", true)
        {
            stream_access = "apk,cors,web",
            ajax = true,
            reserve = true,
            hls = true,
            scheme = "http",
            headers = Http.defaultUaHeaders
        };


        /// <summary>
        /// zona - https://api.kinogram.best/embed/movie/83072
        /// </summary>
        public CollapsSettings Collaps { get; set; } = new CollapsSettings("Collaps", "https://api.luxembd.ws", streamproxy: true, two: false)
        {
            rch_access = "apk",
            stream_access = "apk,cors,web",
            apihost = "https://api.bhcesh.me",
            token = "eedefb541aeba871dcfc756e6b31c02e",
            headers = HeadersModel.Init(Http.defaultFullHeaders,
                ("Origin", "https://kinokrad.my")
            ).ToDictionary(),
            headers_stream = HeadersModel.Init(Http.defaultFullHeaders,
                ("Origin", "https://kinokrad.my"),
                ("sec-fetch-dest", "empty"),
                ("sec-fetch-mode", "cors"),
                ("sec-fetch-site", "cross-site"),
                ("accept", "*/*")
            ).ToDictionary()
        };


        public OnlinesSettings Kinotochka { get; set; } = new OnlinesSettings("Kinotochka", "https://kinovibe.vip")
        {
            httpversion = 2,
            rch_access = "apk,cors",
            stream_access = "apk,cors",
            rchstreamproxy = "web"
        };


        public OnlinesSettings Geosaitebi { get; set; } = new OnlinesSettings("Geosaitebi", "https://geosaitebi.tv", streamproxy: true)
        {
            rch_access = "apk,cors",
            stream_access = "apk",
            rchstreamproxy = "web,cors",
            headers_stream = HeadersModel.Init(Http.defaultFullHeaders,
                ("Origin", "https://geosaitebi.tv"),
                ("referer", "https://geosaitebi.tv/"),
                ("sec-fetch-dest", "empty"),
                ("sec-fetch-mode", "cors"),
                ("sec-fetch-site", "cross-site"),
                ("accept", "*/*")
            ).ToDictionary()
        };

        public OnlinesSettings AsiaGe { get; set; } = new OnlinesSettings("AsiaGe", "https://asia.com.ge")
        {
            rch_access = "apk,cors",
            stream_access = "apk,web,cors"
        };


        public OnlinesSettings LeProduction { get; set; } = new OnlinesSettings("LeProduction", "https://www.le-production.tv")
        {
            rch_access = "apk,cors",
            stream_access = "apk,cors",
            rchstreamproxy = "web"
        };

        /// <summary>
        /// https://ge.movie - запасной вариант
        /// https://1000movie.net/movies/shelter - еще один videodb.cloud
        /// </summary>
        public OnlinesSettings Kinoflix { get; set; } = new OnlinesSettings("Kinoflix", "https://kinoflix.tv", streamproxy: true)
        {
            rch_access = "apk",
            stream_access = "apk",
            rchstreamproxy = "web,cors",
            headers_stream = HeadersModel.Init(Http.defaultFullHeaders,
                ("referer", "https://kinoflix.tv"),
                ("sec-fetch-dest", "empty"),
                ("sec-fetch-mode", "cors"),
                ("sec-fetch-site", "cross-site")
            ).ToDictionary()
        };


        public OnlinesSettings RutubeMovie { get; set; } = new OnlinesSettings("RutubeMovie", "https://rutube.ru", streamproxy: true, rch_access: "apk,cors");


        public OnlinesSettings VkMovie { get; set; } = new OnlinesSettings("VkMovie", "https://api.vkvideo.ru", streamproxy: true)
        {
            rch_access = "apk,cors",
            stream_access = "apk,cors",
            headers = HeadersModel.Init(Http.defaultFullHeaders,
                ("origin", "https://vkvideo.ru"),
                ("referer", "https://vkvideo.ru/")
            ).ToDictionary()
        };


        public OnlinesSettings Plvideo { get; set; } = new OnlinesSettings("Plvideo", "https://api.g1.plvideo.ru", streamproxy: true, enable: false, rch_access: "apk,cors");


        /// <summary>
        /// zona - https://plapi.cdnvideohub.com/api/v1/player/sv/playlist?pub=22&id=5265603&aggr=kp
        /// Плеер CVH - https://site.yummyani.me/catalog/item/monolog-farmatsevta-2
        /// http://lostfilm5.org
        /// </summary>
        public OnlinesSettings CDNvideohub { get; set; } = new OnlinesSettings("CDNvideohub", "https://plapi.cdnvideohub.com", streamproxy: true)
        {
            rch_access = "apk,cors",
            stream_access = "apk,cors",
            httpversion = 2,
            headers = HeadersModel.Init(Http.defaultFullHeaders,
                ("referer", "https://hdkino.pub/"),
                ("sec-fetch-dest", "empty"),
                ("sec-fetch-mode", "cors"),
                ("sec-fetch-site", "cross-site")
            ).ToDictionary()
        };


        public OnlinesSettings Redheadsound { get; set; } = new OnlinesSettings("Redheadsound", "https://redheadsound.studio", enable: false)
        {
            rch_access = "apk",
            headers = HeadersModel.Init("referer", "https://redheadsound.studio/").ToDictionary()
        };


        public OnlinesSettings FlixCDN { get; set; } = new OnlinesSettings("FlixCDN", "https://player0.flixcdn.space", "https://api0.flixcdn.biz/api", streamproxy: true)
        {
            rch_access = "apk",
            stream_access = "apk,cors,web",
            httpversion = 1,
            headers_stream = HeadersModel.Init(
                ("origin", "https://player0.flixcdn.space"),
                ("referer", "https://player0.flixcdn.space/"),
                ("sec-fetch-dest", "video"),
                ("sec-fetch-mode", "cors"),
                ("sec-fetch-site", "cross-site")
            ).ToDictionary()
        };


        /// <summary>
        /// https://kinoplay2.site
        /// kinogo.media
        /// https://film-2024.org
        /// </summary>
        public OnlinesSettings VideoDB { get; set; } = new OnlinesSettings("VideoDB", "https://kinogo.media", "https://30bf3790.obrut.show", streamproxy: true)
        {
            httpversion = 2,
            rch_access = "apk",
            stream_access = "apk,cors,web",
            priorityBrowser = "http",
            imitationHuman = true,
            headers = HeadersModel.Init(Http.defaultFullHeaders,
                ("sec-fetch-storage-access", "active"),
                ("upgrade-insecure-requests", "1")
            ).ToDictionary(),
            headers_stream = HeadersModel.Init(Http.defaultFullHeaders,
                ("accept", "*/*"),
                ("origin", "https://kinogo.media"),
                ("referer", "https://kinogo.media/"),
                ("sec-fetch-dest", "empty"),
                ("sec-fetch-mode", "cors"),
                ("sec-fetch-site", "same-site")
            ).ToDictionary()
        };


        /// <summary>
        /// https://coldfilm.ink
        /// </summary>
        public OnlinesSettings CDNmovies { get; set; } = new OnlinesSettings("CDNmovies", "https://coldcdn.xyz", enable: false)
        {
            rch_access = "apk,cors",
            rchstreamproxy = "web",
            headers = HeadersModel.Init(
                ("DNT", "1"),
                ("Upgrade-Insecure-Requests", "1")
            ).ToDictionary()
        };


        public OnlinesSettings VDBmovies { get; set; } = new OnlinesSettings("VDBmovies", "https://cdnmovies-stream.online", streamproxy: true)
        {
            enable = false,
            rch_access = "apk",
            stream_access = "apk",
            httpversion = 2,
            headers = HeadersModel.Init(Http.defaultFullHeaders,
                ("sec-fetch-storage-access", "active"),
                ("upgrade-insecure-requests", "1")
            ).ToDictionary(),
            headers_stream = HeadersModel.Init(Http.defaultFullHeaders,
                ("accept", "*/*"),
                ("referer", "https://cdnmovies-stream.online"),
                ("origin", "https://cdnmovies-stream.online/"),
                ("sec-fetch-dest", "empty"),
                ("sec-fetch-mode", "cors"),
                ("sec-fetch-site", "cross-site")
            ).ToDictionary()
        };


        public OnlinesSettings FanCDN { get; set; } = new OnlinesSettings("FanCDN", "https://fanserial.me", streamproxy: true)
        {
            enable = true,
            rch_access = "apk",
            rhub_safety = false,
            httpversion = 2,
            imitationHuman = true,
            headers = HeadersModel.Init(Http.defaultFullHeaders,
                ("sec-fetch-storage-access", "active"),
                ("upgrade-insecure-requests", "1")
            ).ToDictionary(),
            headers_stream = HeadersModel.Init(Http.defaultFullHeaders,
                ("origin", "https://fanserial.me"),
                ("referer", "https://fanserial.me/"),
                ("sec-fetch-dest", "empty"),
                ("sec-fetch-mode", "cors"),
                ("sec-fetch-site", "same-site")
            ).ToDictionary()
        };


        public KinobaseSettings Kinobase { get; set; } = new KinobaseSettings("Kinobase", "https://kinobase.org", true, hdr: true)
        {
            httpversion = 2,
            stream_access = "apk,cors,web",
            geostreamproxy = ["ALL"]
        };


        /// <summary>
        /// https://uakinogo.ec
        /// https://uakinogo.online
        /// </summary>
        public OnlinesSettings Kinogo { get; set; } = new OnlinesSettings("Kinogo", "https://kinogo.luxury")
        {
            rch_access = "apk",
            stream_access = "apk,cors",
            rchstreamproxy = "web"
        };


        /// <summary>
        /// Получение учетной записи
        /// 
        /// tg: @monk_in_a_hat
        /// email: helpdesk@lumex.ink
        /// </summary>
        public LumexSettings VideoCDN { get; set; } = new LumexSettings("VideoCDN", "https://api.lumex.space", "API-токен", "https://portal.lumex.host", "ID клиент")
        {
            enable = false,
            stream_access = "apk,cors,web",
            log = false,
            verifyip = true, // ссылки привязаны к ip пользователя
            scheme = "http",
            //geostreamproxy = ["UA"],
            hls = false, // false - mp4 / true - m3u8
            disable_protection = false, // true - отключить проверку на парсер
            disable_ads = false, // отключить рекламу
            vast = new VastConf() { msg = "Реклама от VideoCDN" }
        };


        public LumexSettings Lumex { get; set; } = new LumexSettings("Lumex", "https://portal.lumex.host", null, "lumex.space", "tl6h28Hn1rL5")
        {
            enable = false,
            stream_access = "apk,cors,web",
            hls = true,
            scheme = "http",
            priorityBrowser = "http",
            geostreamproxy = ["ALL"]
        };


        /// <summary>
        /// https://movielab.one
        /// https://vid1730801370.fotpro135alto.com/api/idkp?kp_id=1392550&d=kinogo.inc
        /// </summary>
        public OnlinesSettings HDVB { get; set; } = new OnlinesSettings("HDVB", "https://vid1733431681.entouaedon.com", "https://apivb.com", token: "5e2fe4c70bafd9a7414c4f170ee1b192")
        {
            streamproxy = true,
            rch_access = "apk",
            stream_access = "apk,cors,web",
            headers = HeadersModel.Init(Http.defaultFullHeaders,
                ("referer", "encrypt:kwwsv=22prylhode1rqh2")
            ).ToDictionary(),
            headers_stream = HeadersModel.Init(Http.defaultFullHeaders,
                ("origin", "https://vid1733431681.entouaedon.com"),
                ("referer", "https://vid1733431681.entouaedon.com/"),
                ("sec-fetch-dest", "empty"),
                ("sec-fetch-mode", "cors"),
                ("sec-fetch-site", "same-site")
            ).ToDictionary()
        };


        /// <summary>
        /// api: https://vibix.org/api/external/documentation
        /// iframe: https://coldfilm.ink
        /// </summary>
        public OnlinesSettings Vibix { get; set; } = new OnlinesSettings("Vibix", "https://vibix.org", enable: false)
        {
            rch_access = "apk",
            stream_access = "apk,cors,web",
            httpversion = 2,
            headers = Http.defaultFullHeaders
        };


        /// <summary>
        /// https://videoseed.tv/faq.php
        /// zona - https://tv-2-kinoserial.net/embed/1043037/?token=ccf1c53aef4707461c7c9133a2e254ed
        /// </summary>
        public OnlinesSettings Videoseed { get; set; } = new OnlinesSettings("Videoseed", "https://videoseed.tv", streamproxy: true, enable: false)
        {
            stream_access = "apk,cors,web",
            headers = Http.defaultFullHeaders,
            headers_stream = HeadersModel.Init(Http.defaultFullHeaders,
                ("sec-fetch-dest", "empty"),
                ("sec-fetch-mode", "cors"),
                ("sec-fetch-site", "cross-site")
            ).ToDictionary()
        };


        public AllohaSettings Mirage { get; set; } = new AllohaSettings("Mirage", "https://api.apbugall.org", "https://quadrillion-as.allarknow.online", "6892d506bbdd5790e0ca047ff39462", "", true, true)
        {
            enable = false,
            streamproxy = true,
            httpversion = 2,
            headers = Http.defaultFullHeaders
        };
    }
}
