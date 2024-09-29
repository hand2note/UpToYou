using System;
using System.Collections.Generic;
using UpToYou.Core;

namespace UpToYou.Client
{
    public static class CurrentVersion
    {
        public static bool 
        IsInstalled(this PackageHeader header, string programDirectory) {
            var actualFile = header.VersionProviderFile.Path.ToAbsolute(programDirectory);
            var versionProvider = header.VersionProviderFile;
            return actualFile.FileExists() && 
                (versionProvider.FileVersion == null || versionProvider.FileVersion.VersionEquals(actualFile.GetFileVersion()));
        }

        public static Version? 
        GetInstalledVersion(this PackageHeader packageHeader, string programDirectory) { 
            var versionProvider = packageHeader.VersionProviderFile.Path.ToAbsolute(programDirectory);
            if (!versionProvider.FileExists())
                return null;
            return versionProvider.GetFileVersion();
        }
            

        public static bool
        IsHigherVersionInstalled(this PackageHeader header, string programDirectory) {
            var actualFile = header.VersionProviderFile.Path.ToAbsolute(programDirectory);
            if (!actualFile.FileExists())
                return false;
            var installedVersion =actualFile.GetFileVersion();
            if (installedVersion == null)
                return false;
            var versionProvider = header.VersionProviderFile;
            
            return versionProvider.FileVersion != null && installedVersion >= versionProvider.FileVersion;
        }

        public static IEnumerable<(string name, Version version)>
        GetInstalledVersions(this IEnumerable<PackageHeader> headers, string programDirectory) {
            foreach (var header in headers) {
                var actualFile = header.VersionProviderFile.Path.ToAbsolute(programDirectory);
                if (!actualFile.FileExists())
                    continue;
                var installedVersion = actualFile.GetFileVersion();
                if (installedVersion == null)
                    continue;
                yield return (header.Name, installedVersion);
            }
        }
    }
}
