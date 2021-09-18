﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace UpToYou.Core {
public interface 
IHasCustomProperties {
    Dictionary<string, string>? CustomProperties { get; }
}

public static class 
CustomProperties {

    public static bool
    IsCustomPropertiesSame(this IHasCustomProperties @this, IHasCustomProperties other) {
        if (@this.CustomProperties == null && other.CustomProperties == null)
            return true;

        if (@this.CustomProperties == null)
            return other.CustomProperties == null || other.CustomProperties.Count == 0;

        if (other.CustomProperties == null)
            return @this.CustomProperties == null || @this.CustomProperties.Count == 0;

        foreach (var kv in @this.CustomProperties)
            if (!other.CustomProperties.TryGetValue(kv.Key, out var res) || !string.Equals(res,  kv.Value, StringComparison.Ordinal))
                return false;
        return true;
    }

    public static bool?
    FindCustomBoolProperty(this IHasCustomProperties provider,string key) {
        var value = provider.FindCustomProperty(key);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (bool.TryParse(value, out var res))
            return res;
        return null;
    }

    public static int?
    FindCustomIntProperty(this IHasCustomProperties provider,string key) =>
        provider.TryFindCustomProperty(key, out var prop) && int.TryParse(prop, out var res) ? (int?) res : null;

    public static bool 
    TryFindCustomProperty(this IHasCustomProperties provider, string key, out string? res) {
        if (provider.CustomProperties != null && provider.CustomProperties.TryGetValue(key, out res))
            return true;
        res = null;
        return false;
    }

    public static string?
    FindCustomProperty(this IHasCustomProperties provider, string key) {
        if (provider.CustomProperties != null && provider.CustomProperties.TryGetValue(key, out var res) && !string.IsNullOrWhiteSpace(res))
            return res.Trim();
        return null;
    }

    internal static Dictionary<string, string>? 
    AddCustomProperties(this Dictionary<string, string>? props1, Dictionary<string, string>? props2, bool @override) {
        if (props1 == null)
            return props2;
        if (props2 == null)
            return props1;

        var result = props1.ToDictionary(x => x.Key, x => x.Value);

        foreach (var kv2 in props2) {
            if (!props1.ContainsKey(kv2.Key))
                result.Add(kv2.Key, kv2.Value);
            else if (@override)
                result[kv2.Key] = kv2.Value;
        }
        return result;
    }



}
}
