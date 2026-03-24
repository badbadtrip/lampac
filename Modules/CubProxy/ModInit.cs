using CubProxy.Controllers;
using Shared;
using Shared.Services;
using Shared.Models.AppConf;
using Shared.Models.Events;
using Shared.Models.Module;
using Shared.Models.Module.Interfaces;
using System.Collections.Generic;
using System.IO;

namespace CubProxy
{
    public class ModInit : IModuleLoaded
    {
        public static string modpath;
        static FileSystemWatcher fileWatcher;

        public static ModuleConf conf;

        public void Loaded(InitspaceModel baseconf)
        {
            modpath = baseconf.path;

            updateConf();
            EventListener.UpdateInitFile += updateConf;

            foreach (var m in conf.limit_map)
                CoreInit.conf.WAF.limit_map.Insert(0, m);

            string path = Path.Combine("cache", "cub");
            Directory.CreateDirectory(path);

            foreach (var file in new DirectoryInfo(path).EnumerateFiles("*", new EnumerationOptions
            {
                RecurseSubdirectories = false, // Не заходить в подкаталоги. Перечисляются только файлы в cache/hls, без вложенных папок.
                IgnoreInaccessible = true,     // Пропускает файлы/папки, к которым нет доступа, без выброса исключений
                AttributesToSkip = FileAttributes.ReparsePoint // Пропускает reparse points: symlink, junction/mount points
            }))
            {
                ApiController.cacheFiles.TryAdd(file.Name, (int)file.Length);
            }

            CoreInit.FileCacheCron.Add((path, conf.cache_img));

            fileWatcher = new FileSystemWatcher
            {
                Path = path,
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            fileWatcher.Deleted += FileWatcher_Deleted;
        }


        void FileWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            ApiController.cacheFiles.TryRemove(e.Name, out _);
        }


        void updateConf()
        {
            conf = ModuleInvoke.Init("cub", new ModuleConf()
            {
                viewru = true,
                responseContentLength = true,
                scheme = CoreInit.conf.cub.scheme,
                domain = CoreInit.conf.cub.domain,
                mirror = CoreInit.conf.cub.mirror,
                cache_api = 180,     // 3h
                cache_img = 60 * 24, // 24h
                limit_map = new List<WafLimitRootMap>()
                {
                    new("^/cub/", new WafLimitMap { limit = 50, second = 1 })
                }
            });
        }


        public void Dispose()
        {
            fileWatcher.Deleted -= FileWatcher_Deleted;
        }
    }
}
