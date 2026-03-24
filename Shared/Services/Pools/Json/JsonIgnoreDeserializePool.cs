using Newtonsoft.Json;

namespace Shared.Services.Pools.Json
{
    public static class JsonIgnoreDeserializePool
    {
        [ThreadStatic]
        private static JsonSerializer _instance;

        public static JsonSerializer Instance
            => (_instance ??= JsonSerializer.Create(new JsonSerializerSettings { Error = (se, ev) => { ev.ErrorContext.Handled = true; } }));
    }
}
