using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Attributes;
using Shared.Models.Base;
using Shared.Models.Templates;
using Shared.Services.RxEnumerate;
using Shared.Services.Utilities;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace LeProduction
{
    public class LeProductionController : BaseOnlineController
    {
        public LeProductionController() : base(ModInit.conf) { }

        [HttpGet]
        [Staticache]
        [Route("lite/leproduction")]
        async public Task<ActionResult> Index(string title, string original_title, int clarification, int serial = 0, string href = null)
        {
            if (serial == 1)
                return OnError();

            if (await IsRequestBlocked(rch: true))
                return badInitMsg;

            rhubFallback:

            #region search
            if (string.IsNullOrEmpty(href))
            {
                string searchTitle = clarification == 1 ? title : (original_title ?? title);
                if (string.IsNullOrWhiteSpace(searchTitle))
                    return OnError();

                var search = await InvokeCacheResult<(string href, SimilarTpl similar)>($"leproduction:search:{searchTitle}", TimeSpan.FromHours(4), async e =>
                {
                    string newsHref = null;
                    var similar = new SimilarTpl();

                    string searchUrl = $"{init.host}/index.php?do=search&subaction=search&search_start=0&full_search=0&result_from=1&story={HttpUtility.UrlEncode(searchTitle)}";
                    await httpHydra.GetSpan(searchUrl, spanAction: html =>
                    {
                        string stitle = StringConvert.SearchName(title);
                        string soriginal = StringConvert.SearchName(original_title);

                        var rx = Rx.Matches("<a\\s+href=\"https?://[^/]+/(film/[0-9]+-[^\"]+\\.html)\"[^>]*>([^<]+)</a>", html, options: RegexOptions.IgnoreCase);

                        foreach (var row in rx.Rows())
                        {
                            var g = row.Groups();
                            string itemHref = g[1].Value;
                            string itemTitle = Regex.Replace(g[2].Value, "<[^>]*>", string.Empty).Trim();

                            if (string.IsNullOrWhiteSpace(itemHref) || string.IsNullOrWhiteSpace(itemTitle))
                                continue;

                            similar.Append(itemTitle, string.Empty, string.Empty, itemHref, string.Empty);

                            string normalized = StringConvert.SearchName(itemTitle);
                            if (newsHref == null && (normalized.Contains(stitle) || (!string.IsNullOrWhiteSpace(soriginal) && normalized.Contains(soriginal))))
                                newsHref = itemHref;
                        }
                    });

                    if (newsHref == null && similar.Length > 0)
                        return e.Success((null, similar));

                    if (string.IsNullOrWhiteSpace(newsHref))
                        return e.Fail("search", refresh_proxy: true);

                    return e.Success((newsHref, similar));
                });

                if (!search.IsSuccess)
                {
                    if (IsRhubFallback(search))
                        goto rhubFallback;

                    return OnError(search.ErrorMsg);
                }

                if (search.Value.href == null)
                    return ContentTpl(search.Value.similar);

                href = search.Value.href;
            }
            #endregion

            var cache = await InvokeCacheResult<string>($"leproduction:view:{href}", 20, async e =>
            {
                string iframe = null;
                await httpHydra.GetSpan($"{init.host}/{href}", spanAction: html =>
                {
                    iframe = Rx.Match(html, "<iframe[^>]+id=\"omfg\"[^>]+src=\"([^\"]+)\"");
                    if (string.IsNullOrWhiteSpace(iframe))
                        iframe = Rx.Match(html, "<iframe[^>]+src=\"(https?://[^/]+/playlist_iframe/[0-9]+/?[^\"]*)\"");
                });

                if (string.IsNullOrWhiteSpace(iframe))
                    return e.Fail("iframe", refresh_proxy: true);

                string fileBlock = null;

                await httpHydra.GetSpan(iframe, addheaders: HeadersModel.Init("referer", $"{init.host}/{href}"), spanAction: html =>
                {
                    fileBlock = Rx.Match(html, "file\\s*:\\s*(\\[[\\s\\S]*?\\])\\s*,\\s*embed\\s*:");
                });

                if (string.IsNullOrWhiteSpace(fileBlock))
                    return e.Fail("fileBlock", refresh_proxy: true);

                return e.Success(fileBlock);
            });

            if (IsRhubFallback(cache))
                goto rhubFallback;

            return ContentTpl(cache, () =>
            {
                var mtpl = new MovieTpl(title, original_title, 1);
                var streamQuality = new StreamQualityTpl();

                foreach (Match m in Regex.Matches(cache.Value, "\\[([0-9]+p)\\](https?://[^,\\[\"\t\n ]+)", RegexOptions.IgnoreCase))
                    streamQuality.Append(HostStreamProxy(m.Groups[2].Value), m.Groups[1].Value);

                if (!streamQuality.Any())
                    return null;

                var first = streamQuality.Firts();
                mtpl.Append("По умолчанию", first.link, quality: first.quality, streamquality: streamQuality, vast: init.vast);
                return mtpl;
            });
        }
    }
}
