using System.Text.Json;
using Online.Models.CDNmovies;
using Shared.Services.RxEnumerate;

namespace Online.Services
{
    public struct CDNmoviesInvoke
    {
        #region CDNmoviesInvoke
        string host;
        string apihost;
        HttpHydra httpHydra;
        Func<string, string> onstreamfile;
        public CDNmoviesInvoke(string host, string apihost, HttpHydra httpHydra, Func<string, string> onstreamfile)
        {
            this.host = host != null ? $"{host}/" : null;
            this.apihost = apihost;
            this.httpHydra = httpHydra;
            this.onstreamfile = onstreamfile;
        }
        #endregion

        #region Embed
        public async Task<Voice[]> Embed(long kinopoisk_id)
        {
            Voice[] content = null;

            await httpHydra.GetSpan($"{apihost}/serial/kinopoisk/{kinopoisk_id}", html =>
            {
                ReadOnlySpan<char> file = Rx.Slice(html, "file:'", "'");

                content = JsonSerializer.Deserialize<Voice[]>(file, new JsonSerializerOptions
                {
                    AllowTrailingCommas = true
                });
            });

            if (content == null || content.Length == 0)
            {
                return null;
            }

            return content;
        }
        #endregion

        #region Tpl
        public ITplResult Tpl(Voice[] voices, long kinopoisk_id, string title, string original_title, int t, int s, int sid, VastConf vast = null, bool rjson = false)
        {
            if (voices == null || voices.Length == 0)
                return default;

            string enc_title = HttpUtility.UrlEncode(title);
            string enc_original_title = HttpUtility.UrlEncode(original_title);

            #region Перевод
            var vtpl = new VoiceTpl(voices.Length);

            for (int i = 0; i < voices.Length; i++)
            {
                string link = host + $"lite/cdnmovies?rjson={rjson}&kinopoisk_id={kinopoisk_id}&title={enc_title}&original_title={enc_original_title}&t={i}";
                vtpl.Append(voices[i].title, t == i, link);
            }
            #endregion

            if (s == -1)
            {
                #region Сезоны
                var tpl = new SeasonTpl(vtpl, voices[t].folder.Length);

                for (int i = 0; i < voices[t].folder.Length; i++)
                {
                    string season = Regex.Match(voices[t].folder[i].title, "([0-9]+)$").Groups[1].Value;
                    if (string.IsNullOrEmpty(season))
                        continue;

                    string link = host + $"lite/cdnmovies?rjson={rjson}&kinopoisk_id={kinopoisk_id}&title={enc_title}&original_title={enc_original_title}&t={t}&s={season}&sid={i}";
                    tpl.Append($"{season} сезон", link, season);
                }

                return tpl;
                #endregion
            }
            else
            {
                #region Серии
                var etpl = new EpisodeTpl(vtpl);
                string sArhc = s.ToString();

                foreach (var item in voices[t].folder[sid].folder)
                {
                    var streamquality = new StreamQualityTpl();

                    foreach (Match m in Regex.Matches(item.file, "\\[(360|240)p?\\]([^\\[\\|,\n\r\t ]+\\.(mp4|m3u8))"))
                    {
                        string link = m.Groups[2].Value;
                        if (string.IsNullOrEmpty(link))
                            continue;

                        streamquality.Insert(onstreamfile.Invoke(link), $"{m.Groups[1].Value}p");
                    }

                    string episode = Regex.Match(item.title, "([0-9]+)$").Groups[1].Value;

                    var first = streamquality.Firts();
                    if (first != null)
                        etpl.Append($"{episode} cерия", title ?? original_title, sArhc, episode, first.link, streamquality: streamquality, vast: vast);
                }

                return etpl;
                #endregion
            }
        }
        #endregion
    }
}
