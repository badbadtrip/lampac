using Microsoft.AspNetCore.Mvc;

namespace Online.Controllers
{
    public class Redheadsound : BaseOnlineController
    {
        public Redheadsound() : base(ModInit.siteConf.Redheadsound) { }

        [HttpGet]
        [Route("lite/redheadsound")]
        async public Task<ActionResult> Index(string title, string original_title, int year, int clarification, bool rjson = false)
        {
            if (string.IsNullOrWhiteSpace(title) || year == 0)
                return OnError();

            if (await IsRequestBlocked(rch: true))
                return badInitMsg;

            var oninvk = new RedheadsoundInvoke
            (
               host,
               init.host,
               ongettourl => httpHydra.Get(ongettourl),
               (url, data) => httpHydra.Post(url, data),
               streamfile => HostStreamProxy(streamfile)
            );

        rhubFallback:
            var cache = await InvokeCacheResult($"redheadsound:view:{title}:{year}", 30,
                () => oninvk.Embed(title, year)
            );

            if (IsRhubFallback(cache))
                goto rhubFallback;

            return ContentTpl(cache, () => oninvk.Tpl(cache.Value, title, vast: init.vast, rjson: rjson));
        }
    }
}
