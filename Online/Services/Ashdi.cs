using Online.Models.Ashdi;
using Shared.Services.RxEnumerate;
using System.Text.Json;

namespace Online.Services
{
    public struct AshdiInvoke
    {
        #region AshdiInvoke
        string host;
        string apihost;
        HttpHydra httpHydra;
        Func<string, string> onstreamfile;
        public AshdiInvoke(string host, string apihost, HttpHydra httpHydra, Func<string, string> onstreamfile)
        {
            this.host = host != null ? $"{host}/" : null;
            this.apihost = apihost;
            this.httpHydra = httpHydra;
            this.onstreamfile = onstreamfile;
        }
        #endregion

        #region EmbedKurwa
        public async Task<EmbedModel> EmbedKurwa(string href, int clarification, string title, string original_title, int year, string imdb_id, long kinopoisk_id)
        {
            string iframeUri = href;
            EmbedModel result = null;

            if (string.IsNullOrEmpty(iframeUri))
            {
                var root = KinoukrInvoke.SearchDb(title, original_title, kinopoisk_id > 0 ? kinopoisk_id.ToString() : null, imdb_id);
                if (root == null || root.Count == 0)
                    return null;

                result = new EmbedModel();
                result.similars = new List<Similar>(root.Count);

                foreach (var item in root)
                {
                    if (string.IsNullOrEmpty(item.ashdi))
                        continue;

                    var model = new Similar()
                    {
                        href = $"https://ashdi.vip/{item.ashdi}",
                        title = $"{item.name} / {item.eng_name}",
                        year = item.year
                    };

                    if (item.year == year.ToString())
                        result.similars.Insert(0, model);
                    else
                        result.similars.Add(model);
                }

                if (result.similars.Count == 0)
                    return new EmbedModel() { IsEmpty = true };

                if (result.similars.Count > 1 && result.similars[0].year != year.ToString())
                    return result;

                iframeUri = result.similars[0].href;
            }

            await httpHydra.GetSpan(iframeUri, content =>
            {
                if (!content.Contains("new Playerjs", StringComparison.Ordinal))
                    return;

                if (!Regex.IsMatch(content, "file:([\t ]+)?'\\[\\{"))
                {
                    var rx = Rx.Split("new Playerjs", content);
                    if (1 > rx.Count)
                        return;

                    result = new EmbedModel()
                    {
                        content = rx[1].ToString()
                    };

                    return;
                }
                else
                {
                    try
                    {
                        var root = JsonSerializer.Deserialize<Voice[]>(Rx.Match(content, "file:([\t ]+)?'([^\n\r]+)',", 2), new JsonSerializerOptions
                        {
                            AllowTrailingCommas = true
                        });

                        if (root != null && root.Length > 0)
                            result = new EmbedModel() { serial = root };
                    }
                    catch (System.Exception ex)
                    {
                        Serilog.Log.Error(ex, "{Class} {CatchId}", "Ashdi", "id_4egjgt5u");
                    }
                }
            });



            return result;
        }
        #endregion

        #region Tpl
        public ITplResult Tpl(EmbedModel md, string href, string imdb_id, long kinopoisk_id, string title, string original_title, int clarification, int year, int t, int s, VastConf vast = null, bool rjson = false, string mybaseurl = null)
        {
            if (md == null || md.IsEmpty || (string.IsNullOrEmpty(md.content) && md.serial == null))
                return default;

            string enc_title = HttpUtility.UrlEncode(title);
            string enc_original_title = HttpUtility.UrlEncode(original_title);
            string enc_href = HttpUtility.UrlEncode(href);

            #region similar
            if (md.content == null && md.serial == null)
            {
                if (string.IsNullOrWhiteSpace(href) && md.similars != null && md.similars.Count > 0)
                {
                    var stpl = new SimilarTpl(md.similars.Count);

                    foreach (var similar in md.similars)
                    {
                        string link = host + $"lite/ashdi?rjson={rjson}&clarification={clarification}&title={enc_title}&original_title={enc_original_title}&year={year}&href={HttpUtility.UrlEncode(similar.href)}";

                        stpl.Append(similar.title, similar.year, string.Empty, link);
                    }

                    return stpl;
                }

                return default;
            }
            #endregion

            string fixStream(string _l) => _l.Replace("0yql3tj", "oyql3tj");

            if (md.content != null)
            {
                #region Фильм
                var mtpl = new MovieTpl(title, original_title, 1);

                string hls = Regex.Match(md.content, "file:([\t ]+)?(\"|')([\t ]+)?(?<hls>https?://[^\"'\n\r\t ]+/index.m3u8)").Groups["hls"].Value;
                if (string.IsNullOrEmpty(hls))
                    return default;

                #region subtitle
                SubtitleTpl subtitles = null;
                string subtitle = new Regex("subtitle(\")?:\"([^\"]+)\"").Match(md.content).Groups[2].Value;

                if (!string.IsNullOrEmpty(subtitle))
                {
                    var match = new Regex("\\[([^\\]]+)\\](https?://[^\\,]+)").Match(subtitle);
                    subtitles = new SubtitleTpl(match.Length);

                    while (match.Success)
                    {
                        subtitles.Append(match.Groups[1].Value, onstreamfile.Invoke(fixStream(match.Groups[2].Value)));
                        match = match.NextMatch();
                    }
                }
                #endregion

                mtpl.Append("По умолчанию", onstreamfile.Invoke(fixStream(hls)), subtitles: subtitles, vast: vast);

                return mtpl;
                #endregion
            }
            else
            {
                #region Сериал
                try
                {
                    if (s == -1)
                    {
                        var tpl = new SeasonTpl();
                        var hashseason = new HashSet<string>(20);

                        foreach (var voice in md.serial)
                        {
                            foreach (var season in voice.folder)
                            {
                                if (!hashseason.Add(season.title))
                                    continue;

                                string numberseason = Regex.Match(season.title, "([0-9]+)$").Groups[1].Value;
                                if (string.IsNullOrEmpty(numberseason))
                                    continue;

                                string baseUrl = mybaseurl ?? (host + $"lite/ashdi?rjson={rjson}&imdb_id={imdb_id}&kinopoisk_id={kinopoisk_id}&title={enc_title}&original_title={enc_original_title}&clarification={clarification}&year={year}");
                                string link = $"{baseUrl}&s={numberseason}";

                                tpl.Append(season.title, link, numberseason);
                            }
                        }

                        return tpl;
                    }
                    else
                    {
                        #region Перевод
                        var vtpl = new VoiceTpl();

                        for (int i = 0; i < md.serial.Length; i++)
                        {
                            if (md.serial[i].folder?.FirstOrDefault(i => i.title.EndsWith($" {s}")) == null)
                                continue;

                            if (t == -1)
                                t = i;

                            string baseUrl = mybaseurl ?? (host + $"lite/ashdi?rjson={rjson}&imdb_id={imdb_id}&kinopoisk_id={kinopoisk_id}&title={enc_title}&original_title={enc_original_title}&clarification={clarification}&year={year}");
                            string link = $"{baseUrl}&s={s}&t={i}";

                            vtpl.Append(md.serial[i].title, t == i, link);
                        }
                        #endregion

                        string sArch = s.ToString();
                        var episodes = md.serial[t].folder.First(i => i.title.EndsWith($" {s}")).folder;

                        var etpl = new EpisodeTpl(vtpl, episodes.Length);

                        foreach (var episode in episodes)
                        {
                            #region subtitle
                            SubtitleTpl subtitles = null;

                            if (!string.IsNullOrEmpty(episode.subtitle))
                            {
                                var match = new Regex("\\[([^\\]]+)\\](https?://[^\\,]+)").Match(episode.subtitle);
                                subtitles = new SubtitleTpl(match.Length);

                                while (match.Success)
                                {
                                    subtitles.Append(match.Groups[1].Value, onstreamfile.Invoke(fixStream(match.Groups[2].Value)));
                                    match = match.NextMatch();
                                }
                            }
                            #endregion

                            string file = onstreamfile.Invoke(fixStream(episode.file));
                            etpl.Append(episode.title, title ?? original_title, sArch, Regex.Match(episode.title, "([0-9]+)$").Groups[1].Value, file, subtitles: subtitles, vast: vast);
                        }

                        return etpl;
                    }
                }
                catch
                {
                    return default;
                }
                #endregion
            }
        }
        #endregion
    }
}
