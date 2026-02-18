# Build Instructions

## Quick Build (Application Only)

The installer project requires published artifacts. To build just the application for development:

```powershell
# Build all application projects (excluding installer)
cd src
dotnet build GrafaMon.Domain/GrafaMon.Domain.csproj
dotnet build GrafaMon.Application/GrafaMon.Application.csproj  
dotnet build GrafaMon.Infrastructure/GrafaMon.Infrastructure.csproj
dotnet build GrafaMon.Wpf/GrafaMon.Wpf.csproj
dotnet build GrafaMon.Tests/GrafaMon.Tests.csproj
```

Or use Visual Studio and **unload** the `GrafaMon.Installer` project before building.

## Build with Installer

To build the complete MSI installer:

```powershell
.\build-installer.ps1
```

This script will:
1. Publish the WPF application
2. Build the WiX installer with the published artifacts

## Test

```powershell
cd src
dotnet test GrafaMon.Tests/GrafaMon.Tests.csproj
```
