using Microsoft.AspNetCore.Mvc;
using Microsoft.Playwright;
using Online.Models.VDBmovies;
using Shared.PlaywrightCore;
using Shared.Services.RxEnumerate;
using System.Net.Http;

namespace Online.Controllers
{
    public class VDBmovies : BaseOnlineController
    {
        public static List<MovieDB> database;
        static string referer = CrypTo.DecodeBase64("aHR0cHM6Ly9tb3ZpZWJvb20uc3RvcmUv");
        static readonly HttpClient http2Client = FriendlyHttp.CreateHttp2Client();

        public VDBmovies() : base(ModInit.siteConf.VDBmovies) { }

        [HttpGet]
        [Route("lite/vdbmovies")]
        async public Task<ActionResult> Index(string orid, string imdb_id, long kinopoisk_id, string title, string original_title, bool similar, string t, int sid, int s = -1, bool rjson = false)
        {
            if (await IsRequestBlocked(rch: true))
                return badInitMsg;

            var oninvk = new VDBmoviesInvoke
            (
               host,
               init.hls,
               streamfile => HostStreamProxy(streamfile)
            );

            #region поиск
            if (similar || (string.IsNullOrEmpty(orid) && kinopoisk_id == 0))
            {
                if (!init.spider)
                    return OnError("spider");

                var stpl = new SimilarTpl();

                string stitle = StringConvert.SearchName(title);
                string sorigtitle = StringConvert.SearchName(original_title);

                foreach (var j in database)
                {
                    if (stpl.data.Count > 100)
                        break;

                    bool IsOkTitle = false, IsOkID = false;

                    if (kinopoisk_id > 0 && kinopoisk_id == j.kinopoisk_id)
                        IsOkID = true;

                    if (!string.IsNullOrEmpty(imdb_id) && j.imdb_id == imdb_id)
                        IsOkID = true;

                    if (!IsOkID)
                    {
                        if (sorigtitle != null && StringConvert.SearchName(j.orig_title) == sorigtitle)
                            IsOkTitle = true;

                        if (!IsOkTitle && stitle != null)
                        {
                            if (StringConvert.SearchName(j.ru_title) != null)
                            {
                                if (StringConvert.SearchName(j.ru_title).Contains(stitle))
                                    IsOkTitle = true;
                            }

                            if (!IsOkTitle && StringConvert.SearchName(j.orig_title) != null)
                            {
                                if (StringConvert.SearchName(j.orig_title).Contains(stitle))
                                    IsOkTitle = true;
                            }
                        }
                    }

                    if (IsOkTitle || IsOkID)
                    {
                        if (!similar && IsOkID)
                        {
                            orid = j.id;
                            break;
                        }
                        else
                        {
                            string uri = $"{host}/lite/vdbmovies?orid={j.id}";
                            stpl.Append(j.ru_title ?? j.orig_title, j.year.ToString(), string.Empty, uri, PosterApi.Find(j.kinopoisk_id, j.imdb_id));
                        }
                    }
                }

                if (similar || string.IsNullOrEmpty(orid))
                    return ContentTpl(stpl);
            }
        #endregion

        rhubFallback:
            var cache = await InvokeCacheResult<EmbedModel>(ipkey($"vdbmovies:{orid}:{kinopoisk_id}"), 20, textJson: true, onget: async e =>
            {
                string uri = $"{init.host}/kinopoisk/{kinopoisk_id}/iframe";
                if (!string.IsNullOrEmpty(orid))
                    uri = $"{init.host}/content/{orid}/iframe";

                string file = null, forbidden_quality = null, default_quality = null;

                void parseHtml(ReadOnlySpan<char> html)
                {
                    file = Rx.Match(html, "file:([\t ]+)?'#.([^']+)", 2);
                    if (string.IsNullOrEmpty(file))
                        return;

                    forbidden_quality = Rx.Groups(html, "forbidden_quality:([\t ]+)?(\"|')(?<forbidden>[^\"']+)(\"|')")["forbidden"].Value;
                    default_quality = Rx.Groups(html, "default_quality:([\t ]+)?(\"|')(?<quality>[^\"']+)(\"|')")["quality"].Value;
                }

                if (rch?.enable == true || init.priorityBrowser == "http")
                {
                    var headers = httpHeaders(init, HeadersModel.Init(
                        ("sec-fetch-dest", "iframe"),
                        ("sec-fetch-mode", "navigate"),
                        ("sec-fetch-site", "cross-site"),
                        ("referer", referer)
                    ));

                    if (init.httpversion == 2)
                        httpHydra.RegisterHttp(http2Client);

                    await httpHydra.GetSpan(uri, newheaders: headers, spanAction: html =>
                    {
                        parseHtml(html);
                    });
                }
                else
                {
                    string html = await black_magic(uri, referer);
                    parseHtml(html);
                }

                if (string.IsNullOrEmpty(file))
                    return e.Fail("file", refresh_proxy: true);

                return e.Success(oninvk.Embed(oninvk.DecodeEval(file), forbidden_quality, default_quality));
            });

            if (IsRhubFallback(cache))
                goto rhubFallback;

            return ContentTpl(cache,
                () => oninvk.Tpl(cache.Value, orid, imdb_id, kinopoisk_id, title, original_title, t, s, sid, vast: init.vast, rjson: rjson)
            );
        }

        #region black_magic
        async Task<string> black_magic(string uri, string referer)
        {
            try
            {
                using (var browser = new PlaywrightBrowser(init.priorityBrowser))
                {
                    var page = await browser.NewPageAsync(init.plugin, init.headers, proxy: proxy_data, imitationHuman: init.imitationHuman).ConfigureAwait(false);
                    if (page == null)
                        return null;

                    browser.SetFailedUrl(uri);

                    await page.RouteAsync("**/*", async route =>
                    {
                        try
                        {
                            if (route.Request.Url.StartsWith(referer))
                            {
                                await route.FulfillAsync(new RouteFulfillOptions
                                {
                                    Body = PlaywrightBase.IframeHtml(uri)
                                });
                            }
                            else if (route.Request.Url == uri)
                            {
                                string html = null;
                                await route.ContinueAsync();

                                var response = await page.WaitForResponseAsync(route.Request.Url);
                                if (response != null)
                                    html = await response.TextAsync();

                                browser.SetPageResult(html);
                                return;
                            }
                            else
                            {
                                if (!init.imitationHuman || route.Request.Url.EndsWith(".m3u8") || route.Request.Url.Contains("/cdn-cgi/challenge-platform/"))
                                {
                                    PlaywrightBase.ConsoleLog(() => $"Playwright: Abort {route.Request.Url}");
                                    await route.AbortAsync();
                                }
                                else
                                {
                                    if (await PlaywrightBase.AbortOrCache(page, route))
                                        return;

                                    await route.ContinueAsync();
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Serilog.Log.Error(ex, "{Class} {CatchId}", "VDBmovies", "id_kxfhe9mw");
                        }
                    });

                    PlaywrightBase.GotoAsync(page, referer);
                    return await browser.WaitPageResult().ConfigureAwait(false);
                }
            }
            catch { return null; }
        }
        #endregion
    }
}
