using Microsoft.AspNetCore.Mvc;
using Shared.Attributes;
using System.Net.Http;

namespace SISI.Controllers
{
    public class Chaturbate : BaseSisiController
    {
        static readonly HttpClient http2Client = FriendlyHttp.CreateHttp2Client();

        public Chaturbate() : base(ModInit.siteConf.Chaturbate)
        {
            requestInitialization += () =>
            {
                if (init.httpversion == 2)
                    httpHydra.RegisterHttp(http2Client);
            };
        }

        [HttpGet]
        [Staticache(6)]
        [Route("chu")]
        async public Task<ActionResult> Index(string search, string sort, int pg = 1)
        {
            if (!string.IsNullOrEmpty(search))
                return OnError("no search", false);

            if (await IsRequestBlocked(rch: true, rch_keepalive: -1))
                return badInitMsg;

            rhubFallback:
            var cache = await InvokeCacheResult($"Chaturbate:list:{sort}:{pg}", 5, jsonContext.ListPlaylistItem, async e =>
            {
                List<PlaylistItem> playlists = null;

                await httpHydra.GetSpan(ChaturbateTo.Uri(init.host, sort, pg), span =>
                {
                    playlists = ChaturbateTo.Playlist("chu/potok", span);
                });

                if (playlists == null || playlists.Count == 0)
                    return e.Fail("playlists", refresh_proxy: true);

                return e.Success(playlists);
            });

            if (IsRhubFallback(cache))
                goto rhubFallback;

            return PlaylistResult(cache, ChaturbateTo.Menu(host, sort));
        }


        [HttpGet]
        [Staticache(11)]
        [Route("chu/potok")]
        async public Task<ActionResult> Index(string baba)
        {
            if (await IsRequestBlocked(rch: true))
                return badInitMsg;

            rhubFallback:
            var cache = await InvokeCacheResult($"chaturbate:stream:{baba}", 10, jsonContext.DictionaryStringString, async e =>
            {
                string url = ChaturbateTo.StreamLinksUri(init.host, baba);
                if (url == null)
                    return e.Fail("baba");

                Dictionary<string, string> stream_links = null;

                await httpHydra.GetSpan(url, span =>
                {
                    stream_links = ChaturbateTo.StreamLinks(span);
                });

                if (stream_links == null || stream_links.Count == 0)
                    return e.Fail("stream_links", refresh_proxy: true);

                return e.Success(stream_links);
            });

            if (IsRhubFallback(cache))
                goto rhubFallback;

            return OnResult(cache);
        }
    }
}
