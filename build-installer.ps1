## Copyright(C) 2026 Simon Weston
## Licensed under the GNU General Public License v3.0
## SPDX-License-Identifier: GPL-3.0-only

# Build GrafaMon MSI Installer
# This script builds the WPF application and creates an MSI installer
param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "artifacts"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "GrafaMon MSI Installer Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get solution directory
$scriptPath = $PSScriptRoot
$solutionDir = $scriptPath 
$srcDir = Join-Path $scriptPath "src"
$wpfProject = Join-Path $srcDir "GrafaMon.Wpf\GrafaMon.Wpf.csproj"
$installerProject = Join-Path $srcDir "GrafaMon.Installer\GrafaMon.Installer.wixproj"
$publishDir = Join-Path $solutionDir "$OutputPath\publish"
$outputDir = Join-Path $solutionDir $OutputPath

Write-Host "scriptPath: $PSScriptRoot"
Write-Host "Output Dir: $outputDir"
Write-Host ""


# Get version from Git commit count
$gitCommitCount = (git rev-list --count HEAD)
if ($LASTEXITCODE -ne 0) {
    Write-Host "Warning: Could not get Git commit count, using 0" -ForegroundColor Yellow
    $gitCommitCount = 0
}

# Build version using MSI-compliant format
# MSI constraints: Major < 256, Minor < 256, Build < 65536
$currentDate = Get-Date
$year = $currentDate.Year

# MSI-compliant version format: YY.DDD.CommitCount
# Major = Last 2 digits of year (0-99, always < 256)
# Minor = Day of year (1-366, always < 256)
# Build = Git commit count (< 65536)
$yearShort = $year % 100  # Last 2 digits of year (e.g., 2024 -> 24, 2025 -> 25)
$dayOfYear = $currentDate.DayOfYear  # 1-366
$msiVersion = "$yearShort.$dayOfYear.$gitCommitCount"

# Display version for logging (full year for clarity)
$displayVersion = "$year.$($currentDate.ToString('MMdd')).$gitCommitCount"

Write-Host "Building version:" -ForegroundColor Green
Write-Host "  MSI Version: $msiVersion (MSI-compliant: Year=$yearShort, Day-of-Year=$dayOfYear, Commits=$gitCommitCount)" -ForegroundColor Cyan
Write-Host "  Build Date: $($currentDate.ToString('yyyy-MM-dd')) (Day $dayOfYear of $year)" -ForegroundColor Cyan

# Use MSI-compliant version for build
$version = $msiVersion

# Clean output directory
Write-Host ""
Write-Host "Cleaning output directory..." -ForegroundColor Yellow
if (Test-Path $outputDir) {
    Remove-Item -Path $outputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

# Build and publish WPF application
Write-Host ""
Write-Host "Publishing GrafaMon.Wpf..." -ForegroundColor Yellow
dotnet publish $wpfProject `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:BuildVersion=$version `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to publish WPF application" -ForegroundColor Red
    exit 1
}

Write-Host "? Publish completed successfully" -ForegroundColor Green

# Check if WiX is installed
Write-Host ""
Write-Host "Checking for WiX Toolset..." -ForegroundColor Yellow
$wixInstalled = $null
try {
    $wixInstalled = dotnet tool list --global | Select-String "wix"
} catch {
    # Ignore error
}

if (-not $wixInstalled) {
    Write-Host "WiX Toolset not found. Installing WiX v6.0.2..." -ForegroundColor Yellow
    dotnet tool install --global wix --version 6.0.2
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to install WiX Toolset" -ForegroundColor Red
        exit 1
    }
    Write-Host "? WiX Toolset v6.0.2 installed successfully" -ForegroundColor Green
} else {
    Write-Host "? WiX Toolset found" -ForegroundColor Green
    Write-Host "  Updating to v6.0.2..." -ForegroundColor Yellow
    dotnet tool update --global wix --version 6.0.2
}

# Build MSI installer with bind paths and version
Write-Host ""
Write-Host "Building MSI installer with version $version..." -ForegroundColor Yellow
dotnet build $installerProject `
    -c $Configuration `
    -p:Platform=x64 `
    -p:BuildVersion=$version `
    -p:WixBindPaths="PublishDir=$publishDir;SolutionDir=$srcDir" `
    -o $outputDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to build MSI installer" -ForegroundColor Red
    exit 1
}

# Find and display MSI file
$msiFile = Get-ChildItem -Path $outputDir -Filter "*.msi" | Select-Object -First 1
if ($msiFile) {
    # Rename MSI file with version suffix
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($msiFile.Name)
    $newMsiName = "$baseName-$version.msi"
    $newMsiPath = Join-Path $outputDir $newMsiName
    
    Write-Host ""
    Write-Host "Renaming MSI file to include version..." -ForegroundColor Yellow
    Rename-Item -Path $msiFile.FullName -NewName $newMsiName
    $msiFile = Get-Item -Path $newMsiPath
    Write-Host "? MSI renamed to: $newMsiName" -ForegroundColor Green
    
    $msiSize = [math]::Round($msiFile.Length / 1MB, 2)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "? Build completed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "MSI Installer: $($msiFile.FullName)" -ForegroundColor Cyan
    Write-Host "Size: $msiSize MB" -ForegroundColor Cyan
    Write-Host "Version: $version" -ForegroundColor Cyan
    Write-Host ""
    
    # Validate MSI package
    Write-Host "Validating MSI package..." -ForegroundColor Yellow
    
    # Check if orca.exe is available (optional tool for inspecting MSI files)
    $orcaPath = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\orca.exe"
    if (Test-Path $orcaPath) {
        Write-Host "  ? Orca.exe found - Inspect MSI with: & '$orcaPath' '$($msiFile.FullName)'" -ForegroundColor Green
    } else {
        Write-Host "  ? Orca.exe not found (optional tool for MSI inspection)" -ForegroundColor Gray
    }
    
    # Display installation command examples
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Installation Commands:" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Standard Install:" -ForegroundColor Yellow
    Write-Host "  msiexec /i `"$($msiFile.FullName)`" /l*v install.log" -ForegroundColor White
    Write-Host ""
    Write-Host "Silent Install:" -ForegroundColor Yellow
    Write-Host "  msiexec /i `"$($msiFile.FullName)`" /quiet /norestart" -ForegroundColor White
    Write-Host ""
    Write-Host "Install to Custom Location:" -ForegroundColor Yellow
    Write-Host "  msiexec /i `"$($msiFile.FullName)`" INSTALLFOLDER=`"C:\CustomPath\GrafaMon`"" -ForegroundColor White
    Write-Host ""
    Write-Host "Install Without Desktop Shortcut:" -ForegroundColor Yellow
    Write-Host "  msiexec /i `"$($msiFile.FullName)`" ADDLOCAL=ProductFeature" -ForegroundColor White
    Write-Host ""
    Write-Host "Uninstall:" -ForegroundColor Yellow
    Write-Host "  msiexec /x `"$($msiFile.FullName)`" /quiet /norestart" -ForegroundColor White
    Write-Host ""
    Write-Host "Repair Install:" -ForegroundColor Yellow
    Write-Host "  msiexec /f `"$($msiFile.FullName)`"" -ForegroundColor White
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Note: User data is stored in %APPDATA%\GrafaMon\ and is preserved across upgrades/uninstalls" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host "WARNING: MSI file not found in output directory" -ForegroundColor Yellow
}
