using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Shared.Models.Base;
using Shared.Models.Module;
using Shared.Models.Module.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;

namespace Lamson
{
    public class OnlineApi : IModuleOnline
    {
        public List<ModuleOnlineItem> Invoke(HttpContext httpContext, IMemoryCache memoryCache, RequestModel requestInfo, string host, OnlineEventsModel args)
        {
            var online = new List<ModuleOnlineItem>();

            void send(BaseSettings init, string plugin)
            {
                if (init.enable && !init.rip)
                {
                    string url = init.overridehost;
                    if (string.IsNullOrEmpty(url))
                        url = $"{host}/{plugin}";

                    online.Add(new(init.displayname ?? init.plugin, url, plugin, online.Count));
                }
            }

            send(ModInit.KinoGram, "kinogram");

            return online;
        }


        public Task<List<ModuleOnlineItem>> InvokeAsync(HttpContext httpContext, IMemoryCache memoryCache, RequestModel requestInfo, string host, OnlineEventsModel args)
        {
            return Task.FromResult(default(List<ModuleOnlineItem>));
        }


        public List<ModuleOnlineSpiderItem> Spider(HttpContext httpContext, IMemoryCache memoryCache, RequestModel requestInfo, string host, OnlineSpiderModel args)
        {
            var online = new List<ModuleOnlineSpiderItem>();

            void send(BaseSettings init, string plugin)
            {
                if (init.spider && init.enable && !init.rip)
                {
                    string url = init.overridehost;
                    if (string.IsNullOrEmpty(url))
                        url = $"{host}/{plugin}";

                    online.Add(new(init.displayname ?? init.plugin, $"{url}?title={HttpUtility.UrlEncode(args.title)}&clarification=1&rjson=true&similar=true", online.Count));
                }
            }

            if (!args.isanime)
                send(ModInit.KinoGram, "kinogram");

            return online;
        }


        public Task<List<ModuleOnlineSpiderItem>> SpiderAsync(HttpContext httpContext, IMemoryCache memoryCache, RequestModel requestInfo, string host, OnlineSpiderModel args)
        {
            return Task.FromResult(default(List<ModuleOnlineSpiderItem>));
        }
    }
}
