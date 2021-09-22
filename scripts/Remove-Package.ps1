#UpToYou.AzureConnectionString, UpToYou.AzureBlobStorage environment variables should be set to run the script
#On Windows it can be set in Edit Environment Variables Settings ...

param(
    [string]$PackageName = "Hand2Note Windows x64",
    [string]$Version = "4.0.0.4",
    [string]$AzureConnectionString = "DefaultEndpointsProtocol=https;AccountName=h2n;AccountKey=jzzncAXQsNVh7A7rHngOefRCKgiyIoGhizo8/rZy58cshUAnuT7fygVLNWToNycFTLT3EiMTBKHoXYo4dXrV2A==;EndpointSuffix=core.windows.net",
    [string]$AzureRootContainer = "uptoyou-hand2note4-ring0"
)

& dotnet publish "$PSScriptRoot/../src/UpToYou.Backend.Runner"
Start-Sleep -Milliseconds 500
$args = "RemovePackage " + 
"--PackageName `"$PackageName`"" + 
" --PackageVersion $Version" +
" --AzureConnectionString $AzureConnectionString" + 
" --AzureRootContainer $AzureRootContainer"

$upToYouExe = "$PSScriptRoot/../src\\UpToYou.Backend.Runner\\bin\\Debug\\net5.0\\publish\\UpToYou.exe"
Remove-Item -Path "out.txt" -ErrorAction Ignore
Remove-Item -Path "error.txt" -ErrorAction Ignore
Start-Process -FilePath  $upToYouExe -ArgumentList $args -Wait -NoNewWindow -RedirectStandardOutput "out.txt" -RedirectStandardError "error.txt"
$out = Get-Content "out.txt"
$out | Write-Host
$err = (Get-Content "error.txt") 
if ($err){
    $err | Join-String -Separator "`n" | Write-Error
}