using Shared;
using Shared.Models.AppConf;
using Shared.Models.Events;
using Shared.Models.Module;
using Shared.Models.Module.Interfaces;
using Shared.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TmdbProxy
{
    public class ModInit : IModuleLoaded
    {
        public static string modpath;
        public static ModuleConf conf;
        public static readonly ConcurrentDictionary<string, int> cacheFiles = new();
        static FileSystemWatcher fileWatcher;

        public void Loaded(InitspaceModel baseconf)
        {
            modpath = baseconf.path;

            updateConf();
            EventListener.UpdateInitFile += updateConf;

            foreach (var m in conf.limit_map)
                CoreInit.conf.WAF.limit_map.Insert(0, m);

            string path = Path.Combine("cache", "tmdb");
            Directory.CreateDirectory(path);

            Parallel.ForEach(Directory.GetDirectories(path), new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            },
            dir =>
            {
                foreach (var file in new DirectoryInfo(dir).EnumerateFiles("*", new EnumerationOptions
                {
                    RecurseSubdirectories = false, // Не заходить в подкаталоги
                    IgnoreInaccessible = true,     // Пропускает файлы/папки, к которым нет доступа, без выброса исключений
                    AttributesToSkip = FileAttributes.ReparsePoint // Пропускает reparse points: symlink, junction/mount points
                }))
                {
                    cacheFiles.TryAdd(file.Name, (int)file.Length);
                }
            });

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
            cacheFiles.TryRemove(Path.GetFileName(e.Name), out _);
        }


        void updateConf()
        {
            conf = ModuleInvoke.Init("tmdb", new ModuleConf()
            {
                responseContentLength = true,
                httpversion = 2,
                cache_api = 240,     // 4h
                cache_img = 60 * 14, // 14h
                limit_map = new List<WafLimitRootMap>()
                {
                    new("^/tmdb/", new WafLimitMap { limit = 50, second = 1 })
                }
            });
        }


        public void Dispose()
        {
            fileWatcher.Deleted -= FileWatcher_Deleted;
        }
    }
}
