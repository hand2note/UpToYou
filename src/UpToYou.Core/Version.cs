using System;

namespace UpToYou.Core {
    internal static class VersionHelper {
        public static Version ParseVersion(this string version) =>
            System.Version.Parse(version);

        public static Version? TryReadFolderVersion(this string path) => 
            path.AppendPath(".version").ReadAllFileTextIfExists()?.Trim().ParseVersion();
    }
}
