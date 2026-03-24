using Shared.Models.Module.Interfaces;
using System.Reflection;

namespace Shared.Models.Module.Entrys
{
    public class OnlineModuleEntry
    {
        public static List<IModuleOnline> onlineModulesCache;
        static readonly object _lock = new object();

        public static void EnsureCache(bool forced = false)
        {
            if (forced == false && onlineModulesCache != null)
                return;

            lock (_lock)
            {
                if (forced == false && onlineModulesCache != null)
                    return;

                onlineModulesCache = new List<IModuleOnline>();

                try
                {
                    foreach (var mod in CoreInit.modules.Where(m => m?.assembly != null && m.enable))
                    {
                        var asm = mod.assembly;

                        IEnumerable<Type> types;

                        try
                        {
                            types = asm.GetTypes();
                        }
                        catch (ReflectionTypeLoadException rtle)
                        {
                            Serilog.Log.Error(rtle, "CatchId={CatchId}", "id_aeff5762");
                            types = rtle.Types.Where(t => t != null);
                        }
                        catch
                        {
                            continue;
                        }

                        foreach (var type in types)
                        {
                            try
                            {
                                if (!type.IsClass || type.IsAbstract)
                                    continue;

                                if (!typeof(IModuleOnline).IsAssignableFrom(type))
                                    continue;

                                // Требуется public parameterless ctor
                                var instance = Activator.CreateInstance(type) as IModuleOnline;
                                if (instance != null)
                                    onlineModulesCache.Add(instance);
                            }
                            catch
                            {
                                // игнорируем сломанные типы
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Serilog.Log.Error(ex, "{Class} {CatchId}", "OnlineModuleEntry", "id_vmvbnc5h");
                }
            }
        }

    }
}
