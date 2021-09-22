using System.Text.Json;

namespace UpToYou.Backend {
public static class JsonHelper {
    
    public static T
    ParseJson<T>(this string json) => JsonSerializer.Deserialize<T>(json)!;
    
}
}
