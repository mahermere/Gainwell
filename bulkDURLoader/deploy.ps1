# PowerShell script to build and deploy bulkDURLoader
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("net6.0", "net8.0", "net9.0", "all")]
    [string]$Framework = "all",
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("win-x64", "win-x86", "linux-x64", "osx-x64")]
    [string]$Runtime = "win-x64",
    
    [Parameter(Mandatory = $false)]
    [switch]$SelfContained = $false,
    
    [Parameter(Mandatory = $false)]
    [switch]$SingleFile = $false
)

Write-Host "bulkDURLoader Deployment Script" -ForegroundColor Green
Write-Host "==============================" -ForegroundColor Green

$projectPath = "bulkDURLoader.csproj"

if (-not (Test-Path $projectPath)) {
    Write-Error "Project file not found: $projectPath"
    exit 1
}

function Build-Framework {
    param($targetFramework, $rid, $selfContained, $singleFile)
    
    Write-Host "Building for $targetFramework on $rid..." -ForegroundColor Yellow
    
    $publishArgs = @(
        "publish"
        "-c", "Release"
        "-f", $targetFramework
        "-r", $rid
        "--self-contained", $selfContained.ToString().ToLower()
    )
    
    if ($singleFile -and $selfContained) {
        $publishArgs += "-p:PublishSingleFile=true"
        $publishArgs += "-p:PublishTrimmed=true"
    }
    
    Write-Host "Command: dotnet $($publishArgs -join ' ')" -ForegroundColor Cyan
    
    & dotnet @publishArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Successfully built $targetFramework for $rid" -ForegroundColor Green
        
        $outputPath = "bin\Release\$targetFramework\$rid\publish"
        if (Test-Path $outputPath) {
            Write-Host "Output location: $outputPath" -ForegroundColor Cyan
        }
    }
    else {
        Write-Error "✗ Failed to build $targetFramework for $rid"
    }
}

# Check available .NET SDKs
Write-Host "Checking available .NET SDKs..." -ForegroundColor Yellow
& dotnet --list-sdks

Write-Host "`nStarting build process..." -ForegroundColor Yellow

if ($Framework -eq "all") {
    Build-Framework "net6.0" $Runtime $SelfContained $SingleFile
    Build-Framework "net8.0" $Runtime $SelfContained $SingleFile
    Build-Framework "net9.0" $Runtime $SelfContained $SingleFile
}
else {
    Build-Framework $Framework $Runtime $SelfContained $SingleFile
}

Write-Host "`nDeployment complete!" -ForegroundColor Green

# Show usage examples
Write-Host "`nUsage Examples:" -ForegroundColor Magenta
Write-Host "  .\deploy.ps1 -Framework net6.0 -Runtime win-x64 -SelfContained" -ForegroundColor White
Write-Host "  .\deploy.ps1 -Framework net8.0 -Runtime win-x64 -SelfContained" -ForegroundColor White
Write-Host "  .\deploy.ps1 -Framework net9.0 -Runtime linux-x64 -SelfContained -SingleFile" -ForegroundColor White
Write-Host "  .\deploy.ps1 -Framework all -Runtime win-x64" -ForegroundColor White