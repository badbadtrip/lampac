using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Shared.Models.Base;
using Shared.Models.Events;
using Shared.Models.Module;
using Shared.Models.Module.Interfaces;
using Shared.Services;
using System.Collections.Generic;
using Shared;

namespace Rezka
{
    public class ModInit : IModuleLoaded, IModuleOnline, IModuleOnlineSpider
    {
        public static ModuleConf conf;

        public List<ModuleOnlineItem> Invoke(HttpContext httpContext, RequestModel requestInfo, string host, OnlineEventsModel args)
        {
            return new List<ModuleOnlineItem>()
            {
                new(conf.RezkaPrem, "rhsprem", "HDRezka"),
                new(conf.Rezka)
            };
        }

        public List<ModuleOnlineSpiderItem> Spider(HttpContext httpContext, RequestModel requestInfo, string host, OnlineSpiderModel args)
        {
            return new List<ModuleOnlineSpiderItem>()
            {
                new(conf.RezkaPrem, "rhsprem"),
                new(conf.Rezka)
            };
        }

        public void Loaded(InitspaceModel baseconf)
        {
            CoreInit.conf.online.with_search.Add("rezka");
            CoreInit.conf.online.with_search.Add("rhsprem");

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
            conf = ModuleInvoke.DeserializeInit(new ModuleConf());
        }

        string onlineApiQuality(EventOnlineApiQuality e)
        {
            if (e.balanser == "rezka" && e.kitconf != null)
            {
                bool premium = conf.Rezka.premium;

                if (e.kitconf.TryGetValue("Rezka", out JToken kit))
                {
                    if (kit["premium"] != null)
                        premium = kit.Value<bool>("premium");
                }

                return premium ? " ~ 2160p" : " ~ 720p";
            }
            else
            {
                return e.balanser switch
                {
                    "rhsprem" => " ~ 2160p",
                    _ => null
                };
            }
        }
    }
}
