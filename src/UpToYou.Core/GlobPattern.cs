using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DotNet.Globbing;

namespace UpToYou.Core
{
internal static class
GlobPattern {

    private static readonly ConcurrentDictionary<string, Glob> GlobCache = new ConcurrentDictionary<string, Glob>();

    private static Glob
    CreateGlob(this string pattern) => Glob.Parse(pattern, new GlobOptions {Evaluation = {CaseInsensitive = true}});

    public static Glob 
    GetGlob(this string pattern) => GlobCache.GetOrAdd(pattern, CreateGlob);

    public static bool
    MatchGlob(this string path, string pattern) =>
        pattern.Contains(",") 
            ? pattern.Split(',').Select(x => x.Trim()).Any(x => x.GetGlob().IsMatch(path)) 
            : pattern.GetGlob().IsMatch(path);

    public static bool
    MatchAnyGlob(this string file, IEnumerable<string> patterns) => patterns.Any(file.MatchGlob);

    public static string
    FromExtensionRecursive(string extension) =>  $@"**\*{extension}, **\*{extension}.*";

    public static string
    AppendGlobPattern(this string glob, string other) => glob + ", " + other;

    public static IEnumerable<string>
    GetFilesMatchingGlob(this IEnumerable<string> files, string pattern) => 
        files.Where(x => x.MatchGlob(pattern));

}

internal struct RelativeGlob: IEquatable<RelativeGlob> {
    public string Value { get; }
    public RelativeGlob(string value) => Value = value;

    public override string ToString() => Value;

    public static bool operator == (RelativeGlob path1, RelativeGlob path2) => string.Equals(path1.Value, path2.Value, StringComparison.OrdinalIgnoreCase);
    public static bool operator !=(RelativeGlob path1, RelativeGlob path2) =>  !string.Equals(path1.Value, path2.Value, StringComparison.OrdinalIgnoreCase);

    public bool Equals(RelativeGlob other) => string.Equals(Value, other.Value, StringComparison.InvariantCultureIgnoreCase);
    public override bool Equals(object obj) => obj is RelativeGlob other && Equals(other);
    public override int GetHashCode() => (Value != null ? Value.ToLower().GetHashCode() : 0);
}

internal static class RelativeGlobEx {
    
    public static bool 
    Matches(this RelativePath path, RelativeGlob glob) => path.Value.MatchGlob(glob.Value);

    public static IEnumerable<RelativePath>
    GetMatchingFilesRelative(this string directory, RelativeGlob glob) => directory.EnumerateDirectoryRelativeFiles().Where(x => x.Matches(glob));

    public static IEnumerable<string> 
    GetMatchingFiles(this string directory, RelativeGlob glob) => directory.GetMatchingFilesRelative(glob).Select(x => x.ToAbsolute(directory));

    public static IEnumerable<RelativePath>
    GetMatching(this IEnumerable<RelativePath> paths, RelativeGlob glob) => paths.Where(x => x.Matches(glob));

    public static IEnumerable<RelativeGlob>
    ToRelativeGlobs(this IEnumerable<RelativePath> paths) => paths.Select(x => new RelativeGlob(x.Value));

    public static RelativeGlob
    ToRelativeGlob(this RelativePath path) => new RelativeGlob(path.Value.Escape("[").Escape("]").Escape("*").Escape("!"));

    private static string 
    Escape(this string str, string @char) => str.Replace(@char, "[" + @char + "]");

    public static bool
    IsSingleFileGlob(this RelativeGlob glob) {

        for (int i = 0; i < glob.Value.Length; i++) {
            var @char = glob.Value[i];
            if (@char == '[' && !glob.Value.IsEscaped(i))
                return false;

            if (@char == ']' && !glob.Value.IsEscaped(i))
                return false;

            if (@char == '*' && !glob.Value.IsEscaped(i))
                return false;

            if (@char == '!' && !glob.Value.IsEscaped(i))
                return false;
        }
        
        return true;
    }

    private static bool
    IsEscaped(this string str, int charIndex) => charIndex > 0 && str.Length > charIndex + 1 && str[charIndex - 1] == '[' && str[charIndex + 1] == ']';

    public static RelativeGlob
    ToRelativeGlob(this string path) => new RelativeGlob(path);

    public static IEnumerable<RelativePath> GetMatchingFilesRelative(this string directory, List<RelativeGlob> globs) {
        foreach (var file in directory.EnumerateDirectoryRelativeFiles())
        foreach (var glob in globs)
            if (file.Matches(glob))
                yield return file;
    }

    public static IEnumerable<RelativePath> GetMatchingFilesRelative(this string directory, List<RelativeGlob> globs, List<RelativeGlob> excludingGlobs) {
        foreach (var file in directory.EnumerateDirectoryRelativeFiles()) {
            bool isExcluded = false;
            foreach (var excludingGlob in excludingGlobs)
                if (file.Matches(excludingGlob)) {
                    isExcluded = true;
                    break;
                }

            if (isExcluded)
                continue;

            foreach (var glob in globs)
                if (file.Matches(glob))
                    yield return file;
        }
    }

}
}
