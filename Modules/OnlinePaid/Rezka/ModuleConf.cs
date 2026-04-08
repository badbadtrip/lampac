using Shared.Services;

namespace Rezka
{
    public class ModuleConf
    {
        public RezkaSettings Rezka { get; set; } = new RezkaSettings("Rezka", "https://hdrezka.me", true)
        {
            displayindex = 330,
            stream_access = "apk,cors,web",
            ajax = true,
            reserve = true,
            hls = true,
            scheme = "http",
            headers = Http.defaultUaHeaders
        };

        public RezkaSettings RezkaPrem { get; set; } = new RezkaSettings("RezkaPrem", null)
        {
            enable = false,
            rhub_safety = false,
            displayindex = 331,
            stream_access = "apk,cors,web",
            reserve = true,
            hls = true,
            scheme = "http"
        };
    }
}
