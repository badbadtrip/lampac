using Jint;
using Jint.Native;
using System.Text.Json;

public static class Extensions
{
    public static T Deserialize<T>(this JsValue engine)
    {
        if (engine == null)
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(engine.AsString());
        }
        catch
        {
            return default;
        }
    }
}