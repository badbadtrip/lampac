using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Playwright;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Online.Models.Lumex;
using Shared.Models.Online.Settings;
using Shared.PlaywrightCore;

namespace Online.Controllers
{
    public class Lumex : BaseOnlineController<LumexSettings>
    {
        static readonly Serilog.ILogger Log = Serilog.Log.ForContext<Lumex>();

        public static List<DatumDB> database;

        public Lumex() : base(ModInit.siteConf.Lumex) { }

        [HttpGet]
        [Route("lite/lumex")]
        async public Task<ActionResult> Index(long content_id, string content_type, string imdb_id, long kinopoisk_id, string title, string original_title, string t, int clarification, int s = -1, int serial = -1, bool rjson = false, bool similar = false)
        {
            if (await IsRequestBlocked(rch: false))
                return badInitMsg;

            if (init.priorityBrowser == "firefox")
            {
                if (Firefox.Status == PlaywrightStatus.disabled)
                    return OnError("Firefox disabled");
            }
            else if (init.priorityBrowser != "http")
            {
                if (Chromium.Status == PlaywrightStatus.disabled)
                    return OnError("Chromium disabled");
            }

            var oninvk = new LumexInvoke
            (
               host,
               init,
               httpHydra,
               streamfile => HostStreamProxy(streamfile)
            );

            if (similar || (content_id == 0 && kinopoisk_id == 0 && string.IsNullOrEmpty(imdb_id)))
            {
                var search = await InvokeCacheResult<SimilarTpl>($"lumex:search:{title}:{original_title}:{clarification}", 40, async e =>
                {
                    var result = await oninvk.Search(title, original_title, serial, clarification, database);
                    if (result == null || result.IsEmpty)
                        return e.Fail("search", refresh_proxy: true);

                    return e.Success(result);
                });

                return ContentTpl(search, () => search.Value);
            }

            var cache = await InvokeCacheResult<EmbedModel>($"videocdn:{content_id}:{content_type}:{kinopoisk_id}:{imdb_id}:{proxyManager?.CurrentProxyIp}", 10, async e =>
            {
                string content_uri = null;
                var content_headers = new List<HeadersModel>();

                #region uri
                string targetUrl = $"https://p.{init.iframehost}/{init.clientId}";
                if (content_id > 0)
                {
                    targetUrl += $"/{content_type}/{content_id}";
                }
                else
                {
                    if (kinopoisk_id > 0)
                        targetUrl += $"?kp_id={kinopoisk_id}";
                    if (!string.IsNullOrEmpty(imdb_id))
                        targetUrl += (targetUrl.Contains("?") ? "&" : "?") + $"imdb_id={imdb_id}";
                }
                #endregion

                if (init.priorityBrowser == "http" && kinopoisk_id > 0)
                {
                    content_uri = $"https://api.{init.iframehost}/content?clientId={init.clientId}&contentType=short&kpId={kinopoisk_id}";

                    content_headers = HeadersModel.Init(new Dictionary<string, string>(Http.defaultUaHeaders)
                    {
                        ["accept"] = "*/*",
                        ["accept-language"] = "ru-RU,ru;q=0.9,uk-UA;q=0.8,uk;q=0.7,en-US;q=0.6,en;q=0.5",
                        ["origin"] = $"https://p.{init.iframehost}",
                        ["referer"] = $"https://p.{init.iframehost}/",
                        ["sec-fetch-site"] = "same-site",
                        ["sec-fetch-mode"] = "cors",
                        ["sec-fetch-dest"] = "empty"
                    });
                }
                else
                {
                    #region Playwright
                    try
                    {
                        using (var browser = new PlaywrightBrowser(init.priorityBrowser))
                        {
                            var page = await browser.NewPageAsync(init.plugin, proxy: proxy_data).ConfigureAwait(false);
                            if (page == null)
                                return null;

                            await page.Context.ClearCookiesAsync(new BrowserContextClearCookiesOptions { Domain = $"api.{init.iframehost}", Name = "x-csrf-token" });

                            await page.RouteAsync("**/*", async route =>
                            {
                                try
                                {
                                    if (content_uri != null || browser.IsCompleted)
                                    {
                                        PlaywrightBase.ConsoleLog(() => $"Playwright: Abort {route.Request.Url}");
                                        await route.AbortAsync();
                                        return;
                                    }

                                    if (route.Request.Url.Contains("/content?clientId="))
                                    {
                                        content_uri = route.Request.Url.Replace("%3D", "=").Replace("%3F", "&");
                                        foreach (var item in route.Request.Headers)
                                        {
                                            if (item.Key is "host" or "accept-encoding" or "connection" or "range" or "cookie")
                                                continue;

                                            content_headers.Add(new HeadersModel(item.Key, item.Value));
                                        }

                                        foreach (var h in new List<(string key, string val)>
                                        {
                                            ("sec-fetch-site", "same-site"),
                                            ("sec-fetch-mode", "cors"),
                                            ("sec-fetch-dest", "empty"),
                                        })
                                        {
                                            if (!route.Request.Headers.ContainsKey(h.key))
                                                content_headers.Add(new HeadersModel(h.key, h.val));
                                        }

                                        browser.SetPageResult(string.Empty);
                                        await route.AbortAsync();
                                        return;
                                    }

                                    if (await PlaywrightBase.AbortOrCache(page, route, abortMedia: true, fullCacheJS: true))
                                        return;

                                    await route.ContinueAsync();
                                }
                                catch (System.Exception ex)
                                {
                                    Log.Error(ex, "CatchId={CatchId}", "id_dp4ha52r");
                                }
                            });

                            PlaywrightBase.GotoAsync(page, targetUrl);
                            await browser.WaitPageResult().ConfigureAwait(false);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error(ex, "CatchId={CatchId}", "id_ks9hstew");
                    }
                    #endregion
                }

                if (content_uri == null)
                    return e.Fail("content_uri", refresh_proxy: true);

                var result = await Http.BaseGet(content_uri, proxy: proxy, headers: content_headers);

                if (string.IsNullOrEmpty(result.content))
                    return e.Fail("content", refresh_proxy: true);

                if (!result.response.Headers.TryGetValues("Set-Cookie", out var cook))
                    return e.Fail("cook", refresh_proxy: true);

                string csrf = Regex.Match(cook.FirstOrDefault() ?? "", "x-csrf-token=([^\n\r; ]+)").Groups[1].Value.Trim();
                if (string.IsNullOrEmpty(csrf))
                    return e.Fail("csrf", refresh_proxy: true);

                content_headers.Add(new HeadersModel("x-csrf-token", csrf.Split("%")[0]));
                content_headers.Add(new HeadersModel("cookie", $"x-csrf-token={csrf}"));

                var md = JsonConvert.DeserializeObject<JObject>(result.content)["player"].ToObject<EmbedModel>();
                md.csrf = CrypTo.md5(DateTime.Now.ToFileTime().ToString());

                hybridCache.Set(md.csrf, content_headers, DateTime.Now.AddDays(1));

                return e.Success(md);
            });

            return ContentTpl(cache,
                () => oninvk.Tpl(cache.Value, accsArgs(string.Empty), content_id, content_type, imdb_id, kinopoisk_id, title, original_title, clarification, t, s, rjson: rjson)
            );
        }

        #region Video
        [HttpGet]
        [Route("lite/lumex/video")]
        [Route("lite/lumex/video.m3u8")]
        async public Task<ActionResult> Video(string playlist, string csrf, int max_quality)
        {
            if (string.IsNullOrEmpty(playlist) || string.IsNullOrEmpty(csrf))
                return OnError();

            if (await IsRequestBlocked(rch: false, rch_check: false))
                return badInitMsg;

            var cache = await InvokeCacheResult<string>($"lumex/video:{playlist}:{csrf}", 20, async e =>
            {
                if (!hybridCache.TryGetValue(csrf, out List<HeadersModel> content_headers))
                    return e.Fail("content_headers");

                var result = await Http.Post<JObject>($"https://api.{init.iframehost}" + playlist, "", httpversion: 2, proxy: proxy, timeoutSeconds: 8, headers: content_headers);

                if (result == null || !result.ContainsKey("url"))
                    return e.Fail("result", refresh_proxy: true);

                string url = result.Value<string>("url");
                if (string.IsNullOrEmpty(url))
                    return e.Fail("url");

                string hls = url.StartsWith("/")
                    ? $"{init.scheme}:{url}"
                    : url;

                return e.Success(hls);
            });

            if (!cache.IsSuccess || string.IsNullOrEmpty(cache.Value))
                return OnError(cache.ErrorMsg);

            string streamUrl = cache.Value;

            if (max_quality > 0 && !init.hls)
            {
                var streamquality = new StreamQualityTpl();

                foreach (int q in new int[] { 1080, 720, 480, 360, 240 })
                {
                    if (max_quality >= q)
                        streamquality.Append(HostStreamProxy(Regex.Replace(streamUrl, "/hls\\.m3u8$", $"/{q}.mp4")), $"{q}p");
                }

                var first = streamquality.Firts();
                if (first == null)
                    return OnError("streams");

                return ContentTo(VideoTpl.ToJson("play", first.link, first.quality, streamquality: streamquality, vast: init.vast));
            }

            return Redirect(HostStreamProxy(streamUrl));
        }
        #endregion
    }
}
