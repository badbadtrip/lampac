using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Shared.Services;
using Shared.Models.Base;
using Shared.Models.Proxy;
using Shared.Models.Templates;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;

namespace Shared.Models.Events
{
    public record EventLoadKit(BaseSettings defaultinit, BaseSettings init, JObject userconf, RequestModel requestInfo, IHybridCache hybridCache);

    public record EventMiddleware(bool first, HttpContext httpContext, IMemoryCache memoryCache);

    public record EventBadInitialization(BaseSettings init, bool? rch, RequestModel requestInfo, string host, HttpRequest request, HttpContext httpContext, IHybridCache hybridCache);

    public record EventAppReplace(string source, string token, string arg, string host, RequestModel requestInfo, HttpRequest request, IHybridCache hybridCache);

    public record EventExternalids(string id, string imdb_id, string kinopoisk_id, int serial);

    public record EventHostStreamProxy(BaseSettings conf, string uri, List<HeadersModel> headers, WebProxy proxy, RequestModel requestInfo, HttpContext httpContext, IHybridCache hybridCache);

    public record EventHostImgProxy(RequestModel requestInfo, HttpContext httpContext, string uri, int height, List<HeadersModel> headers, string plugin);

    public record EventMyLocalIp(RequestModel requestInfo, HttpRequest request, HttpContext httpContext, IHybridCache hybridCache);

    public record EventControllerHttpHeaders(string site, Dictionary<string, string> headers, RequestModel requestInfo, HttpRequest request, HttpContext httpContext);

    public record EventStreamQuality(string link, string quality, bool prepend);

    public record EventStreamQualityFirts(IReadOnlyList<StreamQualityDto> data);

    public record EventHttpHandler(string url, HttpClientHandler handler, WebProxy proxy, CookieContainer cookieContainer, IMemoryCache memoryCache);

    public record EventHttpHeaders(string url, HttpRequestMessage client, string cookie, string referer, List<HeadersModel> headers, bool useDefaultHeaders, IMemoryCache memoryCache);

    public record EventHttpResponse(string url, HttpContent data, HttpClient client, string result, HttpResponseMessage response, IMemoryCache memoryCache);

    public record EventProxyApiCreateHttpRequest(string plugin, HttpRequest request, List<HeadersModel> headers, Uri uri, HttpRequestMessage requestMessage);

    public record EventProxyApiCacheStream(HttpContext httpContext, ProxyLinkModel decryptLink);

    public record EventProxyImgMd5key(HttpContext httpContext, RequestModel requestInfo, ProxyLinkModel decryptLink, string href, int width, int height);

    public record EventStaticache(HttpContext httpContext, RequestModel requestInfo);

    public record EventRchRegistry(string connectionId, string ip, string host, RchClientInfo info, NwsConnection connection);

    public record EventRchDisconnected(string connectionId);

    public record EventNwsConnected(string connectionId, RequestModel requestInfo, NwsConnection connection, CancellationToken token);

    public record EventNwsDisconnected(string connectionId);

    public record EventNwsMessage(string connectionId, ReadOnlyMemory<byte> payload, string method, JsonElement args);
}
