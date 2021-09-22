using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpToYou.Core {
internal static class 
Strings {
    public static string 
    Quoted(this string value) => "\"" + value  + "\"";

    public static string
    ToUtf8String(this byte[] bytes) =>new UTF8Encoding(false).GetString(bytes);

    public static byte[]
    Utf8ToBytes(this string str) => new UTF8Encoding(false).GetBytes(str);
    
    public static Version 
    ParseVersion(this string version) =>
        System.Version.Parse(version);
    
    public static string
    AggregateToString<T>(this IEnumerable<T> items, string? separator = null) {
        var result = new StringBuilder();
        foreach (var item in items) {
            if (result.Length != 0 && separator != null)
                result.Append(separator);
            result.Append(item);
        }
        return result.ToString();
    }
}
}
