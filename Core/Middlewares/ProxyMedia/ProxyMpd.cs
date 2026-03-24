using Microsoft.AspNetCore.Http;
using Shared;
using Shared.Models.Proxy;
using Shared.Models.ServerProxy;
using Shared.Services;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Middlewares
{
    public partial class ProxyAPI
    {
        async public Task ProxyMpd(HttpContext httpContext, ServerproxyConf init, ProxyLinkModel decryptLink, HttpResponseMessage response, string contentType, CancellationTokenSource ctsHttp)
        {
            using (HttpContent content = response.Content)
            {
                if (response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.PartialContent ||
                    response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
                {
                    if (response.Content?.Headers?.ContentLength > init.maxlength_m3u)
                    {
                        httpContext.Response.StatusCode = 503;
                        httpContext.Response.ContentType = "text/plain";
                        await httpContext.Response.WriteAsync("bigfile", ctsHttp.Token).ConfigureAwait(false);
                        return;
                    }

                    string mpd = await content.ReadAsStringAsync(ctsHttp.Token).ConfigureAwait(false);
                    if (mpd == null)
                    {
                        httpContext.Response.StatusCode = 503;
                        await httpContext.Response.WriteAsync("error array mpd", ctsHttp.Token).ConfigureAwait(false);
                        return;
                    }

                    mpd = Regex.Replace(mpd, "<BaseURL>([^<]+)</BaseURL>", m =>
                    {
                        string enc = ProxyLink.Encrypt(m.Groups[1].Value, decryptLink, forceMd5: true);
                        return $"<BaseURL>{CoreInit.Host(httpContext)}/proxy-dash/{enc}/</BaseURL>";
                    }
                    );

                    int contentLength = Encoding.UTF8.GetByteCount(mpd);

                    httpContext.Response.ContentType = contentType ?? "application/dash+xml";
                    httpContext.Response.StatusCode = (int)response.StatusCode;

                    if (response.Headers.AcceptRanges != null)
                        httpContext.Response.Headers["accept-ranges"] = "bytes";

                    if (httpContext.Response.StatusCode is 206 or 416)
                    {
                        var contentRange = response.Content.Headers.ContentRange;
                        if (contentRange != null)
                        {
                            httpContext.Response.Headers["content-range"] = contentRange.ToString();
                        }
                        else
                        {
                            if (httpContext.Response.StatusCode == 206)
                                httpContext.Response.Headers["content-range"] = $"bytes 0-{contentLength - 1}/{contentLength}";

                            if (httpContext.Response.StatusCode == 416)
                                httpContext.Response.Headers["content-range"] = $"bytes */{contentLength}";
                        }
                    }
                    else
                    {
                        if (init.responseContentLength && !CoreInit.CompressionMimeTypes.Contains(httpContext.Response.ContentType))
                            httpContext.Response.ContentLength = contentLength;
                    }

                    await httpContext.Response.WriteAsync(mpd, ctsHttp.Token).ConfigureAwait(false);
                }
                else
                {
                    // проксируем ошибку 
                    await CopyProxyHttpResponse(httpContext, response, null, ctsHttp.Token).ConfigureAwait(false);
                }
            }
        }
    }
}
