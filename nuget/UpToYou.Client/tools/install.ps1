param($installPath, $toolsPath, $package, $project)

function MarkFileASCopyToOutputDirectory($item)
{
    Try
    {
        Write-Host Try set $item.Name
        $item.Properties.Item("CopyToOutputDirectory").Value = 2
    }
    Catch
    {
        Write-Host RecurseOn $item.Name
        MarkDirectoryAsCopyToOutputRecursive($item)
    }
}

MarkFileASCopyToOutputDirectory($project.ProjectItems.Item("liblzma.dll"))
MarkFileASCopyToOutputDirectory($project.ProjectItems.Item("liblzma64.dll"))
MarkFileASCopyToOutputDirectory($project.ProjectItems.Item("UpToYou.Client.Runner.dll"))
MarkFileASCopyToOutputDirectory($project.ProjectItems.Item("UpToYou.Client.Runner.exe"))
MarkFileASCopyToOutputDirectory($project.ProjectItems.Item("UpToYou.Client.Runner.deps.json"))
MarkFileASCopyToOutputDirectory($project.ProjectItems.Item("UpToYou.Client.Runner.runtimeconfig.json"))