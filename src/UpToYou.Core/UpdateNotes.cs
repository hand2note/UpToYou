using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UpToYou.Core {

/*Update notes file name format:
{PackageName}.UpdateNotes.{locale}.md

examples:
UpdateNotes.md
UpdateNotes.en.md
MyApp.UpdateNotes.md
MyApp.UpdateNotes.en.md

Update notes file content format:
[//] #: [version]
[UpdateNotes in markdown format]

example:
[//] #: 1.0.0.1
-Fixed some another issue

[//] #: 1.0.0.0
- ``Another Tag:`` Changed _this_ and *those*
- ``Some Tag:`` Fixed some issue

*/


public class 
UpdateNotes {
    public UpdateNotes(string packageName, Version version, string notes) {
        PackageName = packageName;
        Version = version;
        Notes = notes;
    }
    public string PackageName { get; }
    public Version Version { get; }
    public string Notes { get; }

    public bool 
    Hits(string packageName, Version version) =>
        (string.IsNullOrWhiteSpace(packageName) ? string.IsNullOrWhiteSpace(PackageName) : PackageName == packageName ) &&
        Version.VersionEquals(version);
}

public static class 
UpdateNotesHelper {

    public static string PackageNotesAnchor = "[//]: #";

    public static string 
    GetUpdateNotesFileName(string packageName, string locale) =>
        $"{packageName}.UpdateNotes.{locale}.md";

    public static (string? fileName, string? locale)
    ParseUpdateNotesParsFromFile(this string file) {
        var fileName = file.GetFileName();
        fileName = fileName.Replace(".md", "").Replace("UpdateNotes","").Replace("..", ".");
        if (!string.IsNullOrWhiteSpace(fileName)) {
            var localeIndex= fileName.LastIndexOf('.');
            return localeIndex == -1
                ? (fileName, null)
                : (localeIndex > 0? fileName.Substring(0, localeIndex) : null, fileName.Substring(localeIndex + 1));
        }
        return (null, null);
    }

    public static IEnumerable<(Version version, string notes)> 
    ParseUpdateNotes(this string text) =>
        text.Trim().Split(new[] {PackageNotesAnchor}, StringSplitOptions.RemoveEmptyEntries).Where(entry => entry!= "\uFEFF"/*BOM*/ && entry.Length > 2).Select(entry => entry.Trim().ParsePackageUpdateNotes());

    public static bool
    Contains(this IEnumerable<(Version version, string notes)> updateNotes, Version version) => updateNotes.FindNotes(version) != null;

    public static string? 
    FindNotes(this IEnumerable<(Version version, string notes)> updateNotes, Version version) => 
        updateNotes.TryGet(x => x.version.VersionEquals(version), out var res) ? res.notes : null;

    private static (Version version, string notes) 
    ParsePackageUpdateNotes(this string text) {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(text));

        var reader = new StringReader(text);
        var header = reader.ReadLine();
        if (header == null)
            throw new InvalidOperationException($"Failed to parse update notes header \n {text}");
        return (header.Trim().ParseVersion(), reader.ReadToEnd().Trim());
    }
}
}
