using Microsoft.AspNetCore.Http;
using Shared.Models.Base;
using Shared.Models.Events;
using Shared.Models.Module;
using Shared.Models.Module.Interfaces;
using Shared.Models.Online.Settings;
using Shared.Services;
using System.Collections.Generic;

namespace Vibix
{
    public class ModInit : IModuleLoaded, IModuleOnline
    {
        public static OnlinesSettings conf;

        public List<ModuleOnlineItem> Invoke(HttpContext httpContext, RequestModel requestInfo, string host, OnlineEventsModel args)
        {
            return new List<ModuleOnlineItem>()
            {
                new(conf)
            };
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
            /// <summary>
            /// api: https://vibix.org/api/external/documentation
            /// iframe: https://coldfilm.ink
            /// </summary>
            conf = ModuleInvoke.Init("Vibix", new OnlinesSettings("Vibix", "https://vibix.org", enable: false)
            {
                displayindex = 585,
                rch_access = "apk",
                stream_access = "apk,cors,web",
                httpversion = 2,
                headers = Http.defaultFullHeaders
            });
        }

        string onlineApiQuality(EventOnlineApiQuality e)
        {
            return e.balanser switch
            {
                "vibix" => " ~ 1080p",
                _ => null
            };
        }
    }
}
