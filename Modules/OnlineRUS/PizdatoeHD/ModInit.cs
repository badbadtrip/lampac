using Microsoft.AspNetCore.Http;
using Shared;
using Shared.Models.Base;
using Shared.Models.Events;
using Shared.Models.Module;
using Shared.Models.Module.Interfaces;
using Shared.Models.Online.Settings;
using Shared.PlaywrightCore;
using Shared.Services;
using System.Collections.Generic;

namespace PizdatoeHD
{
    public class ModInit : IModuleLoaded, IModuleOnline, IModuleOnlineSpider
    {
        public static OnlinesSettings conf;

        public List<ModuleOnlineItem> Invoke(HttpContext httpContext, RequestModel requestInfo, string host, OnlineEventsModel args)
        {
            if (PlaywrightBrowser.Status == PlaywrightStatus.disabled)
                return null;

            return new List<ModuleOnlineItem>()
            {
                new(conf)
            };
        }

        public List<ModuleOnlineSpiderItem> Spider(HttpContext httpContext, RequestModel requestInfo, string host, OnlineSpiderModel args)
        {
            if (PlaywrightBrowser.Status == PlaywrightStatus.disabled)
                return null;

            return new List<ModuleOnlineSpiderItem>()
            {
                new(conf)
            };
        }

        public void Loaded(InitspaceModel baseconf)
        {
            CoreInit.conf.online.with_search.Add("pizdatoehd");

            updateConf();
            EventListener.UpdateInitFile += updateConf;
            EventListener.OnlineApiQuality += onlineApiQuality;
        }

        public void Dispose()
        {
            EventListener.UpdateInitFile -= updateConf;
            EventListener.OnlineApiQuality -= onlineApiQuality;
        }

        void updateConf()
        {
            conf = ModuleInvoke.Init("PizdatoeHD", new OnlinesSettings("pizdatoehd", "https://rezka.ag")
            {
                displayindex = 331,
                hls = true,
                streamproxy = true,
                stream_access = "apk,cors,web",
                headers_stream = HeadersModel.Init(
                    ("accept", "*/*"),
                    ("cache-control", "no-cache"),
                    ("dnt", "1"),
                    ("origin", "https://rezka.ag"),
                    ("pragma", "no-cache"),
                    ("referer", "https://rezka.ag/"),
                    ("sec-fetch-dest", "empty"),
                    ("sec-fetch-mode", "cors"),
                    ("sec-fetch-site", "cross-site")
                ).ToDictionary()
            });
        }

        string onlineApiQuality(EventOnlineApiQuality e)
        {
            return e.balanser switch
            {
                "pizdatoehd" => " ~ 720p",
                _ => null
            };
        }
    }
}
