using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Web;
using Shared;
using Shared.Models.SISI.Base;

namespace Lamson.Controllers
{
    public class PornGram : BaseSisiController
    {
        public PornGram() : base(ModInit.PornGram) { }

        [HttpGet]
        [Route("porngram")]
        async public Task<ActionResult> Index(string search, string sort, int pg = 1)
        {
            if (await IsRequestBlocked(rch: false))
                return badInitMsg;

            List<PlaylistItem> playlists = null;

            string memKey = $"porngram:list:{search}:{sort}:{pg}";
            var entryCache = await hybridCache.EntryAsync<List<PlaylistItem>>(memKey);

            if (entryCache.success)
            {
                playlists = entryCache.value;
            }
            else
            {
                // await http
                // пасинг

                playlists = new List<PlaylistItem>();

                for (int i = 0; i < 24; i++)
                {
                    playlists.Add(new PlaylistItem()
                    {
                        name = "Tomsk Theater Square",
                        video = $"{host}/porngram/video?href={HttpUtility.UrlEncode("https://www.elecard.com/ru/videos")}",
                        picture = HostImgProxy(init, "https://www.elecard.com/storage/thumbs/1_1280x_FFFFFF/images/Video%20Previews/TheaterSquare_640x360.jpg"),
                        time = "9:15",
                        quality = "4K",
                        json = true
                    });
                }

                proxyManager?.Success();
                hybridCache.Set(memKey, playlists, cacheTime(20));
            }

            var menu = new List<MenuItem>()
            {
                new MenuItem()
                {
                    title = "Поиск",
                    search_on = "search_on",
                    playlist_url = host + "/porngram",
                },
                new MenuItem()
                {
                    title = $"Сортировка: {(string.IsNullOrEmpty(sort) ? "новинки" : sort)}",
                    playlist_url = "submenu",
                    submenu = new List<MenuItem>()
                    {
                        new MenuItem()
                        {
                            title = "Новинки",
                            playlist_url = host + "/porngram"
                        },
                        new MenuItem()
                        {
                            title = "Лучшее",
                            playlist_url = host + $"/porngram?sort=porno-online"
                        },
                        new MenuItem()
                        {
                            title = "Популярное",
                            playlist_url = host + $"/porngram?sort=xxx-top"
                        }
                    }
                }
            };

            return PlaylistResult(playlists, false, menu);
        }


        [HttpGet]
        [Route("porngram/video")]
        async public Task<ActionResult> Video(string href)
        {
            if (await IsRequestBlocked(rch: false))
                return badInitMsg;

            string memKey = $"porngram:video:{href}";
            var entryCache = await hybridCache.EntryAsync<Dictionary<string, string>>(memKey);
            if (entryCache.success)
                return OnResult(entryCache.value);

            // await http
            // пасинг

            var stream_links = new Dictionary<string, string>()
            {
                ["2160p"] = "https://www.elecard.com/storage/video/TheaterSquare_3840x2160.mp4",
                ["1080p"] = "https://www.elecard.com/storage/video/TheaterSquare_1920x1080.mp4",
                ["720p"] = "https://www.elecard.com/storage/video/TheaterSquare_1280x720.mp4"
            };

            proxyManager?.Success();
            hybridCache.Set(memKey, stream_links, cacheTime(10));

            return OnResult(stream_links);
        }
    }
}
