using Shared.Models.Online.Settings;

namespace Online.Config
{
    public class EngConf
    {
        /// <summary>
        /// https://www.hydraflix.vip
        /// </summary>
        public OnlinesSettings Hydraflix { get; set; } = new OnlinesSettings("Hydraflix", "https://vidfast.pro", streamproxy: true)
        {
            priorityBrowser = "firefox"
        };


        /// <summary>
        /// https://vidsrc.xyz
        /// https://vidsrc.pro
        /// https://vidsrc.to
        /// </summary>
        public OnlinesSettings Vidsrc { get; set; } = new OnlinesSettings("Vidsrc", "https://vidsrc.cc", streamproxy: true);


        public OnlinesSettings MovPI { get; set; } = new OnlinesSettings("MovPI", "https://moviesapi.club", streamproxy: true);


        /// <summary>
        /// https://day2soap.xyz
        /// </summary>
        public OnlinesSettings VidLink { get; set; } = new OnlinesSettings("VidLink", "https://vidlink.pro", streamproxy: true);


        public OnlinesSettings Videasy { get; set; } = new OnlinesSettings("Videasy", "https://player.videasy.net", streamproxy: true);


        /// <summary>
        /// https://smashystream.xyz
        /// </summary>
        public OnlinesSettings Smashystream { get; set; } = new OnlinesSettings("Smashystream", "https://player.smashystream.com", streamproxy: true);


        public OnlinesSettings Autoembed { get; set; } = new OnlinesSettings("Autoembed", "https://player.autoembed.cc", streamproxy: true, enable: false);


        /// <summary>
        /// Omega
        /// </summary>
        public OnlinesSettings Playembed { get; set; } = new OnlinesSettings("Playembed", "https://vidora.su", streamproxy: true, enable: false);


        /// <summary>
        /// EmbedSu
        /// </summary>
        public OnlinesSettings Twoembed { get; set; } = new OnlinesSettings("Twoembed", "https://embed.su", streamproxy: true, enable: false)
        {
            headers_stream = HeadersModel.Init(
                ("accept", "*/*"),
                ("accept-language", "en-US,en;q=0.5"),
                ("referer", "https://embed.su/"),
                ("origin", "https://embed.su"),
                ("sec-fetch-dest", "empty"),
                ("sec-fetch-mode", "cors"),
                ("sec-fetch-site", "cross-site")
            ).ToDictionary()
        };


        public OnlinesSettings Rgshows { get; set; } = new OnlinesSettings("Rgshows", "https://api.rgshows.me", streamproxy: true, enable: false)
        {
            headers = HeadersModel.Init(
                ("accept", "*/*"),
                ("user-agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1"),
                ("accept-language", "en-US,en;q=0.5"),
                ("referer", "https://www.rgshows.me/"),
                ("origin", "https://www.rgshows.me"),
                ("sec-fetch-dest", "empty"),
                ("sec-fetch-mode", "cors"),
                ("sec-fetch-site", "same-site")
            ).ToDictionary(),
            headers_stream = HeadersModel.Init(
                ("accept", "*/*"),
                ("user-agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1"),
                ("accept-language", "en-US,en;q=0.5"),
                ("referer", "https://www.rgshows.me/"),
                ("origin", "https://www.rgshows.me"),
                ("sec-fetch-dest", "empty"),
                ("sec-fetch-mode", "cors"),
                ("sec-fetch-site", "cross-site")
            ).ToDictionary()
        };
    }
}
