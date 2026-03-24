using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Online.Models.VeoVeo;
using Shared.Attributes;
using System.Net.Http;

namespace Online.Controllers
{
    public class VeoVeo : BaseOnlineController
    {
        public static List<Movie> database;
        static readonly HttpClient http2Client = FriendlyHttp.CreateHttp2Client();

        public VeoVeo() : base(ModInit.siteConf.VeoVeo) { }

        [HttpGet]
        [Staticache(1)]
        [Route("lite/veoveo")]
        async public Task<ActionResult> Index(long movieid, string imdb_id, long kinopoisk_id, string title, string original_title, int clarification, int s = -1, bool rjson = false, bool similar = false)
        {
            if (await IsRequestBlocked(rch: true, rch_check: !similar))
                return badInitMsg;

            if (movieid == 0)
            {
                if (similar)
                    return await Spider(title);

                var movie = search(imdb_id, kinopoisk_id, title, original_title);
                if (movie == null)
                    return await Spider(clarification == 1 ? title : (original_title ?? title));

                movieid = movie.id;
            }

        #region media
        rhubFallback:
            var cache = await InvokeCacheResult<JArray>($"{init.plugin}:view:{movieid}", 20, async e =>
            {
                if (init.httpversion == 2)
                    httpHydra.RegisterHttp(http2Client);

                var root = await httpHydra.Get<JArray>($"{init.host}/balancer-api/proxy/playlists/catalog-api/episodes?content-id={movieid}");

                if (root == null || root.Count == 0)
                    return e.Fail("data");

                return e.Success(root);
            });

            if (IsRhubFallback(cache))
                goto rhubFallback;
            #endregion

            return ContentTpl(cache, () =>
            {
                if (cache.Value.First["season"].Value<int>("order") == 0)
                {
                    #region Фильм
                    var mtpl = new MovieTpl(title, original_title, 1);

                    var first = cache.Value?.FirstOrDefault();
                    if (first != null)
                    {
                        var episodes = first["episodeVariants"];
                        if (episodes != null)
                        {
                            foreach (var episode in episodes)
                            {
                                string file = episode?.Value<string>("filepath");
                                if (!string.IsNullOrWhiteSpace(file))
                                {
                                    string stream = file.Contains(".json")
                                        ? accsArgs($"{host}/lite/veoveo/parsed.m3u8?link={EncryptQuery(file)}")
                                        : HostStreamProxy(file);

                                    mtpl.Append(episode.Value<string>("title") ?? "1080p", stream, vast: init.vast);
                                }
                            }
                        }
                    }

                    return mtpl;
                    #endregion
                }
                else
                {
                    #region Сериал
                    if (s == -1)
                    {
                        var tpl = new SeasonTpl();
                        var hash = new HashSet<int>();

                        foreach (var item in cache.Value)
                        {
                            var season = item["season"].Value<int>("order");
                            if (hash.Contains(season))
                                continue;

                            hash.Add(season);
                            string link = $"{host}/lite/veoveo?rjson={rjson}&movieid={movieid}&kinopoisk_id={kinopoisk_id}&imdb_id={imdb_id}&title={HttpUtility.UrlEncode(title)}&original_title={HttpUtility.UrlEncode(original_title)}&s={season}";
                            tpl.Append($"{season} сезон", link, season);
                        }

                        return tpl;
                    }
                    else
                    {
                        var episodes = cache.Value.Where(i => i["season"].Value<int>("order") == s);

                        var etpl = new EpisodeTpl(episodes.Count());
                        string sArhc = s.ToString();

                        foreach (var episode in episodes.OrderBy(i => i.Value<int>("order")))
                        {
                            string name = episode.Value<string>("title");

                            var first = cache.Value?.FirstOrDefault();
                            if (first != null)
                            {
                                var variants = first["episodeVariants"];
                                var fileToken = variants?
                                    .OrderByDescending(i => (i.Value<string>("filepath") ?? "").Contains(".m3u8"))
                                    .FirstOrDefault();

                                string file = fileToken?.Value<string>("filepath");
                                if (!string.IsNullOrWhiteSpace(file))
                                {
                                    string stream = HostStreamProxy(file);
                                    if (stream.Contains(".json"))
                                        stream = accsArgs($"{host}/lite/veoveo/parsed.m3u8?link={EncryptQuery(file)}");

                                    etpl.Append(name ?? $"{episode.Value<int>("order")} серия", title ?? original_title, sArhc, episode.Value<int>("order").ToString(), stream, vast: init.vast);
                                }
                            }
                        }

                        return etpl;
                    }
                    #endregion
                }
            });
        }

        #region Parsed
        [HttpGet]
        [Route("lite/veoveo/parsed.m3u8")]
        async public Task<ActionResult> Parsed(string link)
        {
            link = DecryptQuery(link);
            if (string.IsNullOrWhiteSpace(link))
                return OnError();

            string m3u8 = await InvokeCache($"veoveo:parsed:{link}", 20, async () =>
            {
                var parsed = await httpHydra.Get<JObject>(link);
                if (parsed != null && parsed.ContainsKey("sources"))
                {
                    var sources = parsed["sources"] as JArray;
                    if (sources != null && sources.Count > 0)
                    {
                        string m3u8 = sources.First.Value<string>("link");
                        if (!string.IsNullOrEmpty(m3u8))
                            return m3u8;
                    }
                }

                return null;
            });

            if (!string.IsNullOrEmpty(m3u8))
                return Redirect(HostStreamProxy(m3u8));

            return OnError();
        }
        #endregion

        #region Spider
        [HttpGet]
        [Route("lite/veoveo-spider")]
        async public Task<ActionResult> Spider(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return OnError();

            var stpl = new SimilarTpl(100);
            string _t = StringConvert.SearchName(title);
            if (string.IsNullOrEmpty(_t))
                return OnError();

            foreach (var m in database)
            {
                if (stpl.data.Count >= 100)
                    break;

                if (StringConvert.SearchName(m.title, string.Empty).Contains(_t) || StringConvert.SearchName(m.originalTitle, string.Empty).Contains(_t))
                {
                    string uri = $"{host}/lite/veoveo?movieid={m.id}";
                    stpl.Append(m.title ?? m.originalTitle, m.year.ToString(), string.Empty, uri, PosterApi.Find(m.kinopoiskId, m.imdbId));
                }
            }

            return ContentTpl(stpl);
        }
        #endregion


        #region search
        Movie search(string imdb_id, long kinopoisk_id, string title, string original_title)
        {
            string stitle = StringConvert.SearchName(title);
            string sorigtitle = StringConvert.SearchName(original_title);

            Movie goSearch(bool searchToId)
            {
                if (searchToId && kinopoisk_id == 0 && string.IsNullOrEmpty(imdb_id))
                    return null;

                foreach (var item in database)
                {
                    if (searchToId)
                    {
                        if (kinopoisk_id > 0)
                        {
                            if (item.kinopoiskId == kinopoisk_id)
                                return item;
                        }

                        if (!string.IsNullOrEmpty(imdb_id))
                        {
                            if (item.imdbId == imdb_id)
                                return item;
                        }
                    }
                    else
                    {
                        if (sorigtitle != null)
                        {
                            if (StringConvert.SearchName(item.originalTitle) == sorigtitle)
                                return item;
                        }

                        if (stitle != null)
                        {
                            if (StringConvert.SearchName(item.title) == stitle)
                                return item;
                        }
                    }
                }

                return null;
            }

            return goSearch(true) ?? goSearch(false);
        }
        #endregion
    }
}
