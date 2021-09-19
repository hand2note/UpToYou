using System;
using System.Collections.Generic;
using System.Linq;

namespace UpToYou.Core {

public readonly struct 
RelativePath: IEquatable<RelativePath> {
    public string Value { get; }

    public RelativePath(string value) => Value = value;

    public static RelativePath
    SameLocationPath = new RelativePath("/");

    public override string ToString() => Value;
    public static bool operator == (RelativePath path1, RelativePath path2) => string.Equals(path1.Value, path2.Value, StringComparison.OrdinalIgnoreCase);
    public static bool operator !=(RelativePath path1, RelativePath path2) =>  !string.Equals(path1.Value, path2.Value, StringComparison.OrdinalIgnoreCase);
    public static implicit operator string(RelativePath path) => path.Value;

    public bool Equals(RelativePath other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    public override bool Equals(object obj) => obj is RelativePath other && Equals(other);
    public override int GetHashCode() => Value != null ? Value.ToLower().GetHashCode() : 0;
}

public interface IHasRelativePath {
    RelativePath Path { get; }
}

public static class
RelativePathHelper {

    public static string
    Copy(this RelativePath path, string sourceDirectory, string outDirectory) =>
        path.ToAbsolute(sourceDirectory).CopyFile(path.ToAbsolute(outDirectory));

    internal static List<RelativePath>
    ToRelativePathsList(this IEnumerable<string> paths) => paths.Select(x => new RelativePath(x)).ToList();

    internal static IEnumerable<RelativePath>
    ToRelativePaths(this IEnumerable<string> paths) => paths.Select(x => new RelativePath(x));

    internal static RelativePath
    ToRelativePath(this string path) => new RelativePath(path);

    public static string
    ToAbsolute(this RelativePath path, string root) => root.AppendPath(path.Value);

    internal static string
    AppendPath(this string path1, RelativePath path2) => path1.AppendPath(path2.Value);

    public static bool
    ContainsExtension(this RelativePath path, string extension) => path.Value.FileContainsExtension(extension);

    public static bool
    HasExtension(this RelativePath path, string ext) => path.Value.FileHasExtension(ext);

    internal static RelativePath
    GetPathRelativeTo(this string path, string basePath) =>
        basePath.EndsWith("/") || basePath.EndsWith("\\")
            ? new RelativePath(path.Substring(basePath.Length, path.Length - basePath.Length))
            : new RelativePath(path.Substring(basePath.Length + 1, path.Length - basePath.Length - 1));

    internal static T
    FindByPath<T>(this IEnumerable<T> items, RelativePath path) where T : IHasRelativePath =>
        items.FirstOrDefault(x => x.Path.Value == path.Value);

    internal static T
    GetByPath<T>(this IEnumerable<T> items, RelativePath path) where T : IHasRelativePath {
        if (items.TryGet(x => x.Path.Value == path.Value, out var res))
            return res;
        throw new InvalidOperationException($"Item with RelativePath={path.Value.Quoted()}");
    }

    public static bool IsParentTo(this RelativePath path, RelativePath other) => other.Value.StartsWith(path.Value);

    internal static IEnumerable<RelativePath>
    EnumerateDirectoryRelativeFiles(this string directory, bool recursive = true) => directory.GetAllDirectoryFiles(recursive).Select(x => x.GetPathRelativeTo(directory));

}

}
