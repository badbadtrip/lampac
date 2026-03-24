using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Shared.Models.Base;
using Shared.Models.Module;
using Shared.Models.Module.Interfaces;
using Shared.Models.SISI.Base;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lamson
{
    public class SisiApi : IModuleSisi
    {
        public List<ChannelItem> Invoke(HttpContext httpContext, IMemoryCache memoryCache, RequestModel requestInfo, string host, SisiEventsModel args)
        {
            return new List<ChannelItem>()
            {
                new ChannelItem("PornGram", $"{host}/porngram", 1),
                //new ChannelItem("TwoPorn", $"{host}/twoporn")
            };
        }


        async public Task<List<ChannelItem>> InvokeAsync(HttpContext httpContext, IMemoryCache memoryCache, RequestModel requestInfo, string host, SisiEventsModel args)
        {
            return null;
        }
    }
}
