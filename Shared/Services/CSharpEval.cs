using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyModel;
using Shared.Models.Module;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Text.Json;

namespace Shared.Services
{
    public static class CSharpEval
    {
        static ConcurrentDictionary<string, dynamic> scripts = new();

        #region Execute<T>
        public static T Execute<T>(string cs, object model, ScriptOptions options = null)
        {
            return ExecuteAsync<T>(cs, model, options).GetAwaiter().GetResult();
        }

        public static Task<T> ExecuteAsync<T>(string cs, object model, ScriptOptions options = null)
        {
            var entry = scripts.GetOrAdd(CrypTo.md5(cs), _ =>
            {
                if (options == null)
                    options = ScriptOptions.Default;

                options = options.AddReferences(typeof(Console).Assembly).AddImports("System")
                                 .AddReferences(typeof(HttpUtility).Assembly).AddImports("System.Web")
                                 .AddReferences(typeof(Enumerable).Assembly).AddImports("System.Linq")
                                 .AddReferences(typeof(List<>).Assembly).AddImports("System.Collections.Generic")
                                 .AddReferences(typeof(Regex).Assembly).AddImports("System.Text.RegularExpressions");

                return CSharpScript.Create<T>(
                    cs,
                    options,
                    globalsType: model.GetType(),
                    assemblyLoader: new InteractiveAssemblyLoader()
                ).CreateDelegate();
            });

            return entry(model);
        }
        #endregion

        #region BaseExecute<T>
        public static T BaseExecute<T>(string cs, object model, ScriptOptions options = null, InteractiveAssemblyLoader loader = null)
        {
            return BaseExecuteAsync<T>(cs, model, options, loader).GetAwaiter().GetResult();
        }

        public static Task<T> BaseExecuteAsync<T>(string cs, object model, ScriptOptions options = null, InteractiveAssemblyLoader loader = null)
        {
            var entry = scripts.GetOrAdd(CrypTo.md5(cs), _ =>
            {
                return CSharpScript.Create<T>(
                    cs,
                    options,
                    globalsType: model.GetType(),
                    assemblyLoader: loader
                ).CreateDelegate();
            });

            return entry(model);
        }
        #endregion

        #region Execute
        public static void Execute(string cs, object model, ScriptOptions options = null)
        {
            ExecuteAsync(cs, model, options).GetAwaiter().GetResult();
        }

        public static Task ExecuteAsync(string cs, object model, ScriptOptions options = null)
        {
            var entry = scripts.GetOrAdd(CrypTo.md5(cs), _ =>
            {
                if (options == null)
                    options = ScriptOptions.Default;

                options = options.AddReferences(typeof(Console).Assembly).AddImports("System")
                                 .AddReferences(typeof(HttpUtility).Assembly).AddImports("System.Web")
                                 .AddReferences(typeof(Enumerable).Assembly).AddImports("System.Linq")
                                 .AddReferences(typeof(List<>).Assembly).AddImports("System.Collections.Generic")
                                 .AddReferences(typeof(Regex).Assembly).AddImports("System.Text.RegularExpressions");

                return CSharpScript.Create(
                    cs,
                    options,
                    globalsType: model.GetType(),
                    assemblyLoader: new InteractiveAssemblyLoader()
                ).CreateDelegate();
            });

            return entry(model);
        }
        #endregion


        #region Compilation
        public static List<PortableExecutableReference> appReferences;
        static readonly object lockCompilationObj = new();

        public static (Assembly assembly, AssemblyLoadContext alc, string path) Compilation(RootModule mod)
        {
            lock (lockCompilationObj)
            {
                string path = Path.Combine(AppContext.BaseDirectory, mod.path, mod.name);

                if (Directory.Exists(path))
                {
                    #region syntaxTree
                    var syntaxTree = new List<SyntaxTree>();
                    var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);

                    foreach (string file in Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories))
                    {
                        string _file = file.Replace("\\", "/").Replace(path.Replace("\\", "/"), "").Replace(AppContext.BaseDirectory.Replace("\\", "/"), "");
                        if (Regex.IsMatch(_file, "(\\.vs|bin|obj|Properties)/", RegexOptions.IgnoreCase))
                            continue;

                        using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: PoolInvk.bufferSize, options: FileOptions.Asynchronous))
                        {
                            var sourceText = SourceText.From(fileStream, Encoding.UTF8);
                            syntaxTree.Add(CSharpSyntaxTree.ParseText(sourceText, parseOptions, file));
                        }
                    }
                    #endregion

