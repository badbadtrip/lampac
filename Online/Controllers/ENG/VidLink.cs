using Microsoft.AspNetCore.Mvc;
using Shared.PlaywrightCore;

namespace Online.Controllers
{
    public class VidLink : BaseENGController
    {
        public VidLink() : base(ModInit.engConf.VidLink) { }

        [HttpGet]
        [Route("lite/vidlink")]
        public Task<ActionResult> Index(bool checksearch, long id, long tmdb_id, string imdb_id, string title, string original_title, int serial, int s = -1, bool rjson = false)
        {
            return ViewTmdb(checksearch, id, tmdb_id, imdb_id, title, original_title, serial, s, rjson, mp4: true, method: "call");
        }

        #region Video
        [HttpGet]
        [Route("lite/vidlink/video")]
        async public Task<ActionResult> Video(long id, int s = -1, int e = -1, bool play = false)
        {
            if (await IsRequestBlocked(rch: false, rch_check: !play))
                return badInitMsg;

            if (id == 0)
                return OnError();

            string embed = $"{init.host}/movie/{id}";
            if (s > 0)
                embed = $"{init.host}/tv/{id}/{s}/{e}";

            var cache = await InvokeCacheResult<(string file, List<HeadersModel> headers)>($"{embed}:video", 20, async e =>
            {
                if (string.IsNullOrWhiteSpace(embed))
                    return e.Fail("embed");

                var video = await black_magic(embed);
                if (string.IsNullOrWhiteSpace(video.m3u8))
                    return e.Fail("m3u8", refresh_proxy: true);

                var headers_stream = httpHeaders(init.host, init.headers_stream);
                if (headers_stream == null || headers_stream.Count == 0)
                    headers_stream = video.headers;

                if (headers_stream == null || headers_stream.Count == 0)
                    return e.Fail("headers");

                string file = HostStreamProxy(video.m3u8, headers: headers_stream);
                if (string.IsNullOrWhiteSpace(file))
                    return e.Fail("file");

                return e.Success((file, headers_stream));
            });

            if (!cache.IsSuccess)
                return OnError(cache.ErrorMsg);

            if (play)
                return RedirectToPlay(cache.Value.file);

            return ContentTo(VideoTpl.ToJson("play", cache.Value.file, "English", vast: init.vast, headers: init.streamproxy ? null : cache.Value.headers));
        }
        #endregion

        #region black_magic
        async Task<(string m3u8, List<HeadersModel> headers)> black_magic(string uri)
        {
            if (string.IsNullOrEmpty(uri))
                return default;

            try
            {
                string memKey = $"vidlink:black_magic:{uri}";
                if (!hybridCache.TryGetValue(memKey, out (string m3u8, List<HeadersModel> headers) cache))
                {
                    using (var browser = new PlaywrightBrowser(init.priorityBrowser))
                    {
                        var page = await browser.NewPageAsync(init.plugin, httpHeaders(init).ToDictionary(), proxy_data);
                        if (page == null)
                            return default;

                        await page.RouteAsync("**/*", async route =>
                        {
                            try
                            {
                                if (browser.IsCompleted)
                                {
                                    PlaywrightBase.ConsoleLog(() => $"Playwright: Abort {route.Request.Url}");
                                    await route.AbortAsync();
                                    return;
                                }

                                if (await PlaywrightBase.AbortOrCache(page, route, abortMedia: true, fullCacheJS: true, patterCache: "/api/(mercury|venus)$"))
                                    return;

                                if (route.Request.Url.Contains("adsco.") || route.Request.Url.Contains("pubtrky.") || route.Request.Url.Contains("clarity."))
                                {
                                    PlaywrightBase.ConsoleLog(() => $"Playwright: Abort {route.Request.Url}");
                                    await route.AbortAsync();
                                    return;
                                }

                                if (route.Request.Url.Contains(".m3u") || route.Request.Url.Contains(".mp4"))
                                {
                                    cache.headers = new List<HeadersModel>();
                                    foreach (var item in route.Request.Headers)
                                    {
                                        if (item.Key.ToLower() is "host" or "accept-encoding" or "connection" or "range")
                                            continue;

                                        cache.headers.Add(new HeadersModel(item.Key, item.Value.ToString()));
                                    }

                                    PlaywrightBase.ConsoleLog(() => ($"Playwright: SET {route.Request.Url}", cache.headers));
                                    browser.SetPageResult(route.Request.Url);
                                    await route.AbortAsync();
                                    return;
                                }

                                await route.ContinueAsync();
                            }
                            catch (System.Exception ex)
                            {
                                Serilog.Log.Error(ex, "{Class} {CatchId}", "VidLink", "id_ejvmtgh5");
                            }
                        });

                        PlaywrightBase.GotoAsync(page, uri);
                        cache.m3u8 = await browser.WaitPageResult(20);
                    }

                    if (cache.m3u8 == null)
                    {
                        proxyManager?.Refresh();
                        return default;
                    }

                    proxyManager?.Success();
                    hybridCache.Set(memKey, cache, cacheTime(20));
                }

                return cache;
            }
            catch { return default; }
        }
        #endregion
    }
}
