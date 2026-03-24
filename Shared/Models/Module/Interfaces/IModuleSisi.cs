using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Shared.Models.SISI.Base;

namespace Shared.Models.Module.Interfaces
{
    public interface IModuleSisi
    {
        List<ChannelItem> Invoke(HttpContext httpContext, IMemoryCache memoryCache, RequestModel requestInfo, string host, SisiEventsModel args);

        Task<List<ChannelItem>> InvokeAsync(HttpContext httpContext, IMemoryCache memoryCache, RequestModel requestInfo, string host, SisiEventsModel args);
    }
}
