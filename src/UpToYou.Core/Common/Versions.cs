using System;

namespace UpToYou.Core;
internal static class Versions {

    //2.1.0.0 should be equals 2.1
    public static bool
    VersionEquals(this Version version, Version other) => 
        version.Major.VersionComponentEquals(other.Major) &&
        version.Build.VersionComponentEquals(other.Build) &&
        version.Minor.VersionComponentEquals(other.Minor) &&
        version.Revision.VersionComponentEquals(other.Revision);

    public static bool 
    VersionComponentEquals(this int version, int other) {
        if (version.IsZeroOrNotSpecified() && other.IsZeroOrNotSpecified())
            return true;
        return version == other;
    }

    private static bool 
    IsZeroOrNotSpecified(this int versionComponent) => versionComponent == 0 || versionComponent == -1;
}
