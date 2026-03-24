using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Shared.Models.Events;
using System.Threading.Tasks;

namespace Core.Middlewares
{
    public class Module
    {
        private readonly RequestDelegate _next;
        IMemoryCache memoryCache;
        private readonly bool first;

        public Module(RequestDelegate next, IMemoryCache mem, bool first)
        {
            _next = next;
            memoryCache = mem;
            this.first = first;
        }

        async public Task InvokeAsync(HttpContext httpContext)
        {
            bool next = await EventListener.Middleware.Invoke(first, new EventMiddleware(first, httpContext, memoryCache));
            if (!next)
                return;

            await _next(httpContext);
        }
    }
}
