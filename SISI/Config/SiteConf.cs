namespace SISI.Config
{
    public class SiteConf
    {
        public SisiSettings BongaCams { get; set; } = new SisiSettings("BongaCams", "https://ee.bongacams.com", rch_access: "apk", stream_access: "apk,cors,web")
        {
            spider = false,
            httpversion = 2,
            headers = HeadersModel.Init(
                ("referer", "https://ee.bongacams.com/"),
                ("x-requested-with", "XMLHttpRequest")
            ).ToDictionary()
        };

        public SisiSettings Runetki { get; set; } = new SisiSettings("Runetki", "https://rus.runetki5.com", rch_access: "apk", stream_access: "apk,cors,web")
        {
            spider = false,
            httpversion = 2,
            headers = HeadersModel.Init(
                ("referer", "https://rus.runetki5.com/"),
                ("x-requested-with", "XMLHttpRequest")
            ).ToDictionary()
        };

        public SisiSettings Chaturbate { get; set; } = new SisiSettings("Chaturbate", "https://chaturbate.com")
        {
            spider = false,
            httpversion = 2,
            rch_access = "apk,cors",
            stream_access = "apk,cors,web"
        };

        public SisiSettings Ebalovo { get; set; } = new SisiSettings("Ebalovo", "https://www.ebalovo.pro", rch_access: "apk", stream_access: "apk,cors")
        {
            rchstreamproxy = "web",
            headers = Http.defaultFullHeaders,
            headers_stream = HeadersModel.Init(Http.defaultFullHeaders,
                ("sec-fetch-dest", "video"),
                ("sec-fetch-mode", "no-cors"),
                ("sec-fetch-site", "same-origin")
            ).ToDictionary()
        };

        public SisiSettings Eporner { get; set; } = new SisiSettings("Eporner", "https://www.eporner.com")
        {
            httpversion = 2,
            rch_access = "apk,cors",
            stream_access = "apk,cors",
            rchstreamproxy = "web",
            headers_image = HeadersModel.Init(
                ("Accept", "image/jpeg,image/png,image/*;q=0.8,*/*;q=0.5"),
                ("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/534.57.2 (KHTML, like Gecko) Version/5.1.7 Safari/534.57.2"),
                ("Cache-Control", "max-age=0")
            ).ToDictionary()
        };

        public SisiSettings HQporner { get; set; } = new SisiSettings("HQporner", "https://m.hqporner.com", rch_access: "apk,cors", stream_access: "apk,cors,web")
        {
            geostreamproxy = ["ALL"],
            headers = HeadersModel.Init("referer", "https://m.hqporner.com").ToDictionary(),
            headers_image = HeadersModel.Init("referer", "https://m.hqporner.com").ToDictionary()
        };

        public SisiSettings Porntrex { get; set; } = new SisiSettings("Porntrex", "https://www.porntrex.com", streamproxy: true, rch_access: "apk", stream_access: "apk")
        {
            headers_stream = HeadersModel.Init(
                ("referer", "https://www.porntrex.com/")
            ).ToDictionary(),
            headers_image = HeadersModel.Init(
                ("referer", "https://www.porntrex.com/")
            ).ToDictionary()
        };

        public SisiSettings Spankbang { get; set; } = new SisiSettings("Spankbang", "https://ru.spankbang.com")
        {
            httpversion = 2,
            rch_access = "apk,cors,web",
            stream_access = "apk,cors,web"
        };

        public SisiSettings Xhamster { get; set; } = new SisiSettings("Xhamster", "https://ru.xhamster.com")
        {
            httpversion = 2,
            rch_access = "apk,cors",
            stream_access = "apk,cors,web",
            headers_image = HeadersModel.Init(
                ("Accept", "image/jpeg,image/png,image/*;q=0.8,*/*;q=0.5"),
                ("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/534.57.2 (KHTML, like Gecko) Version/5.1.7 Safari/534.57.2"),
                ("Cache-Control", "max-age=0")
            ).ToDictionary()
        };

        /// <summary>
        /// https://in.tizam.info
        /// </summary>
        public SisiSettings Tizam { get; set; } = new SisiSettings("Tizam", "https://tv4.tizam.org")
        {
            rch_access = "apk,cors",
            stream_access = "apk,cors",
            rchstreamproxy = "web"
        };

        /// <summary>
        /// https://www.xvideos.com
        /// </summary>
        public SisiSettings Xvideos { get; set; } = new SisiSettings("Xvideos", "https://www.xv-ru.com")
        {
            httpversion = 2,
            rch_access = "apk,cors",
            stream_access = "apk,cors,web"
        };

        public SisiSettings Xnxx { get; set; } = new SisiSettings("Xnxx", "https://www.xnxx-ru.com")
        {
            httpversion = 2,
            rch_access = "apk,cors",
            stream_access = "apk,cors,web"
        };

        public SisiSettings XvideosRED { get; set; } = new SisiSettings("XvideosRED", "https://www.xvideos.red", enable: false);

        public SisiSettings PornHub { get; set; } = new SisiSettings("PornHub", "https://rt.pornhub.com", streamproxy: true, rch_access: "apk,cors", stream_access: "apk")
        {
            httpversion = 2,
            rchstreamproxy = "web,cors",
            headers = HeadersModel.Init(
                Http.defaultFullHeaders,
                ("cookie", "platform=pc; accessAgeDisclaimerPH=1"),
                ("sec-fetch-dest", "document"),
                ("sec-fetch-site", "same-origin"),
                ("sec-fetch-mode", "navigate")
            ).ToDictionary(),
            headers_image = HeadersModel.Init(
                ("Accept", "image/jpeg,image/png,image/*;q=0.8,*/*;q=0.5"),
                ("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/534.57.2 (KHTML, like Gecko) Version/5.1.7 Safari/534.57.2"),
                ("Cache-Control", "max-age=0"),
                ("referer", "https://rt.pornhub.com/"),
                ("sec-fetch-dest", "document"),
                ("sec-fetch-mode", "navigate"),
                ("sec-fetch-site", "cross-site"),
                ("sec-fetch-user", "?1")
            ).ToDictionary(),
            headers_stream = HeadersModel.Init(
                ("Accept", "*/*"),
                ("origin", "https://rt.pornhub.com"),
                ("referer", "https://rt.pornhub.com/"),
                ("sec-fetch-dest", "empty"),
                ("sec-fetch-mode", "cors"),
                ("sec-fetch-site", "cross-site")
            ).ToDictionary()
        };

        public SisiSettings PornHubPremium { get; set; } = new SisiSettings("PornHubPremium", "https://rt.pornhubpremium.com", enable: false, stream_access: "apk,cors")
        {
            httpversion = 2,
            rchstreamproxy = "web",
            headers = HeadersModel.Init(
                ("sec-fetch-dest", "document"),
                ("sec-fetch-site", "none"),
                ("sec-fetch-user", "?1"),
                ("upgrade-insecure-requests", "1")
            ).ToDictionary(),
            headers_image = HeadersModel.Init(
                ("Accept", "image/jpeg,image/png,image/*;q=0.8,*/*;q=0.5"),
                ("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/534.57.2 (KHTML, like Gecko) Version/5.1.7 Safari/534.57.2"),
                ("Cache-Control", "max-age=0")
            ).ToDictionary()
        };
    }
}
