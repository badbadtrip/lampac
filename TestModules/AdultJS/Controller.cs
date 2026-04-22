using Jint;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Models.SISI.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdultJS
{
    public class ApiController : BaseSisiController
    {
        Engine engine = new Engine();

        public ApiController() : base(ModInit.conf)
        {
            engine.Execute(ModInit.jsFile);
            engine.SetValue("log", new Action<object>(Console.WriteLine));
            engine.SetValue("EncryptQuery", new Func<string, string>(url => EncryptQuery(url)));
        }

        [HttpGet]
        [Route("porngram")]
        async public Task<ActionResult> Index(string search, string sort, int pg = 1)
        {
            if (await IsRequestBlocked(rch: false))
                return badInitMsg;

        rhubFallback:
            var cache = await InvokeCacheResult<Channel>($"PornGram:{search}:{sort}:{pg}", 5, async e =>
            {
                string uri = engine.Invoke("BuildUrl", host, search, sort, pg).AsString();
                string html = await httpHydra.Get(uri);
                if (html == null)
                    return e.Fail("html", refresh_proxy: true);

                var channel = engine.Invoke("Playlist", html, host, search, sort)
                    .Deserialize<Channel>();

                if (channel?.list == null || channel.list.Count == 0)
                    return e.Fail("playlists");

                return e.Success(channel);
            });

            if (IsRhubFallback(cache))
                goto rhubFallback;

            return PlaylistResult(cache);
        }


        [HttpGet]
        [Route("porngram/video")]
        async public Task<ActionResult> Video(string uri, string secret_uri)
        {
            Console.WriteLine($"secret_uri: {DecryptQuery(secret_uri)}");

            if (await IsRequestBlocked(rch: false))
                return badInitMsg;

        rhubFallback:
            var cache = await InvokeCacheResult($"PornGram:{uri}", 10, jsonContext.DictionaryStringString, async e =>
            {
                string html = await httpHydra.Get(uri);
                if (html == null)
                    return e.Fail("html", refresh_proxy: true);

                var stream_links = engine.Invoke("Video", html)
                    .Deserialize<Dictionary<string, string>>();

                return e.Success(stream_links);
            });

            if (IsRhubFallback(cache))
                goto rhubFallback;

            return OnResult(cache);
        }
    }
}
