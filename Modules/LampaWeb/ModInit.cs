using Shared;
using Shared.Services;
using Shared.Models.AppConf;
using Shared.Models.Events;
using Shared.Models.Module;
using Shared.Models.Module.Interfaces;
using System.Collections.Generic;
using LampaWeb.Models;
using LampaWeb.Services;

namespace LampaWeb
{
    public class ModInit : IModuleLoaded
    {
        public static string modpath;

        public static ModuleConf conf;

        public void Loaded(InitspaceModel baseconf)
        {
            modpath = baseconf.path;

            updateConf();
            EventListener.UpdateInitFile += updateConf;

            foreach (var m in conf.limit_map)
                CoreInit.conf.WAF.limit_map.Insert(0, m);

            LampaCron.Start();
        }

        public void Dispose()
        {
            LampaCron.Stop();
            EventListener.UpdateInitFile -= updateConf;
        }

        void updateConf()
        {
            conf = ModuleInvoke.Init("LampaWeb", new ModuleConf()
            {
                autoupdate = true,
                intervalupdate = 90, // minute
                basetag = true,
                index = "lampa-main/index.html",
                git = "yumata/lampa",
                tree = "63650dc132142e50c83ca33ae17e9c1daeed41c6",
                limit_map = new List<WafLimitRootMap>()
                {
                    new("^/(extensions|testaccsdb|msx/)", new WafLimitMap { limit = 10, second = 1 })
                }
            });
        }
    }
}
