param(
    [string]$filter = ''
)

function Test-Project([string]$projectPath){
    Write-Host "Running $projectPath" -ForegroundColor green
    if ($filter -ne ''){
        & dotnet test "$projectPath" --filter $filter    
    }
    else {
        & dotnet test "$projectPath" 
    }
    if ($lastexitcode -ne 0){
        exit 1;
    }
}

Test-Project('./tests/UpToYou.Core.Tests')
Test-Project('./tests/UpToYou.Client.Tests')
Test-Project('./tests/UpToYou.Backend.Runner.Tests')
Test-Project('./tests/UpToYou.Backend.Tests')
