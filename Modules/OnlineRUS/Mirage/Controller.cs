using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Playwright;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared;
using Shared.Models.AppConf;
using Shared.Models.Base;
using Shared.Models.Templates;
using Shared.PlaywrightCore;
using Shared.Services.RxEnumerate;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Mirage
{
    public class MirageController : BaseOnlineController<ModuleConf>
    {
        static IPage page;
        static Timer timer;
        static (string hls, long id_file, string token_movie, int lastseek, DateTime lastreq) curenthsl = new();

        static MirageController()
        {
            Directory.CreateDirectory("cache/mirage");
            CoreInit.conf.WAF.limit_map.Insert(0, new WafLimitRootMap("^/lite/mirage/trans/", new WafLimitMap { limit = 1000, second = 1 }));

            timer = new Timer(_ =>
            {
                if (page != null && DateTime.Now.AddMinutes(-20) > curenthsl.lastreq)
                {
                    try
                    {
                        page.CloseAsync();
                        page = null;
                        curenthsl = default;

                        foreach (var file in Directory.GetFiles("cache/mirage"))
                            System.IO.File.Delete(file);
                    }
                    catch { }
                }
            }, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1));
        }

        public MirageController() : base(ModInit.conf)
        {
            loadKitInitialization = (j, i, c) =>
            {
                if (j.ContainsKey("m4s"))
                    i.m4s = c.m4s;
                return i;
            };
        }

        [HttpGet]
        [Route("lite/mirage")]
        async public Task<ActionResult> Index(string orid, string imdb_id, long kinopoisk_id, string title, string original_title, int serial, string original_language, int year, int t = -1, int s = -1, bool origsource = false, bool rjson = false, bool similar = false)
        {
            if (similar)
                return await RouteSpiderSearch(title, origsource, rjson);

            if (await IsRequestBlocked(rch: false))
                return badInitMsg;

            var result = await search(orid, imdb_id, kinopoisk_id, title, serial, original_language, year);
            if (result.category_id == 0 || result.data == null)
                return OnError();

            JToken data = result.data;
            string tokenMovie = data["token_movie"] != null ? data.Value<string>("token_movie") : null;
            var frame = await iframe(tokenMovie);
            if (frame.all == null)
                return OnError();

            if (result.category_id is 1 or 3)
            {
                #region Фильм
                var videos = frame.all["theatrical"].ToObject<Dictionary<string, Dictionary<string, JObject>>>();

                var mtpl = new MovieTpl(title, original_title, videos.Count);

                foreach (var i in videos)
                {
                    var file = i.Value.First().Value;

                    string translation = file.Value<string>("translation");
                    string quality = file.Value<string>("quality");
                    long id = file.Value<long>("id");
                    bool uhd = init.m4s ? file.Value<bool>("uhd") : false;

                    string link = $"{host}/lite/mirage/video?id_file={id}&token_movie={data.Value<string>("token_movie")}";
                    string streamlink = accsArgs($"{link.Replace("/video", "/video.m3u8")}&play=true");

                    mtpl.Append(translation, link, "call", streamlink, voice_name: uhd ? "2160p" : quality, quality: uhd ? "2160p" : "");
                }

                return ContentTpl(mtpl);
                #endregion
            }
            else
            {
                #region Сериал
                string defaultargs = $"&orid={orid}&imdb_id={imdb_id}&kinopoisk_id={kinopoisk_id}&title={HttpUtility.UrlEncode(title)}&original_title={HttpUtility.UrlEncode(original_title)}&year={year}&original_language={original_language}";

                if (s == -1)
                {
                    #region Сезоны
                    string q = null;

                    try
                    {
                        if (init.m4s)
                            q = frame.active.Value<bool>("uhd") == true ? "2160p" : null;
                    }
                    catch { }

                    Dictionary<string, JToken> seasons;
                    if (frame.all["seasons"] != null)
                        seasons = frame.all["seasons"].ToObject<Dictionary<string, JToken>>();
                    else
                        seasons = frame.all.ToObject<Dictionary<string, JToken>>();

                    if (seasons.First().Key.StartsWith("t"))
                    {
                        var tpl = new SeasonTpl(q);

                        var seasonNumbers = new HashSet<int>();

                        foreach (var translation in seasons)
                        {
                            var file = translation.Value["file"];
                            if (file == null)
                                continue;

                            foreach (var season in file.ToObject<Dictionary<string, object>>())
                            {
                                if (int.TryParse(season.Key, out int seasonNumber))
                                    seasonNumbers.Add(seasonNumber);
                            }
                        }

                        if (!seasonNumbers.Any())
                            seasonNumbers.Add(frame.active.Value<int>("seasons"));

                        foreach (int i in seasonNumbers.OrderBy(i => i))
                            tpl.Append($"{i} сезон", $"{host}/lite/mirage?rjson={rjson}&s={i}{defaultargs}", i.ToString());

                        return ContentTpl(tpl);
                    }
                    else
                    {
                        var tpl = new SeasonTpl(q, seasons.Count);

                        foreach (var season in seasons)
                            tpl.Append($"{season.Key} сезон", $"{host}/lite/mirage?rjson={rjson}&s={season.Key}{defaultargs}", season.Key);

                        return ContentTpl(tpl);
                    }
                    #endregion
                }
                else
                {
                    var vtpl = new VoiceTpl();
                    var etpl = new EpisodeTpl();
                    var voices = new HashSet<int>();

                    string sArhc = s.ToString();

                    if (frame.all[sArhc] is JArray)
                    {
                        #region Перевод
                        foreach (var episode in frame.all[sArhc])
                        {
                            foreach (var voice in episode.ToObject<Dictionary<string, JObject>>().Select(i => i.Value))
                            {
                                int id_translation = voice.Value<int>("id_translation");
                                if (voices.Contains(id_translation))
                                    continue;

                                voices.Add(id_translation);

                                if (t == -1)
                                    t = id_translation;

                                string link = $"{host}/lite/mirage?rjson={rjson}&s={s}&t={id_translation}{defaultargs}";
                                bool active = t == id_translation;

                                vtpl.Append(voice.Value<string>("translation"), active, link);
                            }
                        }
                        #endregion

                        foreach (var episode in frame.all[sArhc])
                        {
                            foreach (var voice in episode.ToObject<Dictionary<string, JObject>>().Select(i => i.Value))
                            {
                                if (voice.Value<int>("id_translation") != t)
                                    continue;

                                string translation = voice.Value<string>("translation");
                                int e = voice.Value<int>("episode");

                                string link = $"{host}/lite/mirage/video?id_file={voice.Value<long>("id")}&token_movie={data.Value<string>("token_movie")}";
                                string streamlink = accsArgs($"{link.Replace("/video", "/video.m3u8")}&play=true");

                                if (e > 0)
                                    etpl.Append($"{e} серия", title ?? original_title, sArhc, e.ToString(), link, "call", voice_name: translation, streamlink: streamlink);
                            }
                        }
                    }
                    else if (frame.all.ToObject<Dictionary<string, object>>().First().Key.StartsWith("t"))
                    {
                        #region Перевод
                        foreach (var node in frame.all)
                        {
                            if (!node.First["file"].ToObject<Dictionary<string, object>>().ContainsKey(sArhc))
                                continue;

                            var voice = node.First["file"].First.First.First.First;
                            int id_translation = voice.Value<int>("id_translation");
                            if (voices.Contains(id_translation))
                                continue;

                            voices.Add(id_translation);

                            if (t == -1)
                                t = id_translation;

                            string link = $"{host}/lite/mirage?rjson={rjson}&s={s}&t={id_translation}{defaultargs}";
                            bool active = t == id_translation;

                            vtpl.Append(voice.Value<string>("translation"), active, link);
                        }
                        #endregion

                        foreach (var node in frame.all)
                        {
                            foreach (var season in node.First["file"].ToObject<Dictionary<string, object>>())
                            {
                                if (season.Key != sArhc)
                                    continue;

                                if (season.Value is JArray sjar)
                                {

                                }
                                else if (season.Value is JObject sjob)
                                {
                                    foreach (var episode in sjob.ToObject<Dictionary<string, JObject>>())
                                    {
                                        if (episode.Value.Value<int>("id_translation") != t)
                                            continue;

                                        string translation = episode.Value.Value<string>("translation");
                                        int e = episode.Value.Value<int>("episode");

                                        string link = $"{host}/lite/mirage/video?id_file={episode.Value.Value<long>("id")}&token_movie={data.Value<string>("token_movie")}";
                                        string streamlink = accsArgs($"{link.Replace("/video", "/video.m3u8")}&play=true");

                                        if (e > 0)
                                            etpl.Append($"{e} серия", title ?? original_title, sArhc, e.ToString(), link, "call", voice_name: translation, streamlink: streamlink);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        #region Перевод
                        foreach (var episode in frame.all[sArhc].ToObject<Dictionary<string, Dictionary<string, JObject>>>())
                        {
                            foreach (var voice in episode.Value.Select(i => i.Value))
                            {
                                int id_translation = voice.Value<int>("id_translation");
                                if (voices.Contains(id_translation))
                                    continue;

                                voices.Add(id_translation);

                                if (t == -1)
                                    t = id_translation;

                                string link = $"{host}/lite/mirage?rjson={rjson}&s={s}&t={id_translation}{defaultargs}";
                                bool active = t == id_translation;

                                vtpl.Append(voice.Value<string>("translation"), active, link);
                            }
                        }
                        #endregion

                        foreach (var episode in frame.all[sArhc].ToObject<Dictionary<string, Dictionary<string, JObject>>>())
                        {
                            foreach (var voice in episode.Value.Select(i => i.Value))
                            {
                                string translation = voice.Value<string>("translation");
                                if (voice.Value<int>("id_translation") != t)
                                    continue;

                                string link = $"{host}/lite/mirage/video?id_file={voice.Value<long>("id")}&token_movie={data.Value<string>("token_movie")}";
                                string streamlink = accsArgs($"{link.Replace("/video", "/video.m3u8")}&play=true");

                                etpl.Append($"{episode.Key} серия", title ?? original_title, sArhc, episode.Key, link, "call", voice_name: translation, streamlink: streamlink);
                            }
                        }
                    }

                    etpl.Append(vtpl);

                    return ContentTpl(etpl);
                }
                #endregion
            }
        }


        #region Video
        [HttpGet]
        [Route("lite/mirage/video")]
        [Route("lite/mirage/video.m3u8")]
        async public Task<ActionResult> Video(long id_file, string token_movie, bool play)
        {
            if (await IsRequestBlocked(rch: false))
                return badInitMsg;

            string hls = null;

            if (curenthsl.id_file == id_file && curenthsl.token_movie == token_movie)
                hls = curenthsl.hls;
            else
            {
                hls = await goMovie($"{init.linkhost}/?token_movie={token_movie}&token={init.token}", id_file);
                if (hls == null)
                    return OnError();

                curenthsl = (hls, id_file, token_movie, 0, DateTime.Now);
            }

            if (play)
                return Redirect(hls);

            return ContentTo(VideoTpl.ToJson("play", hls, "auto",
                vast: init.vast,
                hls_manifest_timeout: (int)TimeSpan.FromSeconds(20).TotalMilliseconds
            ));
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("lite/mirage/trans/{fileName}")]
        async public Task<ActionResult> Trans(string fileName)
        {
            if (await IsRequestBlocked(rch: false))
                return badInitMsg;

            if (!Regex.IsMatch(fileName, "^([a-z0-9\\-]+\\.[a-z0-9]+)$"))
                return BadRequest();

            curenthsl.lastreq = DateTime.Now;

            string path = $"cache/mirage/{fileName}";
            int.TryParse(Regex.Match(fileName, "seg-([0-9]+)").Groups[1].Value, out int indexSeg);

            if (indexSeg > 20)
            {
                try
                {
                    string oldpath = $"cache/mirage/{fileName.Replace($"seg-{indexSeg}", $"seg-{indexSeg - 4}")}";
                    if (System.IO.File.Exists(oldpath))
                        System.IO.File.Delete(oldpath);
                }
                catch { }
            }

            var timeout = TimeSpan.FromSeconds(20);
            var sw = Stopwatch.StartNew();

            while (!System.IO.File.Exists(path) && sw.Elapsed < timeout)
            {
                if (indexSeg > 0)
                {
                    int seek = (indexSeg * 6) - 10;
                    if (seek > 90 && curenthsl.lastseek != seek)
                    {
                        curenthsl.lastseek = seek;

                        await page.EvaluateAsync(@"() => 
                            document.getElementById('player').contentWindow.postMessage(
                              JSON.stringify({
                                api: ""seek"",
                                value: " + seek + @"
                              }),
                              ""*""
                            );
                        ");
                    }

                    await Task.Delay(4_000);
                }
                else
                {
                    await Task.Delay(1_000);
                }
            }

            string type = fileName.Contains(".m3u")
                ? "application/vnd.apple.mpegurl"
                : "video/MP2T";

            return File(System.IO.File.OpenRead(path), type);
        }
        #endregion


        #region iframe
        async Task<(JToken all, JToken active)> iframe(string token_movie)
        {
            if (string.IsNullOrEmpty(token_movie))
                return default;

            string memKey = $"mirage:iframe:{token_movie}";
            if (!hybridCache.TryGetValue(memKey, out (JToken all, JToken active) cache))
            {
                string json = null;

                string uri = $"{init.linkhost}/?token_movie={token_movie}&token={init.token}";

                await httpHydra.GetSpan(uri, safety: true, addheaders: HeadersModel.Init(
                    ("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7"),
                    ("referer", "https://alloha.tv/"),
                    ("sec-fetch-dest", "iframe"),
                    ("sec-fetch-mode", "navigate"),
                    ("sec-fetch-site", "cross-site"),
                    ("upgrade-insecure-requests", "1")
                ),
                spanAction: html =>
                {
                    json = Rx.Match(html, "fileList = JSON.parse\\('([^\n\r]+)'\\);");
                });

                if (string.IsNullOrEmpty(json))
                    return default;

                try
                {
                    var root = JsonConvert.DeserializeObject<JObject>(json);
                    if (root == null || !root.ContainsKey("all"))
                        return default;

                    cache = (root["all"], root["active"]);

                    hybridCache.Set(memKey, cache, cacheTime(40));
                }
                catch { return default; }
            }

            return cache;
        }
        #endregion

        #region goMovie
        async Task<string> goMovie(string uri, long id_file)
        {
            try
            {
                var browser = new PlaywrightBrowser();

                if (page != null)
                    await page.CloseAsync();

                try
                {
                    foreach (var file in Directory.GetFiles("cache/mirage"))
                        System.IO.File.Delete(file);
                }
                catch { }

                page = await browser.NewPageAsync(init.plugin, proxy: proxy_data, keepopen: false).ConfigureAwait(false);
                if (page == null)
                    return default;

                await page.RouteAsync("**/*", async route =>
                {
                    try
                    {
                        if (route.Request.Url.Contains("alloha.tv"))
                        {
                            await route.FulfillAsync(new RouteFulfillOptions
                            {
                                Body = PlaywrightBase.IframeHtml(uri + "&autoplay")
                            });
                        }
                        else if (route.Request.Url.Contains("/?token_movie="))
                        {
                            var fetchHeaders = route.Request.Headers;
                            fetchHeaders.TryAdd("accept-encoding", "gzip, deflate, br, zstd");
                            fetchHeaders.TryAdd("cache-control", "no-cache");
                            fetchHeaders.TryAdd("pragma", "no-cache");
                            fetchHeaders.TryAdd("sec-fetch-dest", "iframe");
                            fetchHeaders.TryAdd("sec-fetch-mode", "navigate");
                            fetchHeaders.TryAdd("sec-fetch-site", "cross-site");
                            fetchHeaders.TryAdd("sec-fetch-storage-access", "active");

                            var fetchResponse = await route.FetchAsync(new RouteFetchOptions
                            {
                                Url = route.Request.Url,
                                Method = "GET",
                                Headers = fetchHeaders,
                            }).ConfigureAwait(false);

                            string body = await fetchResponse.TextAsync().ConfigureAwait(false);

                            var injected = @"
                                <script>
                                (function() {
                                    localStorage.setItem('allplay', '{""captionParam"":{""fontSize"":""100%"",""colorText"":""Белый"",""colorBackground"":""Черный"",""opacityText"":""100%"",""opacityBackground"":""75%"",""styleText"":""Без контура"",""weightText"":""Обычный текст""},""quality"":" + (init.m4s ? "2160" : "1080") + @",""volume"":0.5,""muted"":true,""label"":""(Russian) Forced"",""captions"":false}');
                                })();
                                </script>";

                            await route.FulfillAsync(new RouteFulfillOptions
                            {
                                Status = fetchResponse.Status,
                                Body = injected + body,
                                Headers = fetchResponse.Headers
                            }).ConfigureAwait(false);
                        }
                        else if (route.Request.Method == "POST" && route.Request.Url.Contains("/movies/"))
                        {
                            string newUrl = Regex.Replace(route.Request.Url, "/[0-9]+$", $"/{id_file}");

                            var fetchHeaders = route.Request.Headers;
                            fetchHeaders.TryAdd("accept-encoding", "gzip, deflate, br, zstd");
                            fetchHeaders.TryAdd("cache-control", "no-cache");
                            fetchHeaders.TryAdd("dnt", "1");
                            fetchHeaders.TryAdd("pragma", "no-cache");
                            fetchHeaders.TryAdd("priority", "u=1, i");
                            fetchHeaders.TryAdd("sec-fetch-dest", "empty");
                            fetchHeaders.TryAdd("sec-fetch-mode", "cors");
                            fetchHeaders.TryAdd("sec-fetch-site", "same-origin");
                            fetchHeaders.TryAdd("sec-fetch-storage-access", "active");

                            var fetchResponse = await route.FetchAsync(new RouteFetchOptions
                            {
                                Url = newUrl,
                                Method = "POST",
                                Headers = fetchHeaders,
                                PostData = route.Request.PostDataBuffer
                            }).ConfigureAwait(false);

                            string json = await fetchResponse.TextAsync().ConfigureAwait(false);

                            await route.FulfillAsync(new RouteFulfillOptions
                            {
                                Status = fetchResponse.Status,
                                Body = json,
                                Headers = fetchResponse.Headers
                            }).ConfigureAwait(false);
                        }
                        else
                        {
                            if (route.Request.Url.Contains("/stat") || route.Request.Url.Contains("/lists.php"))
                            {
                                await route.AbortAsync();
                                return;
                            }

                            await route.ContinueAsync();
                        }
                    }
                    catch { }
                });

                TaskCompletionSource<bool> tcsPageResponse = new();

                page.Response += async (s, e) =>
                {
                    if (e.Request.Method == "GET")
                    {
                        try
                        {
                            if ((e.Url.Contains(".ts") || e.Url.Contains(".m4s")) && !tcsPageResponse.Task.IsCompleted)
                            {
                                tcsPageResponse.SetResult(true);

                                await page.EvaluateAsync(@"() => 
                                    document.getElementById('player').contentWindow.postMessage(
                                      JSON.stringify({
                                        api: ""pause""
                                      }),
                                      ""*""
                                    );
                                ");
                            }
                        }
                        catch { }

                        if (e.Url.Contains(".m3u8") ||
                            e.Url.Contains(".ts") ||
                            e.Url.Contains(".mp4") ||
                            e.Url.Contains(".m4s"))
                        {
                            try
                            {
                                var file = await e.BodyAsync();
                                System.IO.File.WriteAllBytes($"cache/mirage/{Path.GetFileName(e.Url)}", file);
                            }
                            catch { }
                        }
                    }
                };

                PlaywrightBase.GotoAsync(page, "https://alloha.tv/");

                if (await tcsPageResponse.Task.WaitAsync(TimeSpan.FromSeconds(15)))
                    return $"{host}/lite/mirage/trans/master.m3u8";
                else
                {
                    await page.CloseAsync();
                    return default;
                }
            }
            catch
            {
                if (page != null)
                    await page.CloseAsync();

                return default;
            }
        }
        #endregion


        #region SpiderSearch
        [HttpGet]
        [Route("lite/mirage-search")]
        async public Task<ActionResult> RouteSpiderSearch(string title, bool origsource = false, bool rjson = false)
        {
            if (string.IsNullOrWhiteSpace(title))
                return OnError();

            if (await IsRequestBlocked(rch: false))
                return badInitMsg;

            var cache = await InvokeCacheResult<JArray>($"mirage:search:{title}", 40, async e =>
            {
                var root = await httpHydra.Get<JObject>($"{init.apihost}/?token={init.token}&name={HttpUtility.UrlEncode(title)}&list", safety: true);
                if (root == null || !root.ContainsKey("data"))
                    return e.Fail("data");

                return e.Success(root["data"].ToObject<JArray>());
            });

            return ContentTpl(cache, () =>
            {
                var stpl = new SimilarTpl(cache.Value.Count);

                foreach (var j in cache.Value)
                {
                    string uri = $"{host}/lite/mirage?orid={j.Value<string>("token_movie")}";
                    stpl.Append(j.Value<string>("name") ?? j.Value<string>("original_name"), j.Value<int>("year").ToString(), string.Empty, uri, PosterApi.Size(j.Value<string>("poster")));
                }

                return stpl;
            });
        }
        #endregion

        #region search
        async ValueTask<(bool refresh_proxy, int category_id, JToken data)> search(string token_movie, string imdb_id, long kinopoisk_id, string title, int serial, string original_language, int year)
        {
            string memKey = $"mirage:view:{kinopoisk_id}:{imdb_id}";
            if (0 >= kinopoisk_id && string.IsNullOrEmpty(imdb_id))
                memKey = $"mirage:viewsearch:{title}:{serial}:{original_language}:{year}";

            if (!string.IsNullOrEmpty(token_movie))
                memKey = $"mirage:view:{token_movie}";

            JObject root;

            if (!hybridCache.TryGetValue(memKey, out (int category_id, JToken data) res))
            {
                string stitle = title.ToLowerAndTrim();

                if (memKey.Contains(":viewsearch:"))
                {
                    if (string.IsNullOrWhiteSpace(title) || year == 0)
                        return default;

                    root = await httpHydra.Get<JObject>($"{init.apihost}/?token={init.token}&name={HttpUtility.UrlEncode(title)}&list={(serial == 1 ? "serial" : "movie")}", safety: true);
                    if (root == null)
                        return (true, 0, null);

                    if (root.ContainsKey("data"))
                    {
                        foreach (var item in root["data"])
                        {
                            if (item.Value<string>("name")?.ToLowerAndTrim() == stitle)
                            {
                                int y = item.Value<int>("year");
                                if (y > 0 && (y == year || y == (year - 1) || y == (year + 1)))
                                {
                                    if (original_language == "ru" && item.Value<string>("country")?.ToLowerAndTrim() != "россия")
                                        continue;

                                    res.data = item;
                                    res.category_id = item.Value<int>("category_id");
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    root = await httpHydra.Get<JObject>($"{init.apihost}/?token={init.token}&kp={kinopoisk_id}&imdb={imdb_id}&token_movie={token_movie}", safety: true);
                    if (root == null)
                        return (true, 0, null);

                    if (root.ContainsKey("data"))
                    {
                        res.data = root.GetValue("data");
                        res.category_id = res.data.Value<int>("category");
                    }
                }

                if (res.data != null || (root.ContainsKey("error_info") && root.Value<string>("error_info") == "not movie"))
                    hybridCache.Set(memKey, res, cacheTime(res.category_id is 1 or 3 ? 120 : 40));
                else
                    hybridCache.Set(memKey, res, cacheTime(2));
            }

            return (false, res.category_id, res.data);
        }
        #endregion
    }





































    //public class Mirage : BaseOnlineController<AllohaSettings>
    //{
    //    static readonly Serilog.ILogger Log = Serilog.Log.ForContext<Mirage>();

    //    static string edge_hash = null, referer = null, origin = null, wsUri = null;
    //    static int current_time = 0;

    //    static Mirage()
    //    {
    //        EventListener.ProxyApiCreateHttpRequest += e =>
    //        {
    //            if (edge_hash != null && e.plugin != null && e.plugin.Equals("mirage", StringComparison.OrdinalIgnoreCase))
    //            {
    //                //e.requestMessage.Headers.Remove("Accepts-Controls");
    //                //e.requestMessage.Headers.TryAddWithoutValidation("Accepts-Controls", edge_hash);

    //                string absolutePath = e.uri.AbsolutePath;
    //                string seg = Regex.Match(absolutePath, "/seg-([0-9]+)").Groups[1].Value;
    //                if (int.TryParse(seg, out int indexSeg) && indexSeg > 0)
    //                {
    //                    int time = (indexSeg * 6) - 120;
    //                    if (time > current_time)
    //                        current_time = time;
    //                }

    //                var headers = HeadersModel.Init
    //                (
    //                    ("accept", "*/*"),
    //                    ("accept-language", "ru-RU,ru;q=0.9,uk-UA;q=0.8,uk;q=0.7,en-US;q=0.6,en;q=0.5"),
    //                    ("accepts-controls", edge_hash),
    //                    ("authorizations", "Bearer pXzvbyDGLYyB6VkwsWZDv3iMKZtsXNzpzRyxZUcsKHXxsSeaYakbo3hw9mBFRc5VQTpqAX6BW8aDEqyLaHYcXSQiV6KHYTVTK6MYRphNAy5sBjtrevqkDzKmLqNdfMZGEU9NELjmtKfZy3RNGzCd767sNh1mXEj4tCcvqndHtzmwAbZNkhm4ghDEasodotMBewypNQ56uotJAQGX11csfeRfBAPk8DcUWWkkqzxca8vbnEw12vUFbBzT6hz8ZB3F3dzUhUXoL2cr1WM1bXQArRCS1MUNMz3X5WDMMQoZKxj2AMTRqp7QQX4dDB9B7VzEZTmyFULhm1AcHHMkoMvSVvKYoBoAKLycYAgMHeD4ECJcGEAGpnkJhrV57zQ7"),
    //                    ("cache-control", "no-cache"),
    //                    ("origin", origin),
    //                    ("pragma", "no-cache"),
    //                    ("referer", referer),
    //                    ("sec-ch-ua", "\"Chromium\";v=\"146\", \"Not-A.Brand\";v=\"24\", \"Google Chrome\";v=\"146\""),
    //                    ("sec-ch-ua-mobile", "?0"),
    //                    ("sec-ch-ua-platform", "\"Windows\""),
    //                    ("sec-fetch-dest", "empty"),
    //                    ("sec-fetch-mode", "cors"),
    //                    ("sec-fetch-site", "cross-site"),
    //                    ("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 Safari/537.36")
    //                );

    //                ////using (var client = new HttpClient())
    //                ////{
    //                ////    var request = new HttpRequestMessage(HttpMethod.Options, e.uri);

    //                ////    foreach (var item in headers)
    //                ////        request.Headers.TryAddWithoutValidation(item.name, item.val);

    //                ////    client.SendAsync(request).Wait();
    //                ////}

    //                e.requestMessage.Headers.Clear();

    //                foreach (var item in headers)
    //                    e.requestMessage.Headers.TryAddWithoutValidation(item.name, item.val);

    //                if (e.requestMessage.Content?.Headers != null)
    //                    e.requestMessage.Content.Headers.Clear();
    //            }

    //            return Task.CompletedTask;
    //        };

    //        //_ = Task.Run(async () =>
    //        //{
    //        //    while (wsUri == null)
    //        //        await Task.Delay(100);

    //        //    dfsd(wsUri);
    //        //});
    //    }

    //    public Mirage() : base(ModInit.conf.Mirage)
    //    {
    //        loadKitInitialization = (j, i, c) =>
    //        {
    //            if (j.ContainsKey("m4s"))
    //                i.m4s = c.m4s;
    //            return i;
    //        };
    //    }

    //    [HttpGet]
    //    [Route("lite/mirage")]
    //    async public Task<ActionResult> Index(string orid, string imdb_id, long kinopoisk_id, string title, string original_title, int serial, string original_language, int year, int t = -1, int s = -1, bool origsource = false, bool rjson = false, bool similar = false)
    //    {
    //        if (similar)
    //            return await RouteSpiderSearch(title, origsource, rjson);

    //        if (await IsRequestBlocked(rch: false))
    //            return badInitMsg;

    //        var result = await search(orid, imdb_id, kinopoisk_id, title, serial, original_language, year);
    //        if (result.category_id == 0 || result.data == null)
    //            return OnError();

    //        JToken data = result.data;
    //        string tokenMovie = data["token_movie"] != null ? data.Value<string>("token_movie") : null;
    //        var frame = await iframe(tokenMovie);
    //        if (frame.all == null)
    //            return OnError();

    //        //return ContentTo(JsonConvert.SerializeObject(frame.all));

    //        if (result.category_id is 1 or 3)
    //        {
    //            #region Фильм
    //            var videos = frame.all["theatrical"].ToObject<Dictionary<string, Dictionary<string, JObject>>>();

    //            var mtpl = new MovieTpl(title, original_title, videos.Count);

    //            foreach (var i in videos)
    //            {
    //                var file = i.Value.First().Value;

    //                string translation = file.Value<string>("translation");
    //                string quality = file.Value<string>("quality");
    //                long id = file.Value<long>("id");
    //                bool uhd = init.m4s ? file.Value<bool>("uhd") : false;

    //                string link = $"{host}/lite/mirage/video?id_file={id}&token_movie={data.Value<string>("token_movie")}";
    //                string streamlink = accsArgs($"{link.Replace("/video", "/video.m3u8")}&play=true");

    //                mtpl.Append(translation, link, "call", streamlink, voice_name: uhd ? "2160p" : quality, quality: uhd ? "2160p" : "");
    //            }

    //            return ContentTpl(mtpl);
    //            #endregion
    //        }
    //        else
    //        {
    //            #region Сериал
    //            string defaultargs = $"&orid={orid}&imdb_id={imdb_id}&kinopoisk_id={kinopoisk_id}&title={HttpUtility.UrlEncode(title)}&original_title={HttpUtility.UrlEncode(original_title)}&year={year}&original_language={original_language}";

    //            if (s == -1)
    //            {
    //                #region Сезоны
    //                string q = null;
    //                try
    //                {
    //                    if (init.m4s)
    //                        q = frame.active.Value<bool>("uhd") == true ? "2160p" : null;
    //                }
    //                catch (System.Exception ex)
    //                {
    //                    Log.Error(ex, "CatchId={CatchId}", "id_ohti3cp4");
    //                }

    //                Dictionary<string, JToken> seasons;
    //                if (frame.all["seasons"] != null)
    //                    seasons = frame.all["seasons"].ToObject<Dictionary<string, JToken>>();
    //                else
    //                    seasons = frame.all.ToObject<Dictionary<string, JToken>>();

    //                if (seasons.First().Key.StartsWith("t"))
    //                {
    //                    var tpl = new SeasonTpl(q);

    //                    var seasonNumbers = new HashSet<int>();

    //                    foreach (var translation in seasons)
    //                    {
    //                        var file = translation.Value["file"];
    //                        if (file == null)
    //                            continue;

    //                        foreach (var season in file.ToObject<Dictionary<string, object>>())
    //                        {
    //                            if (int.TryParse(season.Key, out int seasonNumber))
    //                                seasonNumbers.Add(seasonNumber);
    //                        }
    //                    }

    //                    if (!seasonNumbers.Any())
    //                        seasonNumbers.Add(frame.active.Value<int>("seasons"));

    //                    foreach (int i in seasonNumbers.OrderBy(i => i))
    //                        tpl.Append($"{i} сезон", $"{host}/lite/mirage?rjson={rjson}&s={i}{defaultargs}", i.ToString());

    //                    return ContentTpl(tpl);
    //                }
    //                else
    //                {
    //                    var tpl = new SeasonTpl(q, seasons.Count);

    //                    foreach (var season in seasons)
    //                        tpl.Append($"{season.Key} сезон", $"{host}/lite/mirage?rjson={rjson}&s={season.Key}{defaultargs}", season.Key);

    //                    return ContentTpl(tpl);
    //                }
    //                #endregion
    //            }
    //            else
    //            {
    //                var vtpl = new VoiceTpl();
    //                var etpl = new EpisodeTpl();
    //                var voices = new HashSet<int>();

    //                string sArhc = s.ToString();

    //                if (frame.all[sArhc] is JArray)
    //                {
    //                    #region Перевод
    //                    foreach (var episode in frame.all[sArhc])
    //                    {
    //                        foreach (var voice in episode.ToObject<Dictionary<string, JObject>>().Select(i => i.Value))
    //                        {
    //                            int id_translation = voice.Value<int>("id_translation");
    //                            if (voices.Contains(id_translation))
    //                                continue;

    //                            voices.Add(id_translation);

    //                            if (t == -1)
    //                                t = id_translation;

    //                            string link = $"{host}/lite/mirage?rjson={rjson}&s={s}&t={id_translation}{defaultargs}";
    //                            bool active = t == id_translation;

    //                            vtpl.Append(voice.Value<string>("translation"), active, link);
    //                        }
    //                    }
    //                    #endregion

    //                    foreach (var episode in frame.all[sArhc])
    //                    {
    //                        foreach (var voice in episode.ToObject<Dictionary<string, JObject>>().Select(i => i.Value))
    //                        {
    //                            if (voice.Value<int>("id_translation") != t)
    //                                continue;

    //                            string translation = voice.Value<string>("translation");
    //                            int e = voice.Value<int>("episode");

    //                            string link = $"{host}/lite/mirage/video?id_file={voice.Value<long>("id")}&token_movie={data.Value<string>("token_movie")}";
    //                            string streamlink = accsArgs($"{link.Replace("/video", "/video.m3u8")}&play=true");

    //                            if (e > 0)
    //                                etpl.Append($"{e} серия", title ?? original_title, sArhc, e.ToString(), link, "call", voice_name: translation, streamlink: streamlink);
    //                        }
    //                    }
    //                }
    //                else if (frame.all.ToObject<Dictionary<string, object>>().First().Key.StartsWith("t"))
    //                {
    //                    #region Перевод
    //                    foreach (var node in frame.all)
    //                    {
    //                        if (!node.First["file"].ToObject<Dictionary<string, object>>().ContainsKey(sArhc))
    //                            continue;

    //                        var voice = node.First["file"].First.First.First.First;
    //                        int id_translation = voice.Value<int>("id_translation");
    //                        if (voices.Contains(id_translation))
    //                            continue;

    //                        voices.Add(id_translation);

    //                        if (t == -1)
    //                            t = id_translation;

    //                        string link = $"{host}/lite/mirage?rjson={rjson}&s={s}&t={id_translation}{defaultargs}";
    //                        bool active = t == id_translation;

    //                        vtpl.Append(voice.Value<string>("translation"), active, link);
    //                    }
    //                    #endregion

    //                    foreach (var node in frame.all)
    //                    {
    //                        foreach (var season in node.First["file"].ToObject<Dictionary<string, object>>())
    //                        {
    //                            if (season.Key != sArhc)
    //                                continue;

    //                            if (season.Value is JArray sjar)
    //                            {

    //                            }
    //                            else if (season.Value is JObject sjob)
    //                            {
    //                                foreach (var episode in sjob.ToObject<Dictionary<string, JObject>>())
    //                                {
    //                                    if (episode.Value.Value<int>("id_translation") != t)
    //                                        continue;

    //                                    string translation = episode.Value.Value<string>("translation");
    //                                    int e = episode.Value.Value<int>("episode");

    //                                    string link = $"{host}/lite/mirage/video?id_file={episode.Value.Value<long>("id")}&token_movie={data.Value<string>("token_movie")}";
    //                                    string streamlink = accsArgs($"{link.Replace("/video", "/video.m3u8")}&play=true");

    //                                    if (e > 0)
    //                                        etpl.Append($"{e} серия", title ?? original_title, sArhc, e.ToString(), link, "call", voice_name: translation, streamlink: streamlink);
    //                                }
    //                            }
    //                        }
    //                    }
    //                }
    //                else
    //                {
    //                    #region Перевод
    //                    foreach (var episode in frame.all[sArhc].ToObject<Dictionary<string, Dictionary<string, JObject>>>())
    //                    {
    //                        foreach (var voice in episode.Value.Select(i => i.Value))
    //                        {
    //                            int id_translation = voice.Value<int>("id_translation");
    //                            if (voices.Contains(id_translation))
    //                                continue;

    //                            voices.Add(id_translation);

    //                            if (t == -1)
    //                                t = id_translation;

    //                            string link = $"{host}/lite/mirage?rjson={rjson}&s={s}&t={id_translation}{defaultargs}";
    //                            bool active = t == id_translation;

    //                            vtpl.Append(voice.Value<string>("translation"), active, link);
    //                        }
    //                    }
    //                    #endregion

    //                    foreach (var episode in frame.all[sArhc].ToObject<Dictionary<string, Dictionary<string, JObject>>>())
    //                    {
    //                        foreach (var voice in episode.Value.Select(i => i.Value))
    //                        {
    //                            string translation = voice.Value<string>("translation");
    //                            if (voice.Value<int>("id_translation") != t)
    //                                continue;

    //                            string link = $"{host}/lite/mirage/video?id_file={voice.Value<long>("id")}&token_movie={data.Value<string>("token_movie")}";
    //                            string streamlink = accsArgs($"{link.Replace("/video", "/video.m3u8")}&play=true");

    //                            etpl.Append($"{episode.Key} серия", title ?? original_title, sArhc, episode.Key, link, "call", voice_name: translation, streamlink: streamlink);
    //                        }
    //                    }
    //                }

    //                etpl.Append(vtpl);

    //                return ContentTpl(etpl);
    //            }
    //            #endregion
    //        }
    //    }


    //    #region Video
    //    [HttpGet]
    //    [Route("lite/mirage/video")]
    //    [Route("lite/mirage/video.m3u8")]
    //    async public Task<ActionResult> Video(long id_file, string token_movie, bool play)
    //    {
    //        if (await IsRequestBlocked(rch: false, rch_check: !play))
    //            return badInitMsg;

    //        string memKey = $"mirage:video:{id_file}:{init.m4s}";
    //        if (!hybridCache.TryGetValue(memKey, out (string hls, List<HeadersModel> headers) movie))
    //        {
    //            movie = await goMovie($"{init.linkhost}/?token_movie={token_movie}&token={init.token}", id_file);
    //            if (movie.hls == null)
    //                return OnError();

    //            hybridCache.Set(memKey, movie, cacheTime(10));
    //        }

    //        var streamquality = new StreamQualityTpl();
    //        streamquality.Append(HostStreamProxy(movie.hls, headers: movie.headers, forceMd5: true), "auto");

    //        var first = streamquality.Firts();
    //        if (first == null)
    //            return OnError("streams");

    //        if (play)
    //            return Redirect(first.link);

    //        return ContentTo(VideoTpl.ToJson("play", first.link, "auto",
    //            streamquality: streamquality,
    //            vast: init.vast,
    //            headers: movie.headers,
    //            hls_manifest_timeout: (int)TimeSpan.FromSeconds(20).TotalMilliseconds
    //        ));
    //    }
    //    #endregion

    //    #region iframe
    //    async Task<(JToken all, JToken active)> iframe(string token_movie)
    //    {
    //        if (string.IsNullOrEmpty(token_movie))
    //            return default;

    //        string memKey = $"mirage:iframe:{token_movie}";
    //        if (!hybridCache.TryGetValue(memKey, out (JToken all, JToken active) cache))
    //        {
    //            string json = null;

    //            string uri = $"{init.linkhost}/?token_movie={token_movie}&token={init.token}";
    //            string referer = "https://alloha.tv/";// $"https://lgfilm.fun/" + reffers[Random.Shared.Next(0, reffers.Length)];

    //            await httpHydra.GetSpan(uri, safety: true, addheaders: HeadersModel.Init(
    //                ("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7"),
    //                ("referer", referer),
    //                ("sec-fetch-dest", "iframe"),
    //                ("sec-fetch-mode", "navigate"),
    //                ("sec-fetch-site", "cross-site"),
    //                ("upgrade-insecure-requests", "1")
    //            ),
    //            spanAction: html =>
    //            {
    //                json = Rx.Match(html, "fileList = JSON.parse\\('([^\n\r]+)'\\);");
    //            });

    //            if (string.IsNullOrEmpty(json))
    //                return default;

    //            try
    //            {
    //                var root = JsonConvert.DeserializeObject<JObject>(json);
    //                if (root == null || !root.ContainsKey("all"))
    //                    return default;

    //                cache = (root["all"], root["active"]);

    //                hybridCache.Set(memKey, cache, cacheTime(40));
    //            }
    //            catch { return default; }
    //        }

    //        return cache;
    //    }
    //    #endregion

    //    #region goMovie
    //    async Task<(string hls, List<HeadersModel> headers)> goMovie(string uri, long id_file)
    //    {

    //        //string waUri = "wss://torso-as.stloadi.live/ws/?sid=cCv5HsoClo-HOh0GQ5MAzKqxYRCa5GByi7ev3EdSVl7nqFQPNAr2H_868rdHbuP33kngaw9AT4qqb4ySSx1WhQZJeLJqnzdb1QLnvbmOxEWD-mpL8Q2jSQ4qzDo_DDo1UHrjMi8QgaE14GdUHI7mtXjcnWw2yg9RQ8e24N_Ik1PEdn4ourBuItEUiPFXvfHNQxtcklmI5yyhbfit_D_cQjKXA9flwLp0l8kLegjryUSxio2vHCgqVNR55IWTJxgqrJqjHE_qqlos8KovH4AbpUboXMB1TWq_cGTZjEyPw995cgNiAPH5rmNDRc8HZb6UDeE_Uh2OlFpuNaDBIwzQ&v=2.1&t=1775051574447";
    //        //dfsd(waUri);

    //        //while (edge_hash == null)
    //        //    await Task.Delay(100);

    //        //origin = "https://torso-as.stloadi.live";
    //        //referer = "https://torso-as.stloadi.live/?token_movie=3891fd58401c7f1901d97be42ba7c9&token=c051a15c3dd3a03468b981b8b5ed11";
    //        //string m3u = "https://6c9-71e-504gv.stream-balancer-allo-1.live/0/kntSvPCj4ueT5YKWHh6lx0vdtPRdjxM_SCXbq3jAr-E7y1py6Zigt7uao94YPnHjdl6ENAukP2l5useU0LeQTw9LJV9jQNNRxHEnuPTHYO-fAZcom9MrcqFlDlwHmqu-KsjIBFti3wb96q0NGLdNwxoHJ5Mxsdl7FBSjzvY0ojFZoh59hF2KD9WBrDuvafwKe_ygFU0brCv9dQxJm_b_0b1LIjeG27Ujs_KYEAvGtanrsmkMTuerIqRW31GyaxmdsXEuiVF7f-LHWL_voczYWAmmiPnhIXp8EANUj7DxB9B1YQ37iKHpsjZJ35_ZnPtjINX68gYcNzbha2cpDoElVf23psigdgOjsawiOe2JBCGrtZCH69qBABIBuv-WF3xpGmeLBCd62XOEUdh7fkBKXNy-ZGJoVulan5KBTpepmVCWb4pnmtUqH5BHgJmjLBTyb2QUFf9QJJhC-JTomHxl237ZBq61MFBuxzIIvm8PchEQGZDp6zEUruhNBX33Gz0yLYobZ1gTeiiIVlJC4SWCKXwuOf3AotklWZuw_6Tm125CWuu--6RuyRFDlu8Y9P6E8aWYP-u973XyQFozaIbD8Y7fBkXD7CcD3SdyZv8_smkSVxkUhg-u_80oTdS157uf4O2UuArk1g0kxyXLCV7UTEzM7TF4HGQXzfGLEelR37cNiyP4bm4L_kdRLOjfYSLFtfjWxBQmJ0ui_LRupN80J5tL2bw6rCMMQx2VTL185hiif2E31DUpN5_dU68fquqvc_kOe9080yr0tN0MsT92t6OXYW09PklUfA6hdTkpHutxeAS7R9PH45_slVwiSkDWT_hKSCkGTXoKO3fHXYN23eBww_DXqmwhwPz-ZwCredI8Q6g0YvF1uGxQeW5PLhao5PjvZ_pFgsXVkfJyYxf9uYTMT_C_tQZc_JccXZjMyDMLND7Teat3ifJmrQDsiqIsb4d1SEjJpDcdR6u4BI8H8KGihoY897iqsKoFD-ggQQF0rcWbjVBXWNLpFqo5tZES8zRZMe-bSrEgSeXfi4cdFjfg6QKeDfvA7XGYTuex4qr49_OAsWYbJ3A18e9Eq5_lL9rSxmyr0cM6i5lud8zzbbXu3ZesssWsKV5qnmrHCrIJD-DtYyTiPB_44qEjkTA0N_-v6qwc-OhyR1k4PMJSBbRwIlzZ3OcKYnhVJzpRTVgaOTjMiYV8Kk47iXxplRdFNjjoRXzgVfQjAhQkbFOk3-7tzPuYaDRLBoWGl0ZibE_dZvbqiH7xqGr0olakMU22EcoxHeCOh2ZX14zmya2Ldu-fcXemQ_u5JvQ8ya2xz12VqkOmLJPtumI6KelqDats7sF03c9bA2J5lr-_dipqIhbZz2IQADuK6CKJRxXTA3Rnc2Yljir2So8sv84lIZauf-fRndMTkF9IE2xfnAOOe8NHLkfLeMtY9it2_YNYYv9hAu0NSLB8b-6civ8mYY5q1iBjl5DinGTulmbonO4q4PcLoujWw11d_-HButmlYfsF_lueheeogWhT4ekKXUOP__JjQmh6OiRHUYpZ2eoyW-pANLWGonGLq41KMGN0odxJokaY_4D7Qj1mUvywIuLM5jiH2XwbxIlwQwbEvQaTe_-E1vhzxMxjwqj5KzstG81VFYZt8Yg4mkUm3eYqFTNnKMYGC3ICEAYETj8QOzQFUBMHA4g9qhBhKrA97JOfh-uIvL44KyJgRsk2Scu-joKWizCm_zaiHrUJe9i1uRRX-KeL416CQS2lLuJVtyxi33WZjSyh0waJrI-bnFrxK705AfOCSWz603lwNz5e2dkiVzu8a1zgUSTnfj3TGMoRBhpBhd7Th9dtAQAFgF20k3FiygR71fqwXIbv/master.m3u8";

    //        //var headers = HeadersModel.Init
    //        //(
    //        //    ("accept", "*/*"),
    //        //    ("accept-language", "ru-RU,ru;q=0.9,uk-UA;q=0.8,uk;q=0.7,en-US;q=0.6,en;q=0.5"),
    //        //    ("accepts-controls", edge_hash),
    //        //    ("authorizations", "Bearer pXzvbyDGLYyB6VkwsWZDv3iMKZtsXNzpzRyxZUcsKHXxsSeaYakbo3hw9mBFRc5VQTpqAX6BW8aDEqyLaHYcXSQiV6KHYTVTK6MYRphNAy5sBjtrevqkDzKmLqNdfMZGEU9NELjmtKfZy3RNGzCd767sNh1mXEj4tCcvqndHtzmwAbZNkhm4ghDEasodotMBewypNQ56uotJAQGX11csfeRfBAPk8DcUWWkkqzxca8vbnEw12vUFbBzT6hz8ZB3F3dzUhUXoL2cr1WM1bXQArRCS1MUNMz3X5WDMMQoZKxj2AMTRqp7QQX4dDB9B7VzEZTmyFULhm1AcHHMkoMvSVvKYoBoAKLycYAgMHeD4ECJcGEAGpnkJhrV57zQ7"),
    //        //    ("cache-control", "no-cache"),
    //        //    ("origin", origin),
    //        //    ("pragma", "no-cache"),
    //        //    ("referer", referer),
    //        //    ("sec-ch-ua", "\"Chromium\";v=\"146\", \"Not-A.Brand\";v=\"24\", \"Google Chrome\";v=\"146\""),
    //        //    ("sec-ch-ua-mobile", "?0"),
    //        //    ("sec-ch-ua-platform", "\"Windows\""),
    //        //    ("sec-fetch-dest", "empty"),
    //        //    ("sec-fetch-mode", "cors"),
    //        //    ("sec-fetch-site", "cross-site"),
    //        //    ("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 Safari/537.36")
    //        //);


    //        //return (m3u, headers);



    //        try
    //        {
    //            using (var browser = new PlaywrightBrowser())
    //            {
    //                var page = await browser.NewPageAsync(init.plugin, proxy: proxy_data).ConfigureAwait(false);
    //                if (page == null)
    //                    return default;

    //                page.WebSocket += (_, ws) =>
    //                {
    //                    //if (ws.Url.Contains("?sid="))
    //                    //{
    //                    //    wsUri = ws.Url;
    //                    //    dfsd(wsUri);
    //                    //}

    //                    ws.FrameReceived += (_, frame) =>
    //                    {
    //                        string hash = Regex.Match(frame.Text, "\"edge_hash\":\"([^\"]+)\"").Groups[1].Value;
    //                        if (!string.IsNullOrEmpty(hash))
    //                        {
    //                            edge_hash = hash;
    //                            Console.WriteLine("edge_hash: " + edge_hash);
    //                        }
    //                    };
    //                };

    //                await page.RouteAsync("**/*", async route =>
    //                {
    //                    try
    //                    {
    //                        if (route.Request.Url.Contains("alloha.tv"))
    //                        {
    //                            await route.FulfillAsync(new RouteFulfillOptions
    //                            {
    //                                Body = PlaywrightBase.IframeHtml(uri)
    //                            });
    //                        }
    //                        else if (route.Request.Url.Contains("/?token_movie="))
    //                        {
    //                            var fetchHeaders = route.Request.Headers;
    //                            fetchHeaders.TryAdd("accept-encoding", "gzip, deflate, br, zstd");
    //                            fetchHeaders.TryAdd("cache-control", "no-cache");
    //                            fetchHeaders.TryAdd("pragma", "no-cache");
    //                            fetchHeaders.TryAdd("sec-fetch-dest", "iframe");
    //                            fetchHeaders.TryAdd("sec-fetch-mode", "navigate");
    //                            fetchHeaders.TryAdd("sec-fetch-site", "cross-site");
    //                            fetchHeaders.TryAdd("sec-fetch-storage-access", "active");

    //                            var fetchResponse = await route.FetchAsync(new RouteFetchOptions
    //                            {
    //                                Url = route.Request.Url,
    //                                Method = "GET",
    //                                Headers = fetchHeaders,
    //                                PostData = route.Request.PostDataBuffer
    //                            }).ConfigureAwait(false);

    //                            string body = await fetchResponse.TextAsync().ConfigureAwait(false);

    //                            var injected = @"
    //                                <script>
    //                                (function() {
    //                                    localStorage.setItem('allplay', '{""captionParam"":{""fontSize"":""100%"",""colorText"":""Белый"",""colorBackground"":""Черный"",""opacityText"":""100%"",""opacityBackground"":""75%"",""styleText"":""Без контура"",""weightText"":""Обычный текст""},""quality"":1080,""volume"":0.5,""muted"":true,""label"":""(Russian) Forced"",""captions"":false}');
    //                                })();
    //                                </script>";

    //                            await route.FulfillAsync(new RouteFulfillOptions
    //                            {
    //                                Status = fetchResponse.Status,
    //                                Body = injected + body,
    //                                Headers = fetchResponse.Headers
    //                            }).ConfigureAwait(false);
    //                        }
    //                        else if (route.Request.Method == "POST" && route.Request.Url.Contains("/movies/"))
    //                        {
    //                            string newUrl = Regex.Replace(route.Request.Url, "/[0-9]+$", $"/{id_file}");

    //                            var fetchHeaders = route.Request.Headers;
    //                            fetchHeaders.TryAdd("accept-encoding", "gzip, deflate, br, zstd");
    //                            fetchHeaders.TryAdd("cache-control", "no-cache");
    //                            fetchHeaders.TryAdd("dnt", "1");
    //                            fetchHeaders.TryAdd("pragma", "no-cache");
    //                            fetchHeaders.TryAdd("priority", "u=1, i");
    //                            fetchHeaders.TryAdd("sec-fetch-dest", "empty");
    //                            fetchHeaders.TryAdd("sec-fetch-mode", "cors");
    //                            fetchHeaders.TryAdd("sec-fetch-site", "same-origin");
    //                            fetchHeaders.TryAdd("sec-fetch-storage-access", "active");

    //                            var fetchResponse = await route.FetchAsync(new RouteFetchOptions
    //                            {
    //                                Url = newUrl,
    //                                Method = "POST",
    //                                Headers = fetchHeaders,
    //                                PostData = route.Request.PostDataBuffer
    //                            }).ConfigureAwait(false);

    //                            string json = await fetchResponse.TextAsync().ConfigureAwait(false);

    //                            await route.FulfillAsync(new RouteFulfillOptions
    //                            {
    //                                Status = fetchResponse.Status,
    //                                Body = json,
    //                                Headers = fetchResponse.Headers
    //                            }).ConfigureAwait(false);

    //                            //string targetStream = null;

    //                            //try
    //                            //{
    //                            //    foreach (var hlsSource in JsonConvert.DeserializeObject<JObject>(json)["hlsSource"])
    //                            //    {
    //                            //        // first or default
    //                            //        if (targetStream == null || hlsSource.Value<bool>("default"))
    //                            //        {
    //                            //            foreach (var q in hlsSource["quality"].ToObject<Dictionary<string, string>>())
    //                            //            {
    //                            //                if ((q.Key is "2160" or "1440") && !init.m4s)
    //                            //                    continue;

    //                            //                targetStream = q.Value;
    //                            //                break;
    //                            //            }
    //                            //        }
    //                            //    }
    //                            //}
    //                            //catch (System.Exception ex)
    //                            //{
    //                            //    Log.Error(ex, "CatchId={CatchId}", "id_353rj03q");
    //                            //}

    //                            //if (string.IsNullOrWhiteSpace(targetStream))
    //                            //{
    //                            //    if (init.m4s)
    //                            //        targetStream = Regex.Match(json, "\"(2160|1440)\":\"([^\"]+)\"").Groups[2].Value;

    //                            //    if (string.IsNullOrWhiteSpace(targetStream))
    //                            //        targetStream = Regex.Match(json, "\"(1080|720)\":\"([^\"]+)\"").Groups[2].Value;
    //                            //}

    //                            //if (!string.IsNullOrWhiteSpace(targetStream))
    //                            //    json = Regex.Replace(json, "\"(2160|1440|1080|720|480|360)\":\"[^\"]+\"", $"\"$1\":\"{targetStream}\"");

    //                            //await route.FulfillAsync(new RouteFulfillOptions
    //                            //{
    //                            //    Status = fetchResponse.Status,
    //                            //    Body = json,
    //                            //    Headers = fetchResponse.Headers
    //                            //}).ConfigureAwait(false);
    //                        }
    //                        else
    //                        {
    //                            if (route.Request.Url.Contains("/stat") || route.Request.Url.Contains("/lists.php"))
    //                            {
    //                                await route.AbortAsync();
    //                                return;
    //                            }

    //                            //if (await PlaywrightBase.AbortOrCache(page, route, abortMedia: true, fullCacheJS: true))
    //                            //    return;

    //                            await route.ContinueAsync();
    //                        }
    //                    }
    //                    catch (System.Exception ex)
    //                    {
    //                        Log.Error(ex, "CatchId={CatchId}", "id_7cd6ocle");
    //                    }
    //                });

    //                page.Response += Page_Response;

    //                PlaywrightBase.GotoAsync(page, "https://alloha.tv/");

    //                //while (edge_hash == null)
    //                //    await Task.Delay(100);

    //                try
    //                {
    //                    return await tcsPageResponse.Task.WaitAsync(TimeSpan.FromSeconds(15));
    //                }
    //                catch (System.Exception ex)
    //                {
    //                    Log.Error(ex, "CatchId={CatchId}", "id_wkfd968c");
    //                }
    //                finally
    //                {
    //                    page.Response -= Page_Response;
    //                }
    //            }
    //        }
    //        catch (System.Exception ex)
    //        {
    //            Log.Error(ex, "CatchId={CatchId}", "id_efmlw02s");
    //        }

    //        return default;
    //    }

    //    TaskCompletionSource<(string hls, List<HeadersModel> headers)> tcsPageResponse = new TaskCompletionSource<(string hls, List<HeadersModel> headers)>();

    //    private void Page_Response(object sender, IResponse e)
    //    {
    //        if (e.Request.Method == "GET" && e.Url.Contains("/master.m3u8"))
    //        {
    //            if (tcsPageResponse.Task.IsCompleted)
    //                return;

    //            var headers = HeadersModel.Init
    //            (
    //                ("cache-control", "no-cache"),
    //                ("pragma", "no-cache"),
    //                ("sec-fetch-dest", "empty"),
    //                ("sec-fetch-mode", "cors"),
    //                ("sec-fetch-site", "cross-site")
    //            );

    //            foreach (var item in e.Request.Headers)
    //            {
    //                headers.Add(new HeadersModel(item.Key, item.Value.ToString()));

    //                //Console.WriteLine( item.Key + " - " + item.Value);

    //                if (item.Key.ToLower() is "referer")
    //                    referer = item.Value;

    //                if (item.Key.ToLower() is "origin")
    //                    origin = item.Value;

    //                //if (item.Key.ToLower() is "host" or "accept-encoding" or "connection" or "range")
    //                //    continue;

    //                //if (!Http.defaultFullHeaders.ContainsKey(item.Key.ToLower()))
    //                //    headers.Add(new HeadersModel(item.Key, item.Value.ToString()));
    //            }

    //            tcsPageResponse.SetResult((e.Url, headers));
    //        }
    //    }
    //    #endregion


    //    #region SpiderSearch
    //    [HttpGet]
    //    [Route("lite/mirage-search")]
    //    async public Task<ActionResult> RouteSpiderSearch(string title, bool origsource = false, bool rjson = false)
    //    {
    //        if (string.IsNullOrWhiteSpace(title))
    //            return OnError();

    //        if (await IsRequestBlocked(rch: false))
    //            return badInitMsg;

    //        var cache = await InvokeCacheResult<JArray>($"mirage:search:{title}", 40, async e =>
    //        {
    //            var root = await httpHydra.Get<JObject>($"{init.apihost}/?token={init.token}&name={HttpUtility.UrlEncode(title)}&list", safety: true);
    //            if (root == null || !root.ContainsKey("data"))
    //                return e.Fail("data");

    //            return e.Success(root["data"].ToObject<JArray>());
    //        });

    //        return ContentTpl(cache, () =>
    //        {
    //            var stpl = new SimilarTpl(cache.Value.Count);

    //            foreach (var j in cache.Value)
    //            {
    //                string uri = $"{host}/lite/mirage?orid={j.Value<string>("token_movie")}";
    //                stpl.Append(j.Value<string>("name") ?? j.Value<string>("original_name"), j.Value<int>("year").ToString(), string.Empty, uri, PosterApi.Size(j.Value<string>("poster")));
    //            }

    //            return stpl;
    //        });
    //    }
    //    #endregion

    //    #region search
    //    async ValueTask<(bool refresh_proxy, int category_id, JToken data)> search(string token_movie, string imdb_id, long kinopoisk_id, string title, int serial, string original_language, int year)
    //    {
    //        string memKey = $"mirage:view:{kinopoisk_id}:{imdb_id}";
    //        if (0 >= kinopoisk_id && string.IsNullOrEmpty(imdb_id))
    //            memKey = $"mirage:viewsearch:{title}:{serial}:{original_language}:{year}";

    //        if (!string.IsNullOrEmpty(token_movie))
    //            memKey = $"mirage:view:{token_movie}";

    //        JObject root;

    //        if (!hybridCache.TryGetValue(memKey, out (int category_id, JToken data) res))
    //        {
    //            string stitle = title.ToLowerAndTrim();

    //            if (memKey.Contains(":viewsearch:"))
    //            {
    //                if (string.IsNullOrWhiteSpace(title) || year == 0)
    //                    return default;

    //                root = await httpHydra.Get<JObject>($"{init.apihost}/?token={init.token}&name={HttpUtility.UrlEncode(title)}&list={(serial == 1 ? "serial" : "movie")}", safety: true);
    //                if (root == null)
    //                    return (true, 0, null);

    //                if (root.ContainsKey("data"))
    //                {
    //                    foreach (var item in root["data"])
    //                    {
    //                        if (item.Value<string>("name")?.ToLowerAndTrim() == stitle)
    //                        {
    //                            int y = item.Value<int>("year");
    //                            if (y > 0 && (y == year || y == (year - 1) || y == (year + 1)))
    //                            {
    //                                if (original_language == "ru" && item.Value<string>("country")?.ToLowerAndTrim() != "россия")
    //                                    continue;

    //                                res.data = item;
    //                                res.category_id = item.Value<int>("category_id");
    //                                break;
    //                            }
    //                        }
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                root = await httpHydra.Get<JObject>($"{init.apihost}/?token={init.token}&kp={kinopoisk_id}&imdb={imdb_id}&token_movie={token_movie}", safety: true);
    //                if (root == null)
    //                    return (true, 0, null);

    //                if (root.ContainsKey("data"))
    //                {
    //                    res.data = root.GetValue("data");
    //                    res.category_id = res.data.Value<int>("category");
    //                }
    //            }

    //            if (res.data != null || (root.ContainsKey("error_info") && root.Value<string>("error_info") == "not movie"))
    //                hybridCache.Set(memKey, res, cacheTime(res.category_id is 1 or 3 ? 120 : 40));
    //            else
    //                hybridCache.Set(memKey, res, cacheTime(2));
    //        }

    //        return (false, res.category_id, res.data);
    //    }
    //    #endregion


    //    static ClientWebSocket ws = null;

    //    async static void dfsd(string wsUri)
    //    {
    //        if (ws != null)
    //            return;

    //        var uri = new Uri(wsUri);

    //        ws = new ClientWebSocket();
    //        var cts = new CancellationTokenSource();

    //        Console.WriteLine(wsUri);
    //        await ws.ConnectAsync(uri, cts.Token);
    //        Console.WriteLine("\nConnected!");

    //        _ = Task.Run(async () =>
    //        {
    //            var buffer = new byte[16 * 1024];

    //            while (ws.State == WebSocketState.Open)
    //            {
    //                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);

    //                if (result.MessageType == WebSocketMessageType.Close)
    //                {
    //                    Console.WriteLine("Connection closed by server");
    //                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
    //                    break;
    //                }

    //                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

    //                Console.WriteLine("\nResive message");
    //                Console.WriteLine(message);

    //                string hash = Regex.Match(message, "\"edge_hash\":\"([^\"]+)\"").Groups[1].Value;
    //                if (!string.IsNullOrEmpty(hash))
    //                {
    //                    edge_hash = hash;
    //                    Console.WriteLine("\n\tedge_hash: " + edge_hash);
    //                }

    //                Console.WriteLine("\n");
    //            }
    //        });

    //        _ = Task.Run(async () =>
    //        {
    //            int sd = 0;
    //            while (!cts.Token.IsCancellationRequested && ws.State == WebSocketState.Open)
    //            {
    //                await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
    //                if (sd == current_time)
    //                    continue;

    //                sd = current_time;

    //                string payload = "{\"type\":\"playing\",\"current_time\":" + current_time + ",\"resolution\":\"1080\",\"track_id\":\"1\",\"speed\":1,\"subtitle\":-1,\"ts\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}";

    //                await ws.SendAsync(
    //                  new ArraySegment<byte>(Encoding.UTF8.GetBytes(payload)),
    //                  WebSocketMessageType.Text,
    //                  true,
    //                  cts.Token
    //                );

    //                Console.WriteLine($"current_time: {current_time}");
    //            }
    //        });


    //        Task SendAsync(string payload)
    //        {
    //            return ws.SendAsync(
    //              new ArraySegment<byte>(Encoding.UTF8.GetBytes(payload)),
    //              WebSocketMessageType.Text,
    //              true,
    //              cts.Token
    //            );
    //        }

    //        var sdfsd = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    //        await SendAsync("{\"type\":\"playback_start\",\"current_time\":0,\"resolution\":\"1080\",\"track_id\":\"1\",\"speed\":1,\"subtitle\":-1,\"ts\":" + sdfsd + "}");
    //        await SendAsync("{\"type\":\"init\",\"current_time\":0,\"resolution\":\"1080\",\"track_id\":\"1\",\"speed\":1,\"subtitle\":-1,\"ts\":" + sdfsd + "}");

    //        await Task.Delay(1000);

    //        await SendAsync("{\"type\":\"playback_start\",\"current_time\":0,\"resolution\":\"1080\",\"track_id\":\"1\",\"speed\":1,\"subtitle\":-1,\"ts\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}");
    //        await SendAsync("{\"type\":\"resumed\",\"current_time\":0,\"resolution\":\"1080\",\"track_id\":\"1\",\"speed\":1,\"subtitle\":0,\"ts\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}");
    //        await SendAsync("{\"type\":\"paused\",\"current_time\":0,\"resolution\":\"1080\",\"track_id\":\"1\",\"speed\":1,\"subtitle\":0,\"ts\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}");
    //        await SendAsync("{\"type\":\"resumed\",\"current_time\":0,\"resolution\":\"1080\",\"track_id\":\"1\",\"speed\":1,\"subtitle\":0,\"ts\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}");

    //    }


    //    // static string[] reffers = new string[] { "1400-princessa-i-tajna-goblinov-2024.html", "1400-princessa-i-tajna-goblinov-2024.html", "1400-princessa-i-tajna-goblinov-2024.html", "1400-princessa-i-tajna-goblinov-2024.html", "1400-princessa-i-tajna-goblinov-2024.html", "1400-princessa-i-tajna-goblinov-2024.html", "1400-princessa-i-tajna-goblinov-2024.html", "1400-princessa-i-tajna-goblinov-2024.html", "1400-princessa-i-tajna-goblinov-2024.html", "1400-princessa-i-tajna-goblinov-2024.html", "1400-princessa-i-tajna-goblinov-2024.html", "408-legenda-o-chernom-dereve-2024.html", "408-legenda-o-chernom-dereve-2024.html", "408-legenda-o-chernom-dereve-2024.html", "408-legenda-o-chernom-dereve-2024.html", "408-legenda-o-chernom-dereve-2024.html", "408-legenda-o-chernom-dereve-2024.html", "408-legenda-o-chernom-dereve-2024.html", "408-legenda-o-chernom-dereve-2024.html", "408-legenda-o-chernom-dereve-2024.html", "408-legenda-o-chernom-dereve-2024.html", "408-legenda-o-chernom-dereve-2024.html", "1221-magazin-svetilnikov-2024.html", "1221-magazin-svetilnikov-2024.html", "1221-magazin-svetilnikov-2024.html", "1221-magazin-svetilnikov-2024.html", "1221-magazin-svetilnikov-2024.html", "1221-magazin-svetilnikov-2024.html", "1221-magazin-svetilnikov-2024.html", "1221-magazin-svetilnikov-2024.html", "1221-magazin-svetilnikov-2024.html", "1221-magazin-svetilnikov-2024.html", "1221-magazin-svetilnikov-2024.html", "1112-vspylchivyj-svjaschennik-2024.html", "1112-vspylchivyj-svjaschennik-2024.html", "1112-vspylchivyj-svjaschennik-2024.html", "1112-vspylchivyj-svjaschennik-2024.html", "1112-vspylchivyj-svjaschennik-2024.html", "1112-vspylchivyj-svjaschennik-2024.html", "1112-vspylchivyj-svjaschennik-2024.html", "1112-vspylchivyj-svjaschennik-2024.html", "1112-vspylchivyj-svjaschennik-2024.html", "1112-vspylchivyj-svjaschennik-2024.html", "1112-vspylchivyj-svjaschennik-2024.html", "1239-forsazh-polnyj-vpered-2024.html", "1239-forsazh-polnyj-vpered-2024.html", "1239-forsazh-polnyj-vpered-2024.html", "1239-forsazh-polnyj-vpered-2024.html", "1239-forsazh-polnyj-vpered-2024.html", "1239-forsazh-polnyj-vpered-2024.html", "1239-forsazh-polnyj-vpered-2024.html", "1239-forsazh-polnyj-vpered-2024.html", "1239-forsazh-polnyj-vpered-2024.html", "1239-forsazh-polnyj-vpered-2024.html", "1239-forsazh-polnyj-vpered-2024.html", "1230-chelovek-vnutri-2024.html", "1230-chelovek-vnutri-2024.html", "1230-chelovek-vnutri-2024.html", "1230-chelovek-vnutri-2024.html", "1230-chelovek-vnutri-2024.html", "1230-chelovek-vnutri-2024.html", "1230-chelovek-vnutri-2024.html", "1230-chelovek-vnutri-2024.html", "1230-chelovek-vnutri-2024.html", "1230-chelovek-vnutri-2024.html", "1230-chelovek-vnutri-2024.html", "1214-moj-marchello-2024.html", "1214-moj-marchello-2024.html", "1214-moj-marchello-2024.html", "1214-moj-marchello-2024.html", "1214-moj-marchello-2024.html", "1214-moj-marchello-2024.html", "1214-moj-marchello-2024.html", "1214-moj-marchello-2024.html", "1214-moj-marchello-2024.html", "1214-moj-marchello-2024.html", "1214-moj-marchello-2024.html", "1200-reinkarnacija-vozvraschenie-vedmy-2024.html", "1200-reinkarnacija-vozvraschenie-vedmy-2024.html", "1200-reinkarnacija-vozvraschenie-vedmy-2024.html", "1200-reinkarnacija-vozvraschenie-vedmy-2024.html", "1200-reinkarnacija-vozvraschenie-vedmy-2024.html", "1200-reinkarnacija-vozvraschenie-vedmy-2024.html", "1200-reinkarnacija-vozvraschenie-vedmy-2024.html", "1200-reinkarnacija-vozvraschenie-vedmy-2024.html", "1200-reinkarnacija-vozvraschenie-vedmy-2024.html", "1200-reinkarnacija-vozvraschenie-vedmy-2024.html", "1200-reinkarnacija-vozvraschenie-vedmy-2024.html", "1185-ne-hochu-nichego-terjat-2024.html", "1185-ne-hochu-nichego-terjat-2024.html", "1185-ne-hochu-nichego-terjat-2024.html", "1185-ne-hochu-nichego-terjat-2024.html", "1185-ne-hochu-nichego-terjat-2024.html", "1185-ne-hochu-nichego-terjat-2024.html", "1185-ne-hochu-nichego-terjat-2024.html", "1185-ne-hochu-nichego-terjat-2024.html", "1185-ne-hochu-nichego-terjat-2024.html", "1185-ne-hochu-nichego-terjat-2024.html", "1185-ne-hochu-nichego-terjat-2024.html", "1168-astral-koshmar-v-spring-garden-2024.html", "1168-astral-koshmar-v-spring-garden-2024.html", "1168-astral-koshmar-v-spring-garden-2024.html", "1168-astral-koshmar-v-spring-garden-2024.html", "1168-astral-koshmar-v-spring-garden-2024.html", "1168-astral-koshmar-v-spring-garden-2024.html", "1168-astral-koshmar-v-spring-garden-2024.html", "1168-astral-koshmar-v-spring-garden-2024.html", "1168-astral-koshmar-v-spring-garden-2024.html", "1168-astral-koshmar-v-spring-garden-2024.html", "1168-astral-koshmar-v-spring-garden-2024.html", "1179-komandante-2024.html", "1179-komandante-2024.html", "1179-komandante-2024.html", "1179-komandante-2024.html", "1179-komandante-2024.html", "1179-komandante-2024.html", "1179-komandante-2024.html", "1179-komandante-2024.html", "1179-komandante-2024.html", "1179-komandante-2024.html", "1179-komandante-2024.html", "1157-bolshoe-prikljuchenie-2024.html", "1157-bolshoe-prikljuchenie-2024.html", "1157-bolshoe-prikljuchenie-2024.html", "1157-bolshoe-prikljuchenie-2024.html", "1157-bolshoe-prikljuchenie-2024.html", "1157-bolshoe-prikljuchenie-2024.html", "1157-bolshoe-prikljuchenie-2024.html", "1157-bolshoe-prikljuchenie-2024.html", "1157-bolshoe-prikljuchenie-2024.html", "1157-bolshoe-prikljuchenie-2024.html", "1157-bolshoe-prikljuchenie-2024.html", "1143-kak-stat-korolem-2024.html", "1143-kak-stat-korolem-2024.html", "1143-kak-stat-korolem-2024.html", "1143-kak-stat-korolem-2024.html", "1143-kak-stat-korolem-2024.html", "1143-kak-stat-korolem-2024.html", "1143-kak-stat-korolem-2024.html", "1143-kak-stat-korolem-2024.html", "1143-kak-stat-korolem-2024.html", "1143-kak-stat-korolem-2024.html", "1143-kak-stat-korolem-2024.html", "944-pingvin-2024.html", "944-pingвин-2024.html" };
    //}
}
