using System;
using System.Collections.Generic;
using System.Linq;

namespace UpToYou.Core {

internal static class 
    CollectionsEx {

    public static IEnumerable<T>
    Reversed<T>(this IList<T> col) {
        for (int i = col.Count - 1; i >=0; i--) 
            yield return col[i];

    }

    public static void
    ForEach<T>(this IEnumerable<T> en, Action<T> action) {
        foreach (var el in en) 
            action(el);
    }

    public static IEnumerable<T> 
    ToSingleEnumerable<T>(this T obj) {
        yield return obj;
    }

    public static IEnumerable<T>
    NotNull<T>(this IEnumerable<T?> en) where T : class {
        foreach (var el in en) {
            if (el != null)
                yield return el;
        }
    }

    public static IEnumerable<T> 
    NotEqual<T>(this IEnumerable<T> items, T item) => items.Where(x => !Equals(x, item));

    public static bool
    TryFind<T>(this IEnumerable<T> items, Func<T, bool> predicate, out T res) {
        foreach (var item in items)
            if (predicate(item)) {
                res = item;
                return true;
            }
#pragma warning disable CS8653 // A default expression introduces a null value for a type parameter.
        res = default;
#pragma warning restore CS8653 // A default expression introduces a null value for a type parameter.
        return false;
    }

    public static List<K> 
    MapToList<T, K>(this IEnumerable<T> items, Func<T, K> convert) => items.Select(convert).ToList();

    public static bool 
    ContainsAny<T>(this IEnumerable<T> items, IEnumerable<T> other) => items.Any(arg => other.Contains(arg));

    public static List<T>
    ToSingleItemList<T>(this T item) => new List<T> {item};

    public static IEnumerable<T>
    Except<T>(this IEnumerable<T> items, Func<T, bool> predicate) => items.Where(x => !predicate(x));

  

    public static IEnumerable<TSource> 
    DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) =>
        source.DistinctBy(keySelector, null);
        
    public static IEnumerable<TSource> 
    DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

        return _(); IEnumerable<TSource> _() {
            var knownKeys = new HashSet<TKey>(comparer);
            foreach (var element in source)
                if (knownKeys.Add(keySelector(element)))
                    yield return element;
        }
    }

    public static HashSet<TSource> 
    ToHashSet<TSource>(this IEnumerable<TSource> source) => 
        source.ToHashSet(null);

    public static HashSet<TSource> 
    ToHashSet<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource>? comparer) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return new HashSet<TSource>(source, comparer);
    }

    public interface 
    IExtremaEnumerable<out T> : IEnumerable<T> {
        IEnumerable<T> Take(int count);
        IEnumerable<T> TakeLast(int count);
    }

    public static TSource
    MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) where TKey:IComparable<TKey>
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (selector == null) throw new ArgumentNullException(nameof(selector));

#pragma warning disable CS8653 // A default expression introduces a null value for a type parameter.
        TSource minValueEl = default;
        TKey minValue = default;
#pragma warning restore CS8653 // A default expression introduces a null value for a type parameter.
        foreach (var el in source)
        {
            if (Equals(minValueEl, default) || selector(el).CompareTo(minValue) < 0) {
                minValueEl = el;
                minValue = selector(el);
            }
        }
        return minValueEl;
        
    }

    public static IEnumerable<TSource> 
    TakeUntil<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        return _(); IEnumerable<TSource> _() {
            foreach (var item in source) {
                yield return item;
                if (predicate(item))
                    yield break;
            }
        }
    }




}

}