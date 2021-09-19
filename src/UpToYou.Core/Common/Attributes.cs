using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpToYou.Core{

internal class 
FileExtensionAttribute : Attribute {
    public FileExtensionAttribute(string value) => Value = value;
    public string Value { get; }
}

internal static class Attributes {

    public static T 
    GetAttribute<T>(this object obj) =>  (T)obj.GetType().GetCustomAttributes(typeof(T), true)[0];

    public static T 
    GetEnumAttribute<T>(this object obj){
        var type = obj.GetType();
        return (T)type.GetMember(obj.ToString()).First(x => x.DeclaringType == type).GetCustomAttributes(typeof(T), false)[0];
    }

}

}
