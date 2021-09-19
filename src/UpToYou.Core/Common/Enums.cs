using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpToYou.Core{

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
    
}
