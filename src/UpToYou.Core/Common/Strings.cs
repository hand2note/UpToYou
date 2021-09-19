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
}
}
