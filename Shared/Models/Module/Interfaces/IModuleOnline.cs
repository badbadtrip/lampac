using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Shared.Models.Module.Interfaces
{
    public interface IModuleOnline
    {
        List<ModuleOnlineItem> Invoke(HttpContext httpContext, IMemoryCache memoryCache, RequestModel requestInfo, string host, OnlineEventsModel args);

        Task<List<ModuleOnlineItem>> InvokeAsync(HttpContext httpContext, IMemoryCache memoryCache, RequestModel requestInfo, string host, OnlineEventsModel args);


        List<ModuleOnlineSpiderItem> Spider(HttpContext httpContext, IMemoryCache memoryCache, RequestModel requestInfo, string host, OnlineSpiderModel args);

        Task<List<ModuleOnlineSpiderItem>> SpiderAsync(HttpContext httpContext, IMemoryCache memoryCache, RequestModel requestInfo, string host, OnlineSpiderModel args);
    }
}
