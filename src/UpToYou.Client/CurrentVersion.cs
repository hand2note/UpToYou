using System;
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
                   //versionProvider.FileSize == actualFile.GetFileSize() &&
                    //Treat package as installed if versions are equal
                   (versionProvider.FileVersion == null || versionProvider.FileVersion == actualFile.GetFileVersion());//  && 
                   //actualFile.GetFileHash() == versionProvider.FileHash;
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
    }
}
