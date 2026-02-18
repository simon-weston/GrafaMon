## Copyright(C) 2026 Simon Weston
## Licensed under the GNU General Public License v3.0
## SPDX-License-Identifier: GPL-3.0-only

# Build GrafaMon Application (excluding installer)
# The installer requires published artifacts and should only be built for releases

Write-Host "Building GrafaMon Application..." -ForegroundColor Cyan

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcPath = $scriptPath

# Build order: Domain -> Application -> Infrastructure -> Wpf -> Tests
$projects = @(
    "src\GrafaMon.Domain\GrafaMon.Domain.csproj",
    "src\GrafaMon.Application\GrafaMon.Application.csproj",
    "src\GrafaMon.Infrastructure\GrafaMon.Infrastructure.csproj",
    "src\GrafaMon.Wpf\GrafaMon.Wpf.csproj",
    "src\GrafaMon.Tests\GrafaMon.Tests.csproj"
)

$failed = $false

foreach ($project in $projects) {
    $projectPath = Join-Path $srcPath $project
    Write-Host "`nBuilding $project..." -ForegroundColor Yellow
    
    dotnet build $projectPath --configuration Debug
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to build $project" -ForegroundColor Red
        $failed = $true
        break
    }
}

if (-not $failed) {
    Write-Host "`n? All projects built successfully!" -ForegroundColor Green
    Write-Host "`nNote: The installer project is excluded from this build." -ForegroundColor Gray
    Write-Host "To build the installer, run: .\build-installer.ps1" -ForegroundColor Gray
    exit 0
} else {
    Write-Host "`n? Build failed!" -ForegroundColor Red
    exit 1
}
