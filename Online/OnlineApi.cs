using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared.Models.Events;
using Shared.Models.Module;
using Shared.Models.Module.Entrys;
using Online.SQL;
using Shared.PlaywrightCore;
using System.Data;
using System.Text;
using IO = System.IO;
using Shared.Services.RxEnumerate;

namespace Online.Controllers
{
    public class OnlineApiController : BaseController
    {
        record EventLinkItem(string code, int index, bool work);

        #region online.js
        [HttpGet]
        [AllowAnonymous]
        [Route("online.js")]
        [Route("online/js/{token}")]
        public ContentResult Online(string token)
        {
            SetHeadersNoCache();

            var init = ModInit.conf;
            var apr = init.appReplace;

            string memKey = $"online.js:{apr?.Count ?? 0}:{init.version}:{init.description}:{init.apn}:{host}:{init.spider}:{init.component}:{init.name}:{init.spiderName}";
            if (!memoryCache.TryGetValue(memKey, out (string file, string filecleaer) cache))
            {
                cache.file = FileCache.ReadAllText($"{ModInit.modpath}/plugin.js", "online.js", false)
                    .Replace("{rch_websoket}", FileCache.ReadAllText("plugins/rch_nws.js", "rch_nws.js", false));

                #region appReplace
                if (apr != null)
                {
                    foreach (var r in apr)
                    {
                        string val = r.Value;
                        if (val.StartsWith("file:"))
                            val = IO.File.ReadAllText(val.Substring(5));

                        cache.file = Regex.Replace(cache.file, r.Key, val, RegexOptions.IgnoreCase);
                    }
                }
                #endregion

                if (!init.version)
                {
                    cache.file = Regex.Replace(cache.file, "version: \\'[^\\']+\\'", "version: ''")
                                      .Replace("manifst.name, \" v\"", "manifst.name, \" \"");
                }

                if (init.description != "Плагин для просмотра онлайн сериалов и фильмов")
                    cache.file = Regex.Replace(cache.file, "description: \\'([^\\']+)?\\'", $"description: '{init.description}'");

                if (init.apn != null)
                    cache.file = Regex.Replace(cache.file, "apn: \\'([^\\']+)?\\'", $"apn: '{init.apn}'");

                var bulder = new StringBuilder(cache.file);

                if (!init.spider)
                {
                    bulder = bulder.Replace("addSourceSearch('Spider', 'spider');", "")
                                   .Replace("addSourceSearch('Anime', 'spider/anime');", "");
                }

                if (init.component != "lampac")
                {
                    bulder = bulder.Replace("component: 'lampac'", $"component: '{init.component}'")
                                   .Replace("'lampac', component", $"'{init.component}', component")
                                   .Replace("window.lampac_plugin", $"window.{init.component}_plugin");
                }

                if (init.name != "Lampac")
                    bulder = bulder.Replace("name: 'Lampac'", $"name: '{init.name}'");

                if (CoreInit.conf.kit.aesgcmkeyName != null)
                    bulder = bulder.Replace("aesgcmkey", CoreInit.conf.kit.aesgcmkeyName);

                if (init.spiderName != "Spider")
                {
                    bulder = bulder.Replace("addSourceSearch('Spider'", $"addSourceSearch('{init.spiderName}'")
                                   .Replace("addSourceSearch('Anime'", $"addSourceSearch('{init.spiderName} - Anime'");
                }

                bulder = bulder
                    .Replace("{invc-rch}", FileCache.ReadAllText("plugins/invc-rch.js", "invc-rch.js", false))
                    .Replace("{invc-rch_nws}", FileCache.ReadAllText("plugins/invc-rch_nws.js", "invc-rch_nws.js", false))
                    .Replace("{player-inner}", string.Empty)
                    .Replace("{localhost}", host);

                cache.file = bulder.ToString();
                cache.filecleaer = cache.file.Replace("{token}", string.Empty);

                memoryCache.Set(memKey, cache, DateTime.Now.AddMinutes(10));
            }

            if (EventListener.AppReplace != null)
            {
                string source = EventListener.AppReplace.Invoke("online", new EventAppReplace(cache.file, token, null, host, requestInfo, HttpContext.Request, hybridCache));
                return Content(source.Replace("{token}", HttpUtility.UrlEncode(token)), "application/javascript; charset=utf-8");
            }

            return Content(token != null ? cache.file.Replace("{token}", HttpUtility.UrlEncode(token)) : cache.filecleaer, "application/javascript; charset=utf-8");
        }
        #endregion


        #region externalids
        /// <summary>
        /// imdb_id, kinopoisk_id
        /// </summary>
        static ConcurrentDictionary<string, string> externalids = null;

        static DateTime externalids_lastWriteTime = default, externalids_nextCheck = default;

        [HttpGet]
        [Route("externalids")]
        async public Task<ActionResult> Externalids(string id, string imdb_id, long kinopoisk_id, int serial)
        {
            #region cache
            string memKey = $"OnlineApi:externalids:{id}:{imdb_id}:{kinopoisk_id}:{serial}";
            if (memoryCache.TryGetValue(memKey, out string jsonResult))
                return Content(jsonResult, "application/json; charset=utf-8");
            #endregion

            #region externalids
            if (externalids == null)
            {
                externalids = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(IO.File.ReadAllText("data/externalids.json"));

                if (Lumex.database != null)
                {
                    foreach (var item in Lumex.database)
                    {
                        if (!string.IsNullOrEmpty(item.imdb_id) && item.kinopoisk_id > 0)
                            externalids.AddOrUpdate(item.imdb_id, item.kinopoisk_id.ToString(), (k, v) => item.kinopoisk_id.ToString());
                    }
                }
            }
            #endregion

            #region KP_
            if (id != null && id.StartsWith("KP_"))
            {
                string _kp = id.Substring(0, 3);
                foreach (var eid in externalids)
                {
                    if (eid.Value == _kp && !string.IsNullOrEmpty(eid.Key))
                    {
                        imdb_id = eid.Key;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(imdb_id))
                {
                    return Json(new { imdb_id, kinopoisk_id = _kp });
                }
                else
                {
                    string mkey = $"externalids:KP_:{_kp}";
                    if (!hybridCache.TryGetValue(mkey, out string _imdbid))
                    {
                        var alloha = ModInit.premiumConf.Alloha;
                        var proxyManager = new ProxyManager("alloha", alloha);

                        string json = await Http.Get($"{alloha.apihost}/?token={alloha.token ?? "04941a9a3ca3ac16e2b4327347bbc1"}&kp=" + _kp, timeoutSeconds: 5, proxy: proxyManager.Get());
                        _imdbid = Regex.Match(json ?? "", "\"id_imdb\":\"(tt[^\"]+)\"").Groups[1].Value;
                        hybridCache.Set(mkey, _imdbid, DateTime.Now.AddHours(8));
                    }

                    return Json(new { imdb_id = _imdbid, kinopoisk_id = _kp });
                }
            }
            #endregion

            #region getAlloha / getVSDN / getTabus
            async Task<string> getAlloha(string imdb)
            {
                string kpid = null;
                var alloha = ModInit.premiumConf.Alloha;
                var proxyManager = new ProxyManager("alloha", alloha);

                await Http.GetSpan($"{alloha.apihost}/?token={alloha.token ?? "04941a9a3ca3ac16e2b4327347bbc1"}&imdb={imdb}", timeoutSeconds: 5, proxy: proxyManager.Get(), spanAction: json =>
                {
                    kpid = Rx.Match(json, "\"id_kp\":([0-9]+),");
                });

                if (!string.IsNullOrEmpty(kpid) && kpid != "0" && kpid != "null")
                    return kpid;

                return null;
            }

            async Task<string> getTabus(string imdb)
            {
                string kpid = null;
                var proxyManager = new ProxyManager("collaps", ModInit.siteConf.Collaps);

                await Http.GetSpan("https://api.bhcesh.me/franchise/details?token=d39edcf2b6219b6421bffe15dde9f1b3&imdb_id=" + imdb.Remove(0, 2), timeoutSeconds: 5, proxy: proxyManager.Get(), spanAction: json =>
                {
                    kpid = Rx.Match(json, "\"kinopoisk_id\":\"?([0-9]+)\"?");
                });

                if (!string.IsNullOrEmpty(kpid) && kpid != "0" && kpid != "null")
                    return kpid;

                return null;
            }

            //async Task<string> getVSDN(string imdb)
            //{
            //    //long? res = Lumex.database.FirstOrDefault(i => i.imdb_id == imdb)?.kinopoisk_id;
            //    //if (res > 0)
            //    //    return res.ToString();

            //    if (string.IsNullOrEmpty(ModInit.siteConf.VideoCDN.token) || string.IsNullOrEmpty(ModInit.siteConf.VideoCDN.iframehost))
            //        return null;

            //    ProxyManager proxyManager = ModInit.siteConf.VideoCDN.useproxy
            //        ? new ProxyManager("videocdn", ModInit.siteConf.VideoCDN)
            //        : null;

            //    string kpid = null;

            //    await Http.GetSpan($"{ModInit.siteConf.VideoCDN.iframehost}/api/short?api_token={ModInit.siteConf.VideoCDN.token}&imdb_id={imdb}", json =>
            //    {
            //        string kp = Rx.Groups(json, "\"kp_id\":\"?([0-9]+)\"?")[1].Value;
            //        if (!string.IsNullOrEmpty(kpid) && kpid != "0" && kpid != "null")
            //            kpid = kp;

            //    }, timeoutSeconds: 10, proxy: proxyManager?.Get());

            //    return kpid;
            //}
            #endregion

            #region get imdb_id
            if (string.IsNullOrWhiteSpace(imdb_id))
            {
                if (kinopoisk_id > 0)
                {
                    string kinopoisk_id_str = kinopoisk_id.ToString();
                    foreach (var eid in externalids)
                    {
                        if (eid.Value == kinopoisk_id_str && !string.IsNullOrEmpty(eid.Key))
                        {
                            imdb_id = eid.Key;
                            break;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(imdb_id) && long.TryParse(id, out long _testid) && _testid > 0)
                {
                    await using (var sqlDb = ExternalidsContext.Factory != null
                        ? ExternalidsContext.Factory.CreateDbContext()
                        : new ExternalidsContext())
                    {
                        imdb_id = sqlDb.imdb.Find($"{id}_{serial}")?.value;
                    }

                    if (string.IsNullOrEmpty(imdb_id))
                    {
                        string mkey = $"externalids:locktmdb:{serial}:{id}";
                        if (!memoryCache.TryGetValue(mkey, out _))
                        {
                            memoryCache.Set(mkey, 0, DateTime.Now.AddHours(1));

                            string cat = serial == 1 ? "tv" : "movie";
                            var header = HeadersModel.Init(("lcrqpasswd", CoreInit.rootPasswd));
                            string json = await Http.Get($"http://api.themoviedb.org/3/{cat}/{id}?api_key={CoreInit.conf.cub.api_key}&append_to_response=external_ids", timeoutSeconds: 5, headers: header);
                            if (!string.IsNullOrWhiteSpace(json))
                            {
                                imdb_id = Regex.Match(json, "\"imdb_id\":\"(tt[0-9]+)\"").Groups[1].Value;
                                if (!string.IsNullOrEmpty(imdb_id))
                                {
                                    await using (var sqlDb = ExternalidsContext.Factory != null
                                        ? ExternalidsContext.Factory.CreateDbContext()
                                        : new ExternalidsContext())
                                    {
                                        sqlDb.Add(new ExternalidsSqlModel()
                                        {
                                            Id = $"{id}_{serial}",
                                            value = imdb_id
                                        });

                                        await sqlDb.SaveChangesLocks();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region get kinopoisk_id
            string kpid = null;

            if (!string.IsNullOrWhiteSpace(imdb_id))
            {
                externalids.TryGetValue(imdb_id, out kpid);

                if (string.IsNullOrEmpty(kpid) || kpid == "0")
                {
                    await using (var sqlDb = ExternalidsContext.Factory != null
                        ? ExternalidsContext.Factory.CreateDbContext()
                        : new ExternalidsContext())
                    {
                        kpid = sqlDb.kinopoisk.Find(imdb_id)?.value;

                        if (string.IsNullOrEmpty(kpid) && kinopoisk_id == 0)
                        {
                            string mkey = $"externalids:lockkpid:{imdb_id}";
                            if (!memoryCache.TryGetValue(mkey, out _))
                            {
                                memoryCache.Set(mkey, 0, DateTime.Now.AddDays(1));

                                switch (ModInit.conf.findkp ?? "all")
                                {
                                    case "alloha":
                                        kpid = await getAlloha(imdb_id);
                                        break;
                                    //case "vsdn":
                                    //    kpid = await getVSDN(imdb_id);
                                    //    break;
                                    case "tabus":
                                        kpid = await getTabus(imdb_id);
                                        break;
                                    default:
                                        {
                                            var tasks = new List<Task<string>> { /*getVSDN(imdb_id),*/ getAlloha(imdb_id), getTabus(imdb_id) };

                                            while (tasks.Count > 0)
                                            {
                                                var completedTask = await Task.WhenAny(tasks);
                                                tasks.Remove(completedTask);

                                                var result = completedTask.Result;
                                                if (result != null)
                                                {
                                                    kpid = result;
                                                    break;
                                                }
                                            }

                                            break;
                                        }
                                }

                                if (!string.IsNullOrEmpty(kpid) && kpid != "0")
                                {
                                    sqlDb.Add(new ExternalidsSqlModel()
                                    {
                                        Id = imdb_id,
                                        value = kpid
                                    });

                                    await sqlDb.SaveChangesLocks();
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            kpid = kpid != null ? kpid : kinopoisk_id.ToString();

            if (EventListener.Externalids != null)
            {
                var result = EventListener.Externalids.Invoke(new EventExternalids(id, imdb_id, kpid, serial));

                if (result.imdb_id != null || result.kinopoisk_id != null)
                {
                    imdb_id = result.imdb_id;
                    kpid = result.kinopoisk_id;
                }
            }

            jsonResult = $"{{\"imdb_id\":\"{imdb_id}\",\"kinopoisk_id\":\"{kpid}\"}}";
            memoryCache.Set(memKey, jsonResult, DateTime.Now.AddHours(1));

            return Content(jsonResult, "application/json; charset=utf-8");
        }
        #endregion

        #region WithSearch
        [HttpGet]
        [AllowAnonymous]
        [Route("lite/withsearch")]
        public ActionResult WithSearch()
        {
            if (ModInit.conf.with_search == null)
                return ContentTo("[]");

            return Json(ModInit.conf.with_search);
        }
        #endregion

        #region spider
        [HttpGet]
        [Route("lite/spider")]
        [Route("lite/spider/anime")]
        async public Task<ActionResult> Spider(string title)
        {
            if (!ModInit.conf.spider)
                return ContentTo("{}");

            var rch = new RchClient(HttpContext, host, new BaseSettings() { rhub = true }, requestInfo);
            if (rch.IsNotConnected() || rch.IsRequiredConnected())
                return ContentTo(rch.connectionMsg);

            var user = requestInfo.user;
            var piders = new List<(string name, string uri, int index)>();

            bool isanime = HttpContext.Request.Path.Value?.EndsWith("/anime") == true;

            #region module
            OnlineModuleEntry.EnsureCache();
            var spiderArgs = new OnlineSpiderModel(title, isanime);

            if (OnlineModuleEntry.onlineModulesCache != null && OnlineModuleEntry.onlineModulesCache.Count > 0)
            {
                void addResult(List<ModuleOnlineSpiderItem> result)
                {
                    if (result == null || result.Count == 0)
                        return;

                    foreach (var item in result)
                    {
                        if (string.IsNullOrEmpty(item.name) || string.IsNullOrEmpty(item.url))
                            continue;

                        piders.Add((item.name, item.url, item.index));
                    }
                }

                foreach (var entry in OnlineModuleEntry.onlineModulesCache)
                {
                    try
                    {
                        var result = entry.Spider(HttpContext, memoryCache, requestInfo, host, spiderArgs);
                        if (result != null && result.Count > 0)
                            addResult(result);

                        result = await entry.SpiderAsync(HttpContext, memoryCache, requestInfo, host, spiderArgs);
                        if (result != null && result.Count > 0)
                            addResult(result);
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, "CatchId={CatchId}", "id_bd1de14c");
                    }
                }
            }
            #endregion

            #region send
            void send(BaseSettings init, string plugin = null)
            {
                if (!init.spider || !init.enable || init.rip)
                    return;

                if (init.geo_hide != null)
                {
                    if (requestInfo.Country != null && init.geo_hide.Contains(requestInfo.Country))
                        return;
                }

                if (init.group_hide)
                {
                    if (init.group > 0)
                    {
                        if (user == null || init.group > user.group)
                            return;
                    }
                    else if (CoreInit.conf.accsdb.enable)
                    {
                        if (user == null)
                            return;
                    }
                }

                string url = null;
                string displayname = init.displayname ?? init.plugin;

                if (string.IsNullOrEmpty(init.overridepasswd))
                {
                    url = init.overridehost;
                    if (string.IsNullOrEmpty(url) && init.overridehosts != null && init.overridehosts.Length > 0)
                        url = init.overridehosts[Random.Shared.Next(0, init.overridehosts.Length)];
                }

                if (string.IsNullOrEmpty(url))
                    url = $"{host}/lite/" + (plugin ?? init.plugin).ToLower();

                piders.Add((init.displayname ?? init.plugin, $"{url}?title={HttpUtility.UrlEncode(title)}&clarification=1&rjson=true&similar=true", init.displayindex));
            }
            #endregion

            if (isanime)
            {
                var animeConf = ModInit.animeConf;

                send(animeConf.Kodik);
                send(animeConf.AnimeLib);
                send(animeConf.AnilibriaOnline, "anilibria");
                send(animeConf.Animevost);
                send(animeConf.Animebesst);
                send(animeConf.MoonAnime);
                send(animeConf.AnimeGo);
            }

            send(ModInit.premiumConf.Filmix);
            send(ModInit.premiumConf.FilmixTV, "filmixtv");
            send(ModInit.premiumConf.FilmixPartner, "fxapi");

            send(ModInit.siteConf.Rezka);
            send(ModInit.premiumConf.RezkaPrem, "rhsprem");

            send(ModInit.premiumConf.KinoPub);
            send(ModInit.siteConf.Kinogo);
            send(ModInit.premiumConf.GetsTV, "getstv-search");
            send(ModInit.siteConf.Kinobase);
            send(ModInit.premiumConf.Alloha, "alloha-search");
            send(ModInit.siteConf.Collaps, "collaps-search");
            send(ModInit.siteConf.VeoVeo, "veoveo-spider");

            if (!string.IsNullOrEmpty(ModInit.siteConf.VideoCDN.token))
                send(ModInit.siteConf.VideoCDN);

            if (ModInit.siteConf.Lumex.priorityBrowser == "http" || PlaywrightBrowser.Status != PlaywrightStatus.disabled)
                send(ModInit.siteConf.Lumex);

            send(ModInit.siteConf.VDBmovies);
            send(ModInit.siteConf.HDVB, "hdvb-search");

            return Json(piders.OrderByDescending(i => i.index).ToDictionary(k => k.name, v => v.uri));
        }
        #endregion


        #region events
        [HttpGet]
        [AllowAnonymous]
        [Route("lifeevents")]
        public ActionResult LifeEvents(string memkey, long id, string imdb_id, long kinopoisk_id, int serial)
        {
            string json = null;
            JsonResult error(string msg) => Json(new { accsdb = true, ready = true, online = new string[] { }, msg });

            if (memoryCache.TryGetValue(memkey, out List<EventLinkItem> links) && links != null)
            {
                int readyCount = links.Count(i => i?.code != null);
                if (readyCount > 0)
                {
                    bool ready = links.Count == readyCount;
                    string online = string.Join(",", links.Where(i => i?.code != null).OrderByDescending(i => i.work).ThenBy(i => i.index).Select(i => i.code));

                    if (ready && !online.Contains("\"show\":true"))
                    {
                        if (string.IsNullOrEmpty(imdb_id) && 0 >= kinopoisk_id)
                            return error($"Добавьте \"IMDB ID\" {(serial == 1 ? "сериала" : "фильма")} на https://themoviedb.org/{(serial == 1 ? "tv" : "movie")}/{id}/edit?active_nav_item=external_ids");

                        return error($"Не удалось найти онлайн для {(serial == 1 ? "сериала" : "фильма")}");
                    }

                    json = $"{{\"ready\":{(ready ? "true" : "false")},\"tasks\":{links.Count},\"online\":[{online.Replace("{localhost}", host)}]}}";
                }
            }

            return ContentTo(json ?? "{\"ready\":false,\"tasks\":0,\"online\":[]}");
        }


        static readonly Regex chineseRegex = new Regex("[\u4E00-\u9FFF]"); // Диапазон для китайских иероглифов
        static readonly Regex japaneseRegex = new Regex("[\u3040-\u30FF\uFF66-\uFF9F]"); // Хирагана, катакана и специальные символы
        static readonly Regex koreanRegex = new Regex("[\uAC00-\uD7AF]"); // Диапазон для корейских хангыльских символов

        [HttpGet]
        [Route("lite/events")]
        async public Task<ActionResult> Events(string id, string imdb_id, long kinopoisk_id, long tmdb_id, string title, string original_title, string original_language, int year, string source, string rchtype, int serial = -1, bool life = false, bool islite = false, string account_email = null, string uid = null, string token = null, string nws_id = null)
        {
            var online = new List<(dynamic init, string name, string url, string plugin, int index)>(50);
            bool isanime = original_language is "ja" or "zh";

            #region fix title
            bool fix_title = false;

            if (title != null && original_language != null && original_language.Split("|")[0] is "ja" or "ko" or "zh" or "cn")
            {
                if (long.TryParse(id, out long tmdbid) && tmdbid > 0)
                {
                    if (chineseRegex.IsMatch(title) || japaneseRegex.IsMatch(title) || koreanRegex.IsMatch(title))
                    {
                        string memkey = $"themoviedb:fix_title:{serial}:{tmdbid}";
                        if (!memoryCache.TryGetValue(memkey, out string engName))
                        {
                            var header = HeadersModel.Init(("lcrqpasswd", CoreInit.rootPasswd));
                            var result = await Http.Get<JObject>($"http://api.themoviedb.org/3/{(serial == 1 ? "tv" : "movie")}/{tmdbid}?api_key={CoreInit.conf.cub.api_key}&language=en", timeoutSeconds: 4, headers: header);
                            if (result != null)
                                engName = serial == 1 ? result.Value<string>("name") : result.Value<string>("title");

                            memoryCache.Set(memkey, engName ?? string.Empty, DateTime.Now.AddDays(1));
                        }

                        if (!string.IsNullOrEmpty(engName))
                        {
                            title = engName;
                            fix_title = true;
                        }
                    }
                }
            }
            #endregion

            var conf = ModInit.siteConf;
            var animeConf = ModInit.animeConf;
            var engConf = ModInit.engConf;
            var premiumConf = ModInit.premiumConf;

            var user = requestInfo.user;
            JObject kitconf = loadKitConf();

            #region modules
            OnlineModuleEntry.EnsureCache();

            if (OnlineModuleEntry.onlineModulesCache != null && OnlineModuleEntry.onlineModulesCache.Count > 0)
            {
                var args = new OnlineEventsModel(id, imdb_id, kinopoisk_id, title, original_title, original_language, year, source, rchtype, serial, life, islite, account_email, uid, token, nws_id, kitconf);

                foreach (var entry in OnlineModuleEntry.onlineModulesCache)
                {
                    try
                    {
                        var result = entry.Invoke(HttpContext, memoryCache, requestInfo, host, args);
                        if (result != null && result.Count > 0)
                        {
                            foreach (var r in result)
                                online.Add((null, r.name, r.url, r.plugin, r.index));
                        }

                        result = await entry.InvokeAsync(HttpContext, memoryCache, requestInfo, host, args);
                        if (result != null && result.Count > 0)
                        {
                            foreach (var r in result)
                                online.Add((null, r.name, r.url, r.plugin, r.index));
                        }
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, "CatchId={CatchId}", "id_a7376325");
                    }
                }
            }
            #endregion

            #region send
            void send(BaseSettings _init, string plugin = null, string name = null, string arg_title = null, string arg_url = null, BaseSettings myinit = null)
            {
                var init = myinit != null ? _init : loadKit(_init, kitconf);
                bool enable = init.enable && !init.rip;
                if (!enable)
                    return;

                if (rchtype != null)
                {
                    if (init.client_type != null && !init.client_type.Contains(rchtype))
                        return;

                    string rch_deny = init.RchAccessNotSupport();
                    if (rch_deny != null && rch_deny.Contains(rchtype))
                        return;

                    string stream_deny = init.StreamAccessNotSupport();
                    if (stream_deny != null && stream_deny.Contains(rchtype))
                        return;

                    if (init.rhub && !init.rhub_fallback && !init.corseu && string.IsNullOrWhiteSpace(init.webcorshost))
                    {
                        if (init.rhub_geo_disable != null &&
                            requestInfo.Country != null &&
                            init.rhub_geo_disable.Contains(requestInfo.Country))
                        {
                            return;
                        }
                    }
                }

                if (init.geo_hide != null &&
                    requestInfo.Country != null &&
                    init.geo_hide.Contains(requestInfo.Country))
                {
                    return;
                }

                if (init.group_hide)
                {
                    if (init.group > 0)
                    {
                        if (user == null || init.group > user.group)
                            return;
                    }
                    else if (CoreInit.conf.accsdb.enable)
                    {
                        if (user == null)
                            return;
                    }
                }

                string url = string.Empty;

                if (string.IsNullOrEmpty(init.overridepasswd))
                {
                    url = init.overridehost;
                    if (string.IsNullOrEmpty(url) && init.overridehosts != null && init.overridehosts.Length > 0)
                        url = init.overridehosts[Random.Shared.Next(0, init.overridehosts.Length)];
                }

                string displayname = init.displayname ?? name ?? init.plugin;

                if (!string.IsNullOrEmpty(url))
                {
                    if (plugin == "collaps-dash")
                    {
                        displayname = displayname.Replace("- 720p", "- 1080p");
                        url = url.Replace("/collaps", "/collaps-dash");
                    }
                }
                else
                {
                    url = "{localhost}/lite/" + (plugin ?? (init.plugin ?? name).ToLower()) + arg_url;
                }

                if (original_language != null && original_language.Split("|")[0] is "ru" or "ja" or "ko" or "zh" or "cn")
                {
                    string _p = (plugin ?? (init.plugin ?? name).ToLower());
                    if (_p is "filmix" or "filmixtv" or "fxapi" or "kinoukr" or "rezka" or "rhsprem" or "redheadsound" or "kinopub" or "alloha" or "lumex" or "vcdn" or "videocdn" or "fancdn" or "redheadsound" or "kinotochka" or "remux") // || (_p == "kodik" && kinopoisk_id == 0 && string.IsNullOrEmpty(imdb_id))
                        url += (url.Contains("?") ? "&" : "?") + "clarification=1";
                }

                online.Add((myinit, $"{displayname}{arg_title}", url, (plugin ?? init.plugin ?? name).ToLower(), init.displayindex > 0 ? init.displayindex : online.Count));
            }
            #endregion

            if (original_language != null && original_language.Split("|")[0] is "ja" or "ko" or "zh" or "cn" or "th" or "vi" or "tl")
                send(animeConf.Kodik);

            if (serial == -1 || isanime)
            {
                send(animeConf.AniLiberty);
                send(animeConf.AnilibriaOnline, "anilibria", "Anilibria");
                send(animeConf.AnimeLib);
                send(animeConf.Animevost);
                send(animeConf.Animebesst);
                send(animeConf.Dreamerscast);
                send(animeConf.AnimeGo);
                send(animeConf.AniMedia);

                if (PlaywrightBrowser.Status != PlaywrightStatus.disabled || !string.IsNullOrEmpty(animeConf.MoonAnime.overridehost) || animeConf.MoonAnime.overridehosts?.Length > 0)
                    send(animeConf.MoonAnime);
            }

            #region VoKino
            if (kinopoisk_id > 0 || source?.ToLower() == "vokino")
            {
                string vid = kinopoisk_id.ToString();
                if (source?.ToLower() == "vokino" && !string.IsNullOrEmpty(id))
                    vid = id;

                var myinit = loadKit(premiumConf.VoKino, kitconf, (j, i, c) =>
                {
                    if (j.ContainsKey("online"))
                        i.online = c.online;

                    return i;
                });

                if (myinit.enable && !string.IsNullOrEmpty(myinit.token))
                {
                    async ValueTask vkino()
                    {
                        if (myinit.rhub || !ModInit.conf.checkOnlineSearch)
                        {
                            VoKinoInvoke.SendOnline(myinit, online, null);
                        }
                        else
                        {
                            if (!memoryCache.TryGetValue($"vokino:view:{vid}", out JObject view))
                            {
                                view = await Http.Get<JObject>($"{myinit.host}/v2/view/{vid}?token={myinit.token}", timeoutSeconds: 4);
                                if (view != null)
                                    memoryCache.Set($"vokino:view:{vid}", view, cacheTimeBase(20, init: premiumConf.VoKino));
                            }

                            if (view != null && view.ContainsKey("online") && view["online"] is JObject onlineObj)
                                VoKinoInvoke.SendOnline(myinit, online, onlineObj);
                        }
                    }
                    ;

                    if (CoreInit.conf.accsdb.enable)
                    {
                        if (user != null)
                        {
                            if (myinit.group > user.group && myinit.group_hide) { }
                            else
                                await vkino();
                        }
                    }
                    else
                    {
                        if (myinit.group > 0 && myinit.group_hide && (user == null || myinit.group > user.group)) { }
                        else
                            await vkino();
                    }
                }
            }
            #endregion

            #region Filmix
            {
                var myinit = loadKit(premiumConf.Filmix, kitconf, (j, i, c) =>
                {
                    if (j.ContainsKey("pro"))
                        i.pro = c.pro;
                    return i;
                });

                if (string.IsNullOrEmpty(myinit.token) && (myinit.tokens == null || myinit.tokens.Length == 0) && premiumConf.Filmix.hidefreeStart > 0)
                {
                    if (TimeZoneTo.ByIds(["Europe/Kyiv", "Europe/Kiev", "FLE Standard Time"], out DateTime kievTime))
                    {
                        if (kievTime.Hour >= premiumConf.Filmix.hidefreeStart && kievTime.Hour < premiumConf.Filmix.hidefreeEnd)
                            myinit.enable = false;
                    }
                }

                send(myinit, myinit: myinit);
            }

            send(premiumConf.FilmixTV, "filmixtv");
            send(premiumConf.FilmixPartner, "fxapi", "Filmix");
            #endregion

            send(premiumConf.KinoPub);
            send(premiumConf.IptvOnline, "iptvonline", "iptv.online");
            send(premiumConf.GetsTV);

            #region Alloha
            {
                var myinit = loadKit(premiumConf.Alloha, kitconf, (j, i, c) =>
                {
                    if (j.ContainsKey("m4s"))
                        i.m4s = c.m4s;
                    return i;
                });

                send(myinit, myinit: myinit);
            }
            #endregion

            #region RezkaPrem
            {
                var rezka_premium = loadKit(premiumConf.RezkaPrem, kitconf, (j, i, c) =>
                {
                    if (j.ContainsKey("premium"))
                        i.premium = c.premium;
                    return i;
                });

                send(rezka_premium, "rhsprem", "HDRezka", myinit: rezka_premium);
            }
            #endregion

            #region Rezka
            {
                var myinit = loadKit(conf.Rezka, (j, i, c) =>
                {
                    if (j.ContainsKey("premium"))
                        i.premium = c.premium;
                    return i;
                });

                send(myinit, myinit: myinit);
            }
            #endregion

            if (PlaywrightBrowser.Status != PlaywrightStatus.disabled)
                send(conf.Mirage);

            if (PlaywrightBrowser.Status != PlaywrightStatus.disabled || !string.IsNullOrEmpty(conf.Kinobase.overridehost) || conf.Kinobase.overridehosts?.Length > 0)
                send(conf.Kinobase);

            if (conf.VDBmovies.rhub || conf.VDBmovies.priorityBrowser == "http" || PlaywrightBrowser.Status != PlaywrightStatus.disabled || !string.IsNullOrEmpty(conf.VDBmovies.overridehost) || conf.VDBmovies.overridehosts?.Length > 0)
                send(conf.VDBmovies);

            if (kinopoisk_id > 0)
            {
                if (conf.VideoDB.rhub || conf.VideoDB.priorityBrowser == "http" || PlaywrightBrowser.Status != PlaywrightStatus.disabled || !string.IsNullOrEmpty(conf.VideoDB.overridehost) || conf.VideoDB.overridehosts?.Length > 0)
                    send(conf.VideoDB);
            }

            send(conf.VideoCDN);

            if (conf.Lumex.priorityBrowser == "http" || PlaywrightBrowser.Status != PlaywrightStatus.disabled || !string.IsNullOrEmpty(conf.Lumex.overridehost) || conf.Lumex.overridehosts?.Length > 0)
                send(conf.Lumex);

            if ((serial == -1 || serial == 0) && kinopoisk_id > 0)
                send(conf.FanCDN);

            if (!isanime)
                send(conf.FlixCDN);

            if (PlaywrightBrowser.Status != PlaywrightStatus.disabled || !string.IsNullOrEmpty(conf.Kinogo.overridehost) || conf.Kinogo.overridehosts?.Length > 0)
                send(conf.Kinogo);

            if (KinoukrInvoke.KinoukrDb != null)
            {
                send(ModInit.ukrConf.Ashdi, "ashdi", "Ashdi (Украинский)");

                if (!isanime)
                    send(ModInit.ukrConf.Kinoukr, "kinoukr", "Kinoukr (Украинский)");
            }

            send(ModInit.ukrConf.Eneyida, "eneyida", "Eneyida (Украинский)");

            if (kinopoisk_id > 0)
                send(conf.CDNvideohub, "cdnvideohub", "VideoHUB");

            if ((serial == -1 || serial == 0) && !isanime)
                send(conf.LeProduction);

            #region Collaps
            {
                var myinit = loadKit(conf.Collaps, kitconf, (j, i, c) =>
                {
                    if (j.ContainsKey("dash"))
                        i.dash = c.dash;
                    if (j.ContainsKey("two"))
                        i.two = c.two;
                    return i;
                });

                send(myinit, "collaps", $"Collaps ({(myinit.dash ? "DASH" : "HLS")})", myinit: myinit);

                if (myinit.two && !myinit.dash)
                    send(myinit, "collaps-dash", "Collaps (DASH)");
            }
            #endregion

            if (serial == -1 || serial == 0)
            {
                send(conf.Plvideo, "plvideo", "Plvideo");
                send(conf.RutubeMovie, "rutubemovie", "Rutube");
                send(conf.VkMovie, "vkmovie", "VK Видео");
            }

            if (PlaywrightBrowser.Status != PlaywrightStatus.disabled || !string.IsNullOrEmpty(conf.Videoseed.overridehost) || conf.Videoseed.overridehosts?.Length > 0)
                send(conf.Videoseed);

            send(conf.Vibix);
            send(conf.VeoVeo);

            if (serial == -1 || serial == 0)
                send(premiumConf.iRemux, "remux");

            #region PidTor
            if (ModInit.PidTor.enable)
            {
                if ((ModInit.PidTor.torrs != null && ModInit.PidTor.torrs.Length > 0) || (ModInit.PidTor.auth_torrs != null && ModInit.PidTor.auth_torrs.Count > 0))
                {
                    void psend()
                    {
                        if (ModInit.PidTor.group > 0 && ModInit.PidTor.group_hide)
                        {
                            if (user == null || ModInit.PidTor.group > user.group)
                                return;
                        }

                        online.Add((null, $"{ModInit.PidTor.displayname ?? "PidŦor"}", "{localhost}/lite/pidtor", "pidtor", ModInit.PidTor.displayindex > 0 ? ModInit.PidTor.displayindex : online.Count));
                    }

                    psend();
                }
            }
            #endregion

            if (serial == -1 || serial == 0)
                send(conf.Redheadsound);

            send(conf.HDVB);
            send(conf.Kinotochka);

            #region Грузинские
            if (kinopoisk_id > 0 && !isanime)
                send(conf.Kinoflix);

            if (serial == -1 || serial == 0)
                send(conf.Geosaitebi);

            if (serial == 1)
                send(conf.AsiaGe);
            #endregion

            if ((serial == -1 || (serial == 1 && !isanime)) && kinopoisk_id > 0)
                send(conf.CDNmovies);

            #region ENG
            if ((original_language == null || original_language == "en") && CoreInit.conf.disableEng == false)
            {
                if (tmdb_id > 0 || (source != null && (source is "tmdb" or "cub")))
                {
                    if (PlaywrightBrowser.Status != PlaywrightStatus.disabled || !string.IsNullOrEmpty(engConf.Hydraflix.overridehost) || engConf.Hydraflix.overridehosts?.Length > 0)
                        send(engConf.Hydraflix, "hydraflix", "HydraFlix (ENG)");

                    if (PlaywrightBrowser.Status != PlaywrightStatus.disabled || !string.IsNullOrEmpty(engConf.Vidsrc.overridehost) || engConf.Vidsrc.overridehosts?.Length > 0)
                        send(engConf.Vidsrc, "vidsrc", "VidSrc (ENG)");

                    if (PlaywrightBrowser.Status != PlaywrightStatus.disabled || !string.IsNullOrEmpty(engConf.VidLink.overridehost) || engConf.VidLink.overridehosts?.Length > 0)
                        send(engConf.VidLink, "vidlink", "VidLink (ENG)");

                    if (PlaywrightBrowser.Status != PlaywrightStatus.disabled || !string.IsNullOrEmpty(engConf.Videasy.overridehost) || engConf.Videasy.overridehosts?.Length > 0)
                        send(engConf.Videasy, "videasy", "Videasy (ENG)");

                    if (PlaywrightBrowser.Status != PlaywrightStatus.disabled || !string.IsNullOrEmpty(engConf.MovPI.overridehost) || engConf.MovPI.overridehosts?.Length > 0)
                        send(engConf.MovPI, "movpi", "MovPI (ENG)");

                    if (PlaywrightBrowser.Status != PlaywrightStatus.disabled || !string.IsNullOrEmpty(engConf.Smashystream.overridehost) || engConf.Smashystream.overridehosts?.Length > 0)
                        send(engConf.Smashystream, "smashystream", "SmashyStream (ENG)");

                    if (PlaywrightBrowser.Status != PlaywrightStatus.disabled || !string.IsNullOrEmpty(engConf.Autoembed.overridehost) || engConf.Autoembed.overridehosts?.Length > 0)
                        send(engConf.Autoembed, "autoembed", "AutoEmbed (ENG)");

                    if (PlaywrightBrowser.Status != PlaywrightStatus.disabled || !string.IsNullOrEmpty(engConf.Playembed.overridehost) || engConf.Playembed.overridehosts?.Length > 0)
                        send(engConf.Playembed, "playembed", "PlayEmbed (ENG)");

                    if (Firefox.Status != PlaywrightStatus.disabled || !string.IsNullOrEmpty(engConf.Twoembed.overridehost) || engConf.Twoembed.overridehosts?.Length > 0)
                        send(engConf.Twoembed, "twoembed", "2Embed (ENG)");

                    send(engConf.Rgshows, "rgshows", "RgShows (ENG)");
                }
            }
            #endregion

            #region checkOnlineSearch
            if (ModInit.conf.checkOnlineSearch && !string.IsNullOrEmpty(id))
            {
                string memkey = CrypTo.md5($"checkOnlineSearch:{id}:{serial}:{source?.Replace("tmdb", "")?.Replace("cub", "")}:{online.Count}:{(IsKitConf ? requestInfo.user_uid : null)}");

                if (!memoryCache.TryGetValue(memkey, out List<EventLinkItem> links))
                {
                    var tasks = new List<Task>();
                    links = new List<EventLinkItem>(online.Count);
                    for (int i = 0; i < online.Count; i++)
                        links.Add(default);

                    memoryCache.Set(memkey, links, DateTime.Now.AddMinutes(5));

                    foreach (var o in online)
                    {
                        var tk = checkSearch(memkey, links, tasks.Count, o.init, o.index, o.name, o.url, o.plugin, id, imdb_id, kinopoisk_id, tmdb_id, title, original_title, original_language, source, year, serial, life, rchtype);
                        tasks.Add(tk);
                    }

                    if (life)
                        return Json(new { life = true, memkey, title = (fix_title ? title : null) });

                    await Task.WhenAll(tasks);
                }

                if (life)
                    return Json(new { life = true, memkey });

                return ContentTo($"[{string.Join(",", links.Where(i => i.code != null).OrderByDescending(i => i.work).ThenBy(i => i.index).Select(i => i.code)).Replace("{localhost}", host)}]");
            }
            #endregion

            string online_result = string.Join(",", online.OrderBy(i => i.index).Select(i => "{\"name\":\"" + i.name + "\",\"url\":\"" + i.url + "\",\"balanser\":\"" + i.plugin + "\"}"));
            return ContentTo($"[{online_result.Replace("{localhost}", host)}]");
        }
        #endregion

        #region checkSearch
        async Task checkSearch(string memkey, List<EventLinkItem> links, int indexList, dynamic init, int index, string name, string uri, string plugin,
                               string id, string imdb_id, long kinopoisk_id, long tmdb_id, string title, string original_title, string original_language, string source, int year, int serial, bool life, string rchtype)
        {
            try
            {
                string srq = uri.Replace("{localhost}", $"http://{CoreInit.conf.listen.localhost}:{CoreInit.conf.listen.port}");
                var header = uri.Contains("{localhost}") ? HeadersModel.Init(("xhost", host), ("xscheme", HttpContext.Request.Scheme), ("lcrqpasswd", CoreInit.rootPasswd)) : null;

                string checkuri = $"{srq}{(srq.Contains("?") ? "&" : "?")}id={HttpUtility.UrlEncode(id)}&imdb_id={imdb_id}&kinopoisk_id={kinopoisk_id}&tmdb_id={tmdb_id}&title={HttpUtility.UrlEncode(title)}&original_title={HttpUtility.UrlEncode(original_title)}&original_language={original_language}&source={source}&year={year}&serial={serial}&rchtype={rchtype}&checksearch=true";
                string res = await Http.Get(AccsDbInvk.Args(checkuri, HttpContext), timeoutSeconds: 10, headers: header);

                if (string.IsNullOrEmpty(res))
                    res = string.Empty;

                bool rch = res.Contains("\"rch\":true");
                bool work = rch || res.Contains("data-json=")
                    || res.Contains("\"type\":\"movie\"")
                    || res.Contains("\"type\":\"episode\"")
                    || res.Contains("\"type\":\"season\"");

                string quality = string.Empty;
                string balanser = plugin.Contains("/") ? plugin.Split("/")[1] : plugin;

                #region определение качества
                if (work && life)
                {
                    foreach (string q in new string[] { "2160", "1080", "720", "480", "360" })
                    {
                        if (res.Contains("<!--q:"))
                        {
                            quality = " - " + Regex.Match(res, "<!--q:([^>]+)-->").Groups[1].Value;
                            break;
                        }
                        else if (res.Contains($"\"{q}p\"") || res.Contains($">{q}p<") || res.Contains($"<!--{q}p-->"))
                        {
                            quality = $" - {q}p";
                            break;
                        }
                    }

                    if (quality == "2160")
                        quality = res.Contains("HDR") ? " - 4K HDR" : " - 4K";

                    if (init != null)
                    {
                        if (balanser == "filmix")
                        {
                            if (!init.pro)
                                quality = string.IsNullOrEmpty(init.token) ? " - 480p" : " - 720p";
                        }

                        if (balanser == "alloha")
                            quality = string.IsNullOrEmpty(quality) ? (init.m4s ? " ~ 2160p" : " ~ 1080p") : quality;

                        if (balanser == "rezka" || balanser == "rhs")
                        {
                            string rezkaq = init.premium ? " ~ 2160p" : " ~ 720p";
                            quality = string.IsNullOrEmpty(quality) ? rezkaq : quality;
                        }

                        if (balanser == "collaps")
                            quality = init.dash ? " ~ 1080p" : " ~ 720p";
                    }

                    if (quality == string.Empty)
                    {
                        switch (balanser)
                        {
                            case "fxapi":
                            case "filmix":
                            case "filmixtv":
                            case "kinopub":
                            case "vokino":
                            case "vokino-alloha":
                            case "vokino-filmix":
                            case "alloha":
                            case "remux":
                            case "pidtor":
                            case "rhsprem":
                            case "animelib":
                            case "mirage":
                            case "videodb":
                            case "iptvonline":
                            case "plvideo":
                            case "rutubemovie":
                            case "vkmovie":
                            case "cdnvideohub":
                                quality = " ~ 2160p";
                                break;
                            case "kinobase":
                            case "kinogo":
                            case "getstv":
                            case "zetflix":
                            case "vcdn":
                            case "videocdn":
                            case "lumex":
                            case "vibix":
                            case "videoseed":
                            case "eneyida":
                            case "kinoukr":
                            case "ashdi":
                            case "hdvb":
                            case "anilibria":
                            case "aniliberty":
                            case "redheadsound":
                            case "iframevideo":
                            case "animego":
                            case "lostfilmhd":
                            case "vdbmovies":
                            case "collaps-dash":
                            case "fancdn":
                            case "moonanime":
                            case "playembed":
                            case "rgshows":
                            case "twoembed":
                            case "vidsrc":
                            case "smashystream":
                            case "hydraflix":
                            case "movpi":
                            case "videasy":
                            case "vidlink":
                            case "autoembed":
                            case "veoveo":
                            case "vokino-vibix":
                            case "vokino-monframe":
                            case "vokino-remux":
                            case "vokino-ashdi":
                            case "vokino-hdvb":
                            case "flixcdn":
                            case "kinoflix":
                            case "dreamerscast":
                            case "leproduction":
                            case "asiage":
                                quality = " ~ 1080p";
                                break;
                            case "voidboost":
                            case "animedia":
                            case "animevost":
                            case "animebesst":
                            case "kodik":
                            case "kinotochka":
                            case "rhs":
                            case "geosaitebi":
                                quality = " ~ 720p";
                                break;
                            case "kinokrad":
                            case "kinoprofi":
                            case "seasonvar":
                                quality = " - 480p";
                                break;
                            case "cdnmovies":
                                quality = " - 360p";
                                break;
                            default:
                                break;
                        }

                        if (balanser == "vokino")
                            quality = res.Contains("4K HDR") ? " - 4K HDR" : res.Contains("4K ") ? " - 4K" : quality;
                    }
                }
                #endregion

                if (!name.Contains(" - ") && ModInit.conf.showquality && !string.IsNullOrEmpty(quality))
                {
                    name = Regex.Replace(name, " ~ .*$", "");
                    name += quality;
                }

                links[indexList] = new("{" + $"\"name\":\"{name}\",\"url\":\"{uri}\",\"index\":{index},\"show\":{work.ToString().ToLower()},\"balanser\":\"{plugin}\",\"rch\":{rch.ToString().ToLower()}" + "}", index, work);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "CatchId={CatchId}", "id_effc21fb");
            }
        }
        #endregion
    }
}
