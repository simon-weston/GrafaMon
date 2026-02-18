# Installation Guide

Complete guide for installing, upgrading, and uninstalling GrafaMon.

---

## ?? Installation Methods

### **Standard Installation (Recommended)**

Double-click `GrafaMon.msi` and follow the wizard.

**Features:**
- ? Progress indication

---

### **Silent Installation (Automated/Enterprise)**

For automated deployments or enterprise environments:

```cmd
msiexec /i GrafaMon.msi /quiet /norestart
```

**Parameters:**
- `/i` - Install
- `/quiet` - No user interface
- `/norestart` - Don't restart automatically

---

### **Installation with Logging**

Create a detailed log file for troubleshooting:

```cmd
msiexec /i GrafaMon.msi /l*v install.log
```

**Log Levels:**
- `/l*v` - Verbose logging (recommended for debugging)
- `/l*` - All messages
- `/l` - Basic logging

The log file will be created in the current directory.

---

### **Custom Installation Directory**

Install to a custom location instead of default `C:\Program Files\GrafaMon`:

```cmd
msiexec /i GrafaMon.msi INSTALLFOLDER="C:\CustomPath\GrafaMon"
```

**Note:** Path must include the application folder name.

---

### **Install Without Desktop Shortcut**

Install only the core application without desktop shortcut:

```cmd
msiexec /i GrafaMon.msi ADDLOCAL=ProductFeature
```

You can still access the app from the Start Menu.

---

## ?? Upgrading

### **Automatic Upgrade Detection**

The installer automatically detects and upgrades previous versions:

1. Run the new `GrafaMon.msi`
2. Installer detects old version
3. Old version is removed
4. New version is installed
5. User data is preserved

**User Data Preservation:**
- ? Configuration: `%APPDATA%\GrafaMon\config.json`
- ? Logs: `%APPDATA%\GrafaMon\logs\`
- ? All settings are preserved across upgrades

---

### **Upgrade with Logging**

```cmd
msiexec /i GrafaMon.msi /l*v upgrade.log
```

---

### **Silent Upgrade**

```cmd
msiexec /i GrafaMon.msi /quiet /norestart
```

---

## ? Uninstallation

### **Uninstall via Add/Remove Programs** (Recommended)

1. Open **Settings** ? **Apps** ? **Apps & features**
2. Search for "GrafaMon"
3. Click **Uninstall**
4. Follow the wizard

---

### **Uninstall via MSI**

```cmd
msiexec /x GrafaMon.msi /quiet /norestart
```

---

### **Uninstall via Product Code**

Find the product code in the registry:

```cmd
msiexec /x {ProductCode} /quiet /norestart
```

To find the product code:
```powershell
Get-WmiObject -Class Win32_Product | Where-Object {$_.Name -eq "GrafaMon"} | Select-Object IdentifyingNumber
```

---

### **User Data After Uninstall**

?? **Important:** Uninstalling **does not** remove user data.

**User data location:** `%APPDATA%\GrafaMon\`

**Contains:**
- `config.json` - Configuration file
- `logs\` - Application logs

**To perform a complete clean uninstall:**

1. Uninstall the application (see above)
2. Manually delete user data folder:
   ```cmd
   rmdir /s /q "%APPDATA%\GrafaMon"
   ```

---

## ?? Repair Installation

If the application is corrupted or missing files:

### **Repair via Add/Remove Programs**

1. Open **Settings** ? **Apps** ? **Apps & features**
2. Search for "GrafaMon"
3. Click **Modify**
4. Click **Repair**

---

### **Repair via MSI**

```cmd
msiexec /f GrafaMon.msi
```

**Repair Options:**
- `/fa` - Repair all files
- `/fo` - Repair if file is missing
- `/fv` - Repair if wrong version
- `/f` - All repair options (default)

---

## ??? Troubleshooting

### **Installation Fails**

**1. Enable Verbose Logging:**
```cmd
msiexec /i GrafaMon.msi /l*v install.log
```

Check `install.log` for error messages.

---

**2. Check for Running Instances:**

The installer will prompt to close GrafaMon if it's running. If the prompt doesn't appear:

```powershell
# Check if running
Get-Process GrafaMon.Wpf -ErrorAction SilentlyContinue

