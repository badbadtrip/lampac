using Shared;
using Shared.Services;
using Shared.Models.AppConf;
using Shared.Models.Events;
using Shared.Models.Module;
using Shared.Models.Module.Interfaces;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace Sync
{
    public class ModInit : IModuleLoaded, IModuleConfigure
    {
        public static string modpath;
        public static ModuleConf conf;

        public void Configure(ConfigureModel app)
        {
            app.services.AddDbContextFactory<SqlContext>(SqlContext.ConfiguringDbBuilder);
        }

        public void Loaded(InitspaceModel baseconf)
        {
            modpath = baseconf.path;

            updateConf();
            EventListener.UpdateInitFile += updateConf;
            SyncNwsEvents.Start();

            foreach (var m in conf.limit_map)
                CoreInit.conf.WAF.limit_map.Insert(0, m);

            Directory.CreateDirectory("database/storage");
            Directory.CreateDirectory("database/storage/temp");

            SqlContext.Initialization(baseconf.app.ApplicationServices);
        }

        void updateConf()
        {
            conf = ModuleInvoke.Init("Sync", new ModuleConf()
            {
                storageTemp = false,
                limit_map = new List<WafLimitRootMap>()
                {
                    new("^/(storage|bookmark)/", new WafLimitMap { limit = 10, second = 1 })
                }
            });
        }

        public void Dispose()
        {
            SyncNwsEvents.Stop();
            EventListener.UpdateInitFile -= updateConf;
        }
    }
}
