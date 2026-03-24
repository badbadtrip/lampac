using Newtonsoft.Json;
using Online.Models.KinoUkr;
using System.Threading;

namespace Online.Services
{
    public static class KurwaCron
    {
        static int _updatingKinoukrDb = 0;

        async public static void Kinoukr(object state)
        {
            if (Interlocked.Exchange(ref _updatingKinoukrDb, 1) == 1)
                return;

            try
            {
                bool savedb = false;

                string mainHtml = await Http.Get("https://kinoukr.tv/main/");
                if (mainHtml == null)
                    return;

                var m = Regex.Match(mainHtml, "class=\"mask flex-col ps-link\" href=\"https?://[^/]+/([^\"]+\\.html)\"");
                while (m.Success)
                {
                    string link = m.Groups[1].Value;
                    string news = await Http.Get("https://kinoukr.com/" + link);
                    if (news != null)
                    {
                        string name = Regex.Match(news, "itemprop=\"name\">([^<]+)</h1>").Groups[1].Value.Trim();
                        string eng_name = Regex.Match(news, "class=\"foriginal\">([^<]+)</div>").Groups[1].Value.Trim();
                        string year = Regex.Match(news, "<span>Рік:</span> ?<a [^>]+>([0-9]+)</a>").Groups[1].Value;

                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(eng_name) && !string.IsNullOrEmpty(year))
                        {
                            string tortuga = Regex.Match(news, "src=\"https?://tortuga\\.[a-z]+/([^\"]+)\"").Groups[1].Value;
                            if (string.IsNullOrEmpty(tortuga))
                                tortuga = null;

                            string ashdi = Regex.Match(news, "src=\"https?://ashdi\\.vip/([^\"]+)\"").Groups[1].Value;
                            if (string.IsNullOrEmpty(ashdi))
                                ashdi = null;

                            if (!string.IsNullOrEmpty(tortuga) || !string.IsNullOrEmpty(ashdi))
                            {
                                var md = new Model()
                                {
                                    ashdi = ashdi,
                                    tortuga = tortuga,
                                    eng_name = eng_name,
                                    name = name,
                                    year = year
                                };

                                if (!KinoukrInvoke.KinoukrDb.ContainsKey(link))
                                {
                                    KinoukrInvoke.KinoukrDb.TryAdd(link, md);
                                    savedb = true;
                                }
                                else
                                {
                                    if (string.IsNullOrEmpty(md.tortuga))
                                        md.tortuga = KinoukrInvoke.KinoukrDb[link].tortuga;

                                    if (string.IsNullOrEmpty(md.ashdi))
                                        md.ashdi = KinoukrInvoke.KinoukrDb[link].ashdi;

                                    KinoukrInvoke.KinoukrDb[link] = md;
                                }
                            }
                        }
                    }

                    m = m.NextMatch();
                }

                if (savedb)
                    File.WriteAllText("data/kinoukr.json", JsonConvert.SerializeObject(KinoukrInvoke.KinoukrDb, Formatting.Indented));
            }
            catch (System.Exception ex)
            {
                Serilog.Log.Error(ex, "CatchId={CatchId}", "id_5b103f03");
            }
            finally
            {
                Volatile.Write(ref _updatingKinoukrDb, 0);
            }
        }
    }
}
