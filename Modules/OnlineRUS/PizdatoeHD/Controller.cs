using Microsoft.AspNetCore.Mvc;
using Microsoft.Playwright;
using Shared;
using Shared.Models.Base;
using Shared.Models.Templates;
using Shared.PlaywrightCore;
using System.Threading.Tasks;
using System.Web;

namespace PizdatoeHD
{
    public class PizdatoeHDController : BaseOnlineController
    {
        PizdaInvoke oninvk;

        public PizdatoeHDController() : base(ModInit.conf)
        {
            requestInitializationAsync = async () =>
            {
                oninvk = new PizdaInvoke
                (
                    host,
                    "lite/pizdatoehd",
                    init,
                    streamfile => HostStreamProxy(streamfile)
                );
            };
        }


        [HttpGet]
        [Route("lite/pizdatoehd")]
        async public Task<ActionResult> Index(string title, string original_title, int clarification, int year, int s = -1, string href = null, bool rjson = false, int serial = -1, bool similar = false, string source = null, string id = null)
        {
            if (await IsRequestBlocked(rch: false))
                return badInitMsg;

            if (string.IsNullOrWhiteSpace(href) && string.IsNullOrWhiteSpace(title))
                return OnError();

            using (var browser = new PlaywrightBrowser(init.priorityBrowser))
            {
                var page = await browser.NewPageAsync(init.plugin, init.headers, proxy: proxy_data, imitationHuman: true).ConfigureAwait(false);
                if (page == null)
                    return OnError("page");

                #region search
                if (string.IsNullOrEmpty(href))
                {
                    var search = await InvokeCacheResult<SearchModel>($"pizdatoehd:search:{title}:{original_title}:{clarification}:{year}", 240, textJson: true, onget: async e =>
                    {
                        string search_uri = $"{init.host}/search/?do=search&subaction=search&q={HttpUtility.UrlEncode(clarification == 1 ? title : (original_title ?? title))}";

                        var result = await page.GotoAsync(search_uri, new PageGotoOptions() { WaitUntil = WaitUntilState.DOMContentLoaded });
                        if (result == null)
                            return e.Fail("не удалось загрузить страницу", refresh_proxy: true);

                        string html = await result.TextAsync();
                        if (string.IsNullOrEmpty(html))
                            return e.Fail("не удалось получить содержимое страницы");

                        var content = oninvk.Search(search_uri, html, title, original_title, year);
                        if (content == null || content.IsError)
                            return e.Fail(string.Empty, refresh_proxy: true);

                        if (content.IsEmpty)
                        {
                            if (rch.enable || content.content != null)
                                return e.Fail(content.content ?? "content");
                        }

                        return e.Success(content);
                    });

                    if (search.ErrorMsg != null)
                        return ShowError(string.IsNullOrEmpty(search.ErrorMsg) ? "поиск не дал результатов" : search.ErrorMsg);

                    if (similar || string.IsNullOrEmpty(search.Value?.href))
                    {
                        if (search.Value?.IsEmpty == true)
                            return ShowError(search.Value.content ?? "поиск не дал результатов");

                        return ContentTpl(search, () =>
                        {
                            if (search.Value.similar == null)
                                return default;

                            var stpl = new SimilarTpl(search.Value.similar.Count);
                            string enc_title = HttpUtility.UrlEncode(title);
                            string enc_original_title = HttpUtility.UrlEncode(original_title);

                            foreach (var similar in search.Value.similar)
                            {
                                string link = $"{host}/lite/pizdatoehd?rjson={rjson}&title={enc_title}&original_title={enc_original_title}&href={HttpUtility.UrlEncode(similar.href)}";

                                stpl.Append(similar.title, similar.year, string.Empty, link, PosterApi.Size(similar.img));
                            }

                            return stpl;
                        });
                    }

                    href = search.Value.href;
                }
                #endregion

                var cache = await InvokeCacheResult<Model>($"pizdatoehd:{href}", 15, async e =>
                {
                    var result = await page.GotoAsync($"{init.host}/{href}", new PageGotoOptions() { WaitUntil = WaitUntilState.DOMContentLoaded });
                    if (result == null)
                        return e.Fail("не удалось загрузить страницу", refresh_proxy: true);

                    string html = await result.TextAsync();
                    if (string.IsNullOrEmpty(html))
                        return e.Fail("не удалось получить содержимое страницы");

                    var content = oninvk.Embed(href, html);
                    if (content == null)
                        return e.Fail("не удалось распарсить страницу");

                    return e.Success(content);
                });

                if (cache.Value?.IsEmpty == true)
                    return ShowError(cache.Value.content);

                return ContentTpl(cache,
                    () => oninvk.Tpl(cache.Value, accsArgs(string.Empty), title, original_title, s, href, rjson)
                );
            }
        }

        #region Movie
        [HttpGet]
        [Route("lite/pizdatoehd/movie")]
        [Route("lite/pizdatoehd/movie.m3u8")]
        async public Task<ActionResult> Movie(string title, string original_title, string voice, long id, int t, int director = 0, int s = -1, int e = -1, string favs = null, bool play = false)
        {
            if (await IsRequestBlocked(rch: false))
                return badInitMsg;

            using (var browser = new PlaywrightBrowser(init.priorityBrowser))
            {
                var page = await browser.NewPageAsync(init.plugin, init.headers, proxy: proxy_data, imitationHuman: true).ConfigureAwait(false);
                if (page == null)
                    return OnError("page");

                var cache = await InvokeCacheResult<MovieModel>(ipkey($"pizdatoehd:movie:{voice}"), 20, async e =>
                {
                    var result = await page.GotoAsync($"{init.host}/{voice}", new PageGotoOptions() { WaitUntil = WaitUntilState.DOMContentLoaded });
                    if (result == null)
                        return e.Fail("не удалось загрузить страницу", refresh_proxy: true);

                    string html = await result.TextAsync();
                    if (string.IsNullOrEmpty(html))
                        return e.Fail("не удалось получить содержимое страницы");

                    var content = oninvk.Movie(html);
                    if (content == null)
                        return e.Fail("не удалось распарсить страницу");

                    return e.Success(content);
                });

                //if (md == null)
                //{
                //    md = await InvokeCache(ipkey($"rezka:view:get_cdn_series:{id}:{t}:{director}:{s}:{e}"), 20,
                //        () => oninvk.Movie(id, t, director, s, e, favs),
                //        textJson: true
                //    );
                //}

                if (cache.Value?.links == null || cache.Value.links.Count == 0)
                    return OnError();

                string result = oninvk.Movie(cache.Value, title, original_title, play, vast: init.vast);
                if (result == null)
                    return OnError();

                if (play)
                    return RedirectToPlay(result);

                return ContentTo(result);
            }
        }
        #endregion
    }
}
