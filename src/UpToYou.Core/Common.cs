using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UpToYou.Core
{

internal static class 
StringEx {
    public static string 
    Quoted(this string value) => "\"" + value  + "\"";

    public static string
    ToUtf8String(this byte[] bytes) =>new UTF8Encoding(false).GetString(bytes);

    public static byte[]
    Utf8ToBytes(this string str) => new UTF8Encoding(false).GetBytes(str);
}

internal interface 
IHasValue<out T> {
    T Value { get; }
}

internal class FileExtensionAttribute : Attribute, IHasValue<string> {
    public FileExtensionAttribute(string value) => Value = value;
    public string Value { get; }
}

internal static class AttributesEx {

    public static T 
    GetAttribute<T>(this object obj) =>  (T)obj.GetType().GetCustomAttributes(typeof(T), true)[0];

    public static T 
    GetEnumAttribute<T>(this object obj){
        var type = obj.GetType();
        return (T)type.GetMember(obj.ToString()).First(x => x.DeclaringType == type).GetCustomAttributes(typeof(T), false)[0];
    }

}

internal static class
EnumHelper {
    public static IEnumerable<T>
    GetValues<T>() {
        foreach (var value in Enum.GetValues(typeof(T))) 
            yield return (T)value;
    }

    public static T
    ParseEnum<T>(this string str) => (T)Enum.Parse(typeof(T), str, false);
}

internal static class
BytesExtensions {
    public static double 
    BytesToMegabytes(this long bytes, int roundingDecimals = 2) => Math.Round(bytes / 1_000_000d, roundingDecimals);

    public static double 
    BytesToMegabytes(this int bytes, int roundingDecimals = 2) => Math.Round(bytes / 1_000_000d, roundingDecimals);
}

}
