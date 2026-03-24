using Shared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Core.Services
{
    public static class CronCacheWatcher
    {
        static readonly Serilog.ILogger Log = Serilog.Log.ForContext("SourceContext", nameof(CronCacheWatcher));

        sealed class WatcherContext
        {
            public int Minute;
            public ConcurrentDictionary<string, DateTime> Files = new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            public FileSystemWatcher Watcher;
        }

        static readonly List<WatcherContext> _contexts = new List<WatcherContext>();
        static Timer _cronTimer;
        static int _updating = 0;

        public static void Run()
        {
            CoreInit.FileCacheCron.Add(("img", CoreInit.conf.serverproxy.image.cache_time));
            CoreInit.FileCacheCron.Add(("hls", CoreInit.conf.serverproxy.cache_hls));

            foreach (var conf in CoreInit.FileCacheCron)
            {
                try
                {
                    string path = Path.Combine("cache", conf.path);
                    if (conf.minute == -1 || !Directory.Exists(path))
                        continue;

                    var context = new WatcherContext
                    {
                        Minute = conf.minute,
                        Watcher = new FileSystemWatcher(path)
                        {
                            IncludeSubdirectories = true,
                            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size,
                            EnableRaisingEvents = true
                        }
                    };

                    foreach (var file in new DirectoryInfo(path).EnumerateFiles("*", new EnumerationOptions
                    {
                        RecurseSubdirectories = true,
                        IgnoreInaccessible = true,
                        AttributesToSkip = FileAttributes.ReparsePoint
                    }))
                    {
                        try
                        {
                            context.Files[file.FullName] = file.LastWriteTimeUtc;
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error(ex, "CatchId={CatchId}", "id_uv8f7e4q");
                        }
                    }

                    context.Watcher.Created += (_, e) => updateFile(context, e.FullPath);
                    context.Watcher.Changed += (_, e) => updateFile(context, e.FullPath);
                    context.Watcher.Renamed += (_, e) =>
                    {
                        context.Files.TryRemove(e.OldFullPath, out var _);
                        updateFile(context, e.FullPath);
                    };

                    _contexts.Add(context);
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex, "CatchId={CatchId}", "id_n5isgl46");
                }
            }

            _cronTimer = new Timer(cron, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        static void updateFile(WatcherContext context, string fullPath)
        {
            try
            {
                context.Files[fullPath] = File.GetLastWriteTimeUtc(fullPath);
            }
            catch (System.Exception ex)
            {
                Log.Error(ex, "CatchId={CatchId}", "id_bflxmkvb");
            }
        }

        static void cron(object state)
        {
            if (Interlocked.Exchange(ref _updating, 1) == 1)
                return;

            try
            {
                foreach (var context in _contexts)
                {
                    var cutoff = DateTime.UtcNow.AddMinutes(-context.Minute);

                    foreach (var item in context.Files)
                    {
                        try
                        {
                            if (context.Minute == 0 || cutoff > item.Value)
                            {
                                if (context.Files.TryRemove(item.Key, out var _))
                                    File.Delete(item.Key);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error(ex, "CatchId={CatchId}", "id_1ijbvnkf");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error(ex, "CatchId={CatchId}", "id_asjavlf6");
            }
            finally
            {
                Volatile.Write(ref _updating, 0);
            }
        }
    }
}