# Force close
Stop-Process -Name GrafaMon.Wpf -Force
```

Then retry installation.

---

**3. Check Permissions:**

Installation requires **Administrator** privileges (perMachine scope).

Right-click Command Prompt ? **Run as Administrator**

```cmd
msiexec /i GrafaMon.msi /l*v install.log
```

---

**4. Check Disk Space:**

Ensure you have at least **100 MB** free space on the target drive.

---

**5. Check Windows Installer Service:**

```cmd
sc query msiserver
```

If not running:
```cmd
sc config msiserver start= demand
sc start msiserver
```

---

### **Upgrade Fails**

**1. Uninstall Old Version First:**

```cmd
msiexec /x GrafaMon.msi /quiet /norestart
msiexec /i GrafaMon-New.msi /l*v install.log
```

**2. Check Version Number:**

You cannot downgrade to an older version. The installer will reject downgrades.

---

### **Application Doesn't Start After Install**

**1. Verify Installation:**

Check if files exist:
```cmd
dir "C:\Program Files\GrafaMon\GrafaMon.Wpf.exe"
```

**2. Check Event Viewer:**

- Open **Event Viewer** ? **Windows Logs** ? **Application**
- Look for errors from "GrafaMon.Wpf.exe"

**3. Run Manually:**

```cmd
cd "C:\Program Files\GrafaMon"
GrafaMon.Wpf.exe
```

Check for error messages.

**4. Check .NET 8 Runtime:**

```cmd
dotnet --list-runtimes
```

Ensure `Microsoft.NETCore.App 8.0.x` is listed.

If missing, the installer should have included it (self-contained), but you can install it manually:
https://dotnet.microsoft.com/download/dotnet/8.0

---

### **Desktop Shortcut Not Created**

**1. Check Feature Selection:**

During installation, ensure "Desktop Shortcut" feature is selected.

**2. Manually Create Shortcut:**

Right-click Desktop ? **New** ? **Shortcut**
```
Target: "C:\Program Files\GrafaMon\GrafaMon.Wpf.exe"
Name: GrafaMon
```

---

## ?? Silent Installation Reference

### **Complete Silent Install**

```cmd
msiexec /i GrafaMon.msi /quiet /norestart /l*v install.log INSTALLFOLDER="C:\Program Files\GrafaMon"
```

### **Silent Uninstall**

```cmd
msiexec /x GrafaMon.msi /quiet /norestart /l*v uninstall.log
```

### **Silent Repair**

```cmd
msiexec /f GrafaMon.msi /quiet /norestart
```

---

## ?? Enterprise Deployment

### **Group Policy Deployment**

1. Copy `GrafaMon.msi` to a network share
2. Open **Group Policy Management**
3. Create a new GPO
4. Navigate to **Computer Configuration** ? **Policies** ? **Software Settings** ? **Software Installation**
5. Right-click ? **New** ? **Package**
6. Browse to `GrafaMon.msi` on the network share
7. Select **Assigned**
8. Link the GPO to the target OU

---

### **SCCM/Intune Deployment**

**SCCM:**
```cmd
msiexec /i GrafaMon.msi /quiet /norestart /l*v C:\Windows\Temp\GrafaMon_Install.log
```

**Intune:**
- Application Type: **Windows app (Win32)**
- Install command: `msiexec /i GrafaMon.msi /quiet /norestart`
- Uninstall command: `msiexec /x GrafaMon.msi /quiet /norestart`
- Detection method: **File exists** ? `C:\Program Files\GrafaMon\GrafaMon.Wpf.exe`

---

### **PDQ Deploy**

1. Create new package
2. Install step:
   ```cmd
   msiexec /i GrafaMon.msi /quiet /norestart
   ```
3. Success codes: `0, 3010`

---

## ?? MSI Properties Reference

| Property | Default | Description | Example |
|----------|---------|-------------|---------|
| `INSTALLFOLDER` | `C:\Program Files\GrafaMon` | Installation directory | `INSTALLFOLDER="D:\Apps\GrafaMon"` |
| `ADDLOCAL` | `ALL` | Features to install | `ADDLOCAL=ProductFeature` (no desktop shortcut) |
| `DESKTOPSHORTCUT` | `1` | Create desktop shortcut | `DESKTOPSHORTCUT=0` (disable) |

---

## ? Installation Checklist

Before installing:
- [ ] Windows 8.1 or later (Windows 10 1809+ recommended)
- [ ] Administrator privileges
- [ ] 100 MB free disk space
- [ ] No other instances of GrafaMon running

After installing:
- [ ] Application starts successfully
- [ ] Start Menu shortcut created
- [ ] Desktop shortcut created (if selected)
- [ ] Configuration wizard appears on first run
- [ ] Can access Settings from tray icon

---

## ?? Support

**Documentation:**
- Configuration: See `CONFIGURATION.md`
- Architecture: See `ARCHITECTURE.md`
- Source Code: https://github.com/simon-weston/GrafaMon

**Issue Reporting:**
- GitHub Issues: https://github.com/simon-weston/GrafaMon/issues
- Include `install.log` if installation fails

---

## ?? Version History

The installer uses semantic versioning: `YY.DDD.NN`

- **YY** = Last 2 digits of year (24 = 2024)
- **DDD** = Day of year (1-366)
- **NN** = Git commit count

**Example:** Version `24.354.42`
- Built in 2024
- On day 354 of the year (December 19)
- From commit #42

You can check your installed version:
- Right-click **GrafaMon.Wpf.exe** ? **Properties** ? **Details**
- Or check **Add/Remove Programs**

---

