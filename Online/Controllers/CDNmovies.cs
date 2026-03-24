using Microsoft.AspNetCore.Mvc;
using Shared.Attributes;

namespace Online.Controllers
{
    public class CDNmovies : BaseOnlineController
    {
        public CDNmovies() : base(ModInit.siteConf.CDNmovies) { }

        [HttpGet]
        [Staticache(1)]
        [Route("lite/cdnmovies")]
        async public Task<ActionResult> Index(long kinopoisk_id, string title, string original_title, int t, int s = -1, int sid = -1, bool rjson = false)
        {
            if (kinopoisk_id == 0)
                return OnError();

            if (await IsRequestBlocked(rch: true))
                return badInitMsg;

            var oninvk = new CDNmoviesInvoke
            (
               host,
               init.host,
               httpHydra,
               onstreamtofile => HostStreamProxy(onstreamtofile)
            );

        rhubFallback:
            var cache = await InvokeCacheResult($"cdnmovies:view:{kinopoisk_id}", 20,
                () => oninvk.Embed(kinopoisk_id)
            );

            if (IsRhubFallback(cache))
                goto rhubFallback;

            return ContentTpl(cache,
                () => oninvk.Tpl(cache.Value, kinopoisk_id, title, original_title, t, s, sid, vast: init.vast, rjson: rjson)
            );
        }
    }
}
