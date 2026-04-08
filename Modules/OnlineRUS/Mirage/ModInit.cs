using Microsoft.AspNetCore.Http;
using Shared.Models.Base;
using Shared.Models.Events;
using Shared.Models.Module;
using Shared.Models.Module.Interfaces;
using Shared.PlaywrightCore;
using Shared.Services;
using System.Collections.Generic;

namespace Mirage
{
    public class ModInit : IModuleLoaded, IModuleOnline
    {
        public static ModuleConf conf;

        public List<ModuleOnlineItem> Invoke(HttpContext httpContext, RequestModel requestInfo, string host, OnlineEventsModel args)
        {
            if (PlaywrightBrowser.Status == PlaywrightStatus.disabled)
                return null;

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
            conf = ModuleInvoke.Init("Mirage", new ModuleConf("Mirage", "https://api.apbugall.org", "https://torso-as.stloadi.live", "d94156979ccf2d072540fce92eabe9", "", true, true)
            {
                enable = false,
                displayindex = 510,
                streamproxy = true,
                httpversion = 2,
                headers = Http.defaultFullHeaders
            });
        }

        string onlineApiQuality(EventOnlineApiQuality e)
        {
            return e.balanser == "mirage" ? " ~ 2160p" : null;
        }
    }
}
