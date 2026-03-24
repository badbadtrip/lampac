using Microsoft.AspNetCore.Mvc;
using Shared.Attributes;
using Shared.PlaywrightCore;

namespace SISI.Controllers
{
    public class BongaCams : BaseSisiController
    {
        public BongaCams() : base(ModInit.siteConf.BongaCams) { }

        [HttpGet]
        [Staticache(6)]
        [Route("bgs")]
        async public Task<ActionResult> Index(string search, string sort, int pg = 1)
        {
            if (!string.IsNullOrEmpty(search))
                return OnError("no search", false);

            if (await IsRequestBlocked(rch: true, rch_keepalive: -1))
                return badInitMsg;

            rhubFallback:
            var cache = await InvokeCacheResult<(List<PlaylistItem> playlists, int total_pages)>($"BongaCams:list:{sort}:{pg}", 5, async e =>
            {
                string url = BongaCamsTo.Uri(init.host, sort, pg);

                int total_pages = 1;
                List<PlaylistItem> playlists = null;

                if (rch?.enable == true || init.priorityBrowser == "http")
                {
                    await httpHydra.GetSpan(url, span =>
                    {
                        playlists = BongaCamsTo.Playlist(span, out total_pages);
                    });
                }
                else
                {
                    string html = await PlaywrightBrowser.Get(init, init.cors(url), httpHeaders(init), proxy_data);

                    playlists = BongaCamsTo.Playlist(html, out total_pages);
                }

                if (playlists == null || playlists.Count == 0)
                    return e.Fail("playlists", refresh_proxy: true);

                return e.Success((playlists, total_pages));
            });

            if (IsRhubFallback(cache))
                goto rhubFallback;

            if (!cache.IsSuccess)
                return OnError(cache.ErrorMsg);

            return PlaylistResult(
                cache.Value.playlists,
                cache.ISingleCache,
                BongaCamsTo.Menu(host, sort),
                total_pages: cache.Value.total_pages
            );
        }


        //[HttpGet]
        //[Route("bgs/potok.m3u8")]
        //async public Task<ActionResult> Index(string baba)
        //{
        //    if (!ModInit.conf.BongaCams.enable)
        //        return OnError("disable");

        //    string memKey = $"bongacams:stream:{baba}";
        //    if (memoryCache.TryGetValue(memKey, out string hls))
        //        return Redirect(HostStreamProxy(ModInit.conf.BongaCams.streamproxy, hls));

        //    var root = await HttpClient.Post<Amf>(
        //               $"{ModInit.conf.BongaCams.host}/tools/amf.php?x-country=ua&res=1061112?{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}", $"method=getRoomData&args%5B%5D={baba}&args%5B%5D=&args%5B%5D=",
        //               useproxy: ModInit.conf.BongaCams.useproxy,
        //               addHeaders: new List<(string name, string val)>()
        //    {
        //                ("dnt", "1"),
        //                //("referer", ModInit.conf.BongaCams.host),
        //                ("sec-fetch-dest", "empty"),
        //                ("sec-fetch-mode", "cors"),
        //                ("sec-fetch-site", "same-origin"),
        //                ("x-requested-with", "XMLHttpRequest"),
        //                ("x-ab-split-group", "5645da7355b7d0ac0590e38a54d1d996f6754e425c1709e4420e1c68d90620315932e5bef13fd38e"),
        //                   //("cookie", "bonga20120608=dcf21cf81fc13991e8f999c26126a857; ts_type2=1; fv=ZGR5BGL0AGV2ZD==; uh=GH5AAJMvIy9bLzkmDaSZsyOTsxg6Zt==; sg=501; BONGA_REF=https%3A%2F%2Fwww.google.com%2F; reg_ver2=3; warning18=%5B%22ru_RU%22%5D; __ti=H4sIAAAAAAACAyWIOw6AIBBEr2KmJ9ldIcbZ05BIQa3BgnB3Fav3GcNhymQUXZKETYKaGLgrT8cBTt6lNjB-ev3LWB1teufK7FGVtb-dHxKMhapUAAAA; __asc=6527103917a758e97aa2f42fa81; __auc=6527103917a758e97aa2f42fa81; _ga=GA1.2.901307154.1625469917; _gid=GA1.2.1041270203.1625469917; _gat_gtag_UA_10874655_24=1; _gat_gtag_UA_10874655_62=1; tj0ffcjy9e=1802827793"),
        //    });

        //    if (string.IsNullOrWhiteSpace(root?.localData?.videoServerUrl))
        //        return OnError("baba");

        //    hls = $"http:{root.localData.videoServerUrl}/hls/stream_{baba}/public-aac/stream_{baba}/chunks.m3u8";
        //    memoryCache.Set(memKey, hls, DateTime.Now.AddMinutes(AppInit.conf.multiaccess ? 10 : 5));

        //    return Redirect(HostStreamProxy(ModInit.conf.BongaCams.streamproxy, hls));
        //}
    }
}
