using UpToYou.Core;

namespace UpToYou.Client {
internal static class SelfBinaries {
    public static string InstallExecutable = "UpToYou.Client.Runner.exe";
    
    public static bool 
    IsSelfBinaryFile(this RelativePath path) =>
        path.Value == "UpToYou.dll" ||
        path.Value == "UpToYou.Core.dll" ||
        path.Value == "UpToYou.Client.dll" || 
        path.Value == "UpToYou.Client.Runner.exe";

    public static bool 
    IsInstallExecutable(this RelativePath path) => 
        path.Value == InstallExecutable;

}
}
