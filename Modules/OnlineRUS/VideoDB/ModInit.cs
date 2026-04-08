using Microsoft.AspNetCore.Http;
using Shared.Models.Base;
using Shared.Models.Events;
using Shared.Models.Module;
using Shared.Models.Module.Interfaces;
using Shared.Models.Online.Settings;
using Shared.PlaywrightCore;
using Shared.Services;
using System.Collections.Generic;

namespace VideoDB
{
    public class ModInit : IModuleLoaded, IModuleOnline
    {
        public static OnlinesSettings conf;

        public List<ModuleOnlineItem> Invoke(HttpContext httpContext, RequestModel requestInfo, string host, OnlineEventsModel args)
        {
            var online = new List<ModuleOnlineItem>();

            if (args.kinopoisk_id > 0 && (conf.rhub || conf.priorityBrowser == "http" || PlaywrightBrowser.Status != PlaywrightStatus.disabled))
                online.Add(new(conf));

            return online;
        }

        public void Loaded(InitspaceModel baseconf)
        {
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
            conf = ModuleInvoke.Init("VideoDB", new OnlinesSettings("VideoDB", "https://kinogo.media", "https://30bf3790.obrut.show", streamproxy: true)
            {
                displayindex = 515,
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
            });
        }

        string onlineApiQuality(EventOnlineApiQuality e)
        {
            return e.balanser == "videodb" ? " ~ 2160p" : null;
        }
    }
}
