using Newtonsoft.Json;

namespace Shared.Services.Pools.Json
{
    public static class JsonDefaultSerializerPool
    {
        [ThreadStatic]
        private static JsonSerializer _instance;

        public static JsonSerializer Instance
            => (_instance ??= JsonSerializer.CreateDefault());
    }
}