                    #region references
                    if (mod.references != null)
                    {
                        foreach (string refns in mod.references)
                        {
                            string dlrns = Path.Combine(AppContext.BaseDirectory, mod.path, mod.name, refns);

                            if (refns.EndsWith("/"))
                            {
                                foreach (string dlPath in Directory.GetFiles(dlrns, "*.dll"))
                                {
                                    if (appReferences.FirstOrDefault(a => a.FilePath == dlPath) == null)
                                    {
                                        var assembly = Assembly.LoadFrom(dlPath);
                                        appReferences.Add(MetadataReference.CreateFromFile(assembly.Location));
                                    }
                                }
                            }
                            else if (File.Exists(dlrns))
                            {
                                if (appReferences.FirstOrDefault(a => a.FilePath == dlrns) == null)
                                {
                                    var assembly = Assembly.LoadFrom(dlrns);
                                    appReferences.Add(MetadataReference.CreateFromFile(assembly.Location));
                                }
                            }
                            else
                            {
                                var dependencyContext = DependencyContext.Default;

                                foreach (var library in dependencyContext.RuntimeLibraries.SelectMany(library => library.GetDefaultAssemblyNames(dependencyContext)))
                                {
                                    if (library.Name.Equals(refns, StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (!appReferences.Any(r => string.Equals(Path.GetFileNameWithoutExtension(r.FilePath), library.Name, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            var assembly = Assembly.Load(library);
                                            appReferences.Add(MetadataReference.CreateFromFile(assembly.Location));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    var (generatorAssembly, generatorAssemblyPath) = EnsureRuntimeGeneratorReferences();
                    var compilation = CSharpCompilation.Create(mod.name, syntaxTree, references: appReferences, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                    #region JsonSourceGenerator
                    var generatorType = generatorAssembly?.GetType("System.Text.Json.SourceGeneration.JsonSourceGenerator", throwOnError: false, ignoreCase: false);

                    var generators = new List<ISourceGenerator>();
                    if (generatorType != null && Activator.CreateInstance(generatorType) is object generatorInstance)
                    {
                        if (generatorInstance is ISourceGenerator sourceGenerator)
                            generators.Add(sourceGenerator);
                        else if (generatorInstance is IIncrementalGenerator incrementalGenerator)
                            generators.Add(incrementalGenerator.AsSourceGenerator());
                    }

                    Compilation updatedCompilation = compilation;

                    if (generators.Count > 0)
                    {
                        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators, parseOptions: parseOptions);
                        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
                        updatedCompilation = outputCompilation;

                        var runResult = driver.GetRunResult();

                        foreach (var diagnostic in generatorDiagnostics)
                            Console.WriteLine($"[{mod.name}][JsonSourceGenerator][driver] {diagnostic}");

                        foreach (var diagnostic in runResult.Diagnostics)
                            Console.WriteLine($"[{mod.name}][JsonSourceGenerator][run] {diagnostic}");

                        foreach (var result in runResult.Results)
                        {
                            if (result.GeneratedSources.Length > 0)
                            {
                                Console.WriteLine($"[{mod.name}][JsonSourceGenerator][{result.Generator.GetType().FullName}] generated: {result.GeneratedSources.Length}");
                                foreach (var diagnostic in result.Diagnostics)
                                    Console.WriteLine($"[{mod.name}][JsonSourceGenerator][{result.Generator.GetType().FullName}] {diagnostic}");
                            }
                        }
                    }
                    #endregion

                    using (var ms = PoolInvk.msm.GetStream())
                    {
                        var result = updatedCompilation.Emit(ms);

                        if (result.Success)
                        {
                            ms.Seek(0, SeekOrigin.Begin);

                            var alc = new AssemblyLoadContext(mod.name, isCollectible: true);
                            var assembly = alc.LoadFromStream(ms);

                            return (assembly, alc, path);
                        }
                        else
                        {
                            Console.WriteLine($"\ncompilation error: {mod.name}");
                            foreach (var diagnostic in result.Diagnostics)
                            {
                                if (diagnostic.Severity == DiagnosticSeverity.Error)
                                    Console.WriteLine(diagnostic);
                            }
                            Console.WriteLine("\n");
                        }
                    }

                }

                return default;
            }
        }
        #endregion

        #region JsonSourceGenerator
        static bool HasReference(string assemblyPath)
            => appReferences.Any(r => string.Equals(r.FilePath, assemblyPath, StringComparison.OrdinalIgnoreCase));

        static string FindJsonGeneratorAssemblyPath()
        {
            var loadedAssemblyPath = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, "System.Text.Json.SourceGeneration", StringComparison.OrdinalIgnoreCase))
                ?.Location;

            if (!string.IsNullOrEmpty(loadedAssemblyPath) && File.Exists(loadedAssemblyPath))
                return loadedAssemblyPath;

            string dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            if (string.IsNullOrEmpty(dotnetRoot))
            {
                var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
                dotnetRoot = Path.GetFullPath(Path.Combine(runtimeDir, "..", "..", ".."));
            }

            var packsPath = Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref");
            if (!Directory.Exists(packsPath))
                return null;

            var generatorPath = Directory.GetDirectories(packsPath).Select(versionDir => new
            {
                Path = versionDir,
                Version = Version.TryParse(Path.GetFileName(versionDir), out var parsedVersion)
                        ? parsedVersion
                        : new Version(0, 0)
            })
                .OrderByDescending(versionDir => versionDir.Version)
                .Select(versionDir => Path.Combine(versionDir.Path, "analyzers", "dotnet", "cs", "System.Text.Json.SourceGeneration.dll"))
                .FirstOrDefault(File.Exists);

            return generatorPath;
        }

        static (Assembly generatorAssembly, string generatorAssemblyPath) EnsureRuntimeGeneratorReferences()
        {
            var jsonAssemblyPath = typeof(JsonSerializer).Assembly.Location;
            if (!string.IsNullOrEmpty(jsonAssemblyPath) && !HasReference(jsonAssemblyPath))
                appReferences.Add(MetadataReference.CreateFromFile(jsonAssemblyPath));

            var generatorAssemblyPath = FindJsonGeneratorAssemblyPath();
            Assembly generatorAssembly = null;

            if (!string.IsNullOrEmpty(generatorAssemblyPath))
            {
                generatorAssembly = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.Location, generatorAssemblyPath, StringComparison.OrdinalIgnoreCase));

                if (generatorAssembly == null)
                {
                    try
                    {
                        generatorAssembly = Assembly.LoadFrom(generatorAssemblyPath);
                    }
                    catch (System.Exception ex)
                    {
                        Serilog.Log.Error(ex, "{Class} {CatchId}", "CSharpEval", "id_puhbziqk");
                    }
                }

            }

            return (generatorAssembly, generatorAssemblyPath);
        }
        #endregion
    }
}
