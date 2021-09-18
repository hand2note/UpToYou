#UpToYou.AzureConnectionString, UpToYou.AzureBlobStorage environment variables should be set to run the script
#On Windows it can be set in Edit Environment Variables Settings ...

param(
    [string]$PackageName,
    [string]$Version
)

$runnerCsproj = "$PSScriptRoot/../src/UpToYou.Backend.Runner/UpToYou.Backend.Runner.csproj"
Start-Process -FilePath "dotnet.exe" -ArgumentList "restore $runnerCsproj" -Wait -NoNewWindow
Start-Sleep -Milliseconds 500
Start-Process -FilePath "dotnet.exe" -ArgumentList "publish $runnerCsproj --configuration Release" -Wait -NoNewWindow 
$runnerDll = "$PSScriptRoot/../src\\UpToYou.Backend.Runner\\bin\\Release\\netcoreapp2.2\\publish\\UpToYou.dll"
Start-Sleep -Milliseconds 500
Start-Process -FilePath "dotnet.exe" -ArgumentList "$runnerDll RemovePackage --PackageName ""$PackageName"" --PackageVersion $Version" -Wait -NoNewWindow