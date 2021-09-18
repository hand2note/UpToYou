using System;
using UpToYou.Core;

namespace UpToYou.Client
{
    public static class CurrentVersion
    {
        public static bool 
        IsInstalled(this PackageMetadata metadata, string programDirectory) {
            var actualFile = metadata.VersionProviderFile.Path.ToAbsolute(programDirectory);
            var versionProvider = metadata.VersionProviderFile;
            return actualFile.FileExists() && 
                   //versionProvider.FileSize == actualFile.GetFileSize() &&
                    //Treat package as installed if versions are equal
                   (versionProvider.FileVersion == null || versionProvider.FileVersion == actualFile.GetFileVersion());//  && 
                   //actualFile.GetFileHash() == versionProvider.FileHash;
        }

        public static Version?
        GetInstalledVersion(this Update update, string programDirectory) => update.PackageMetadata.GetInstalledVersion(programDirectory);

        public static Version? 
        GetInstalledVersion(this PackageMetadata packageMetadata, string programDirectory) { 
            var versionProvider = packageMetadata.VersionProviderFile.Path.ToAbsolute(programDirectory);
            if (!versionProvider.FileExists())
                return null;
            return versionProvider.GetFileVersion();
        }
            

        public static bool
        IsHigherVersionInstalled(this PackageMetadata metadata, string programDirectory) {
            var actualFile = metadata.VersionProviderFile.Path.ToAbsolute(programDirectory);
            if (!actualFile.FileExists())
                return false;
            var installedVersion =actualFile.GetFileVersion();
            if (installedVersion == null)
                return false;
            var versionProvider = metadata.VersionProviderFile;
            
            return versionProvider.FileVersion != null && installedVersion >= versionProvider.FileVersion;
        }
    }
}
