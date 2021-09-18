using System;
using System.Collections.Generic;
using System.Linq;

namespace UpToYou.Core {

internal interface IHasUniqueId {
    string Id { get; }
}

internal static class UniqueId {

    public const int UniqueIdLength = 8;

    public static string 
    NewUniqueId() =>  Guid.NewGuid().ToString().Substring(0,UniqueIdLength).Replace("-", "");

    public static string
    NewUniqueId(HashSet<string>? existingIds) {
        string res = NewUniqueId();
        if (existingIds == null)
            return res;
        while(existingIds.Contains(res))
            res = NewUniqueId();
        return res;
    }

    public static string
    NewUniqueId(int maxLength) => Guid.NewGuid().ToString().Replace("-", "").Substring(0, maxLength);

    public static T FindById<T>(this IEnumerable<T> en, string id) where T : IHasUniqueId =>
        en.FirstOrDefault(x => x.Id == id);

    public static T GetById<T>(this IEnumerable<T> en, string id) where T : IHasUniqueId {
        var item = en.FirstOrDefault(x => x.Id == id);
        if (Equals(item, default))
            throw new InvalidOperationException($"Item with id = {id.Quoted()} not found");
        return item;
    }

    public static IEnumerable<T> 
    NotEqualById<T>(this IEnumerable<T> items, T other) where T : IHasUniqueId => 
        items.Where(x => x.Id != other.Id);
}
}
