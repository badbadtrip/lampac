using Newtonsoft.Json;
using Shared.Services.Pools.Json;
using System.Collections;
using System.IO.Compression;
using System.Text;

namespace Shared.Services.Utilities
{
    public static class JsonHelper
    {
        static readonly Serilog.ILogger Log = Serilog.Log.ForContext("SourceContext", nameof(JsonHelper));

        #region ListReader
        public static async Task<List<T>> ListReader<T>(string filePath, int capacity = 0)
        {
            var items = new List<T>(capacity);

            await using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, PoolInvk.bufferSize, options: FileOptions.Asynchronous))
            {
                using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress, leaveOpen: true))
                {
                    using (var reader = new JsonStreamReaderPool(gzipStream, Encoding.UTF8, leaveOpen: true))
                    {
                        using (var jsonReader = new JsonTextReader(reader))
                        {
                            var serializer = new JsonSerializer();
                            while (jsonReader.Read())
                            {
                                if (jsonReader.TokenType == JsonToken.StartObject)
                                {
                                    try
                                    {
                                        items.Add(serializer.Deserialize<T>(jsonReader));
                                    }
                                    catch (System.Exception ex)
                                    {
                                        Log.Error(ex, "CatchId={CatchId}", "id_pz0shzjo");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return items;
        }
        #endregion

        #region IEnumerableReader
        public static IEnumerable<T> IEnumerableReader<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return Enumerable.Empty<T>();

            return new JsonItemEnumerable<T>(filePath);
        }
        #endregion


        #region [Codex AI] JsonItemEnumerable<T>
        private class JsonItemEnumerable<T> : IEnumerable<T>
        {
            readonly string filePath;

            public JsonItemEnumerable(string filePath)
            {
                this.filePath = filePath;
            }

            public IEnumerator<T> GetEnumerator() => new JsonItemEnumerator(filePath);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private class JsonItemEnumerator : IEnumerator<T>
            {
                readonly string filePath;
                readonly JsonSerializer serializer = new JsonSerializer();

                FileStream fileStream;
                GZipStream gzipStream;
                StreamReader reader;
                JsonTextReader jsonReader;

                public JsonItemEnumerator(string filePath)
                {
                    this.filePath = filePath;
                    Initialize();
                }

                public T Current { get; private set; }

                object IEnumerator.Current => Current;

                void Initialize()
                {
                    try
                    {
                        fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: PoolInvk.bufferSize, options: FileOptions.Asynchronous);
                        gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
                        reader = new StreamReader(gzipStream, Encoding.UTF8, false, PoolInvk.bufferSize);
                        jsonReader = new JsonTextReader(reader);
                    }
                    catch
                    {
                        Dispose();
                    }
                }

                public bool MoveNext()
                {
                    if (jsonReader == null)
                        return false;

                    while (jsonReader.Read())
                    {
                        if (jsonReader.TokenType == JsonToken.StartObject)
                        {
                            try
                            {
                                Current = serializer.Deserialize<T>(jsonReader);
                                return true;
                            }
                            catch (System.Exception ex)
                            {
                                Log.Error(ex, "CatchId={CatchId}", "id_qccuip0u");
                            }
                        }
                    }

                    Current = default;
                    return false;
                }

                public void Reset() => throw new NotSupportedException();

                public void Dispose()
                {
                    jsonReader?.Close();
                    reader?.Dispose();
                    gzipStream?.Dispose();
                    fileStream?.Dispose();
                }
            }
        }
        #endregion
    }
}
