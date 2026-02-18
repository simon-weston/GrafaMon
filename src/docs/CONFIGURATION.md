# Configuration Guide

Complete guide to configuring GrafaMon - Grafana Alert Monitoring Desktop Notifier application.

---

## 📋 Configuration File Location

The application reads configuration from `config.json` in the same directory as the executable.

**Default Location:**
```
C:\Users\%USERNAME%\AppData\Roaming\GrafaMon\config.json
```

---

## 📝 Configuration File Format

The configuration file is a JSON file with the following structure:

```json
{
  "GrafanaBaseUrl": "https://your-grafana-instance.com",
  "ApiKey": "your-grafana-api-key-here",
  "GrafanaOrgId": "1",
  "ActiveAlertsPath": "/api/alertmanager/grafana/api/v2/alerts",
  "PollingIntervalSeconds": 60,
  "EnableSoundNotifications": true,
  "SoundFilePath": ""
}
```

---

## ⚙️ Configuration Options

### **GrafanaBaseUrl** (Required)

**Type:** `string`  
**Description:** The base URL of your Grafana instance.  
**Validation:** Must be a valid HTTP/HTTPS URL.

**Examples:**
```json
"GrafanaBaseUrl": "https://grafana.example.com"
"GrafanaBaseUrl": "https://grafana.example.com:3000"
"GrafanaBaseUrl": "http://localhost:3000"
```

**Common Mistakes:**
- ❌ Including trailing slash: `"https://grafana.example.com/"`
- ❌ Including path: `"https://grafana.example.com/api"`
- ✅ Correct: `"https://grafana.example.com"`

---

### **ApiKey** (Required)

**Type:** `string`  
**Description:** Grafana API key (Service Account token) with read permissions.  
**Validation:** Must not be empty or whitespace.

**Example:**
```json
"ApiKey": "(glsa_1234567890abcdefghijklmnopqrstuvwxyz)"
```

**How to Generate:**
1. Go to **Grafana → Configuration → Service Accounts**
2. Click **Add service account**
3. Name: `AlertNotifications`
4. Role: **Viewer** (read-only access is sufficient)
5. Click **Add service account token**
6. Copy the token and paste into `config.json`

**Security Notes:**
- ✅ API key is stored encrypted text in `config.json`
- ⚠️ Protect `config.json` with appropriate file permissions
- ✅ API key is masked in logs (shown as `***`)
- ✅ Use a dedicated service account with minimal permissions

---

### **GrafanaOrgId** (Required)

**Type:** `string`  
**Description:** Grafana organization ID.  
**Default:** `"1"` (default organization)  
**Validation:** Must not be empty or whitespace.

**Examples:**
```json
"GrafanaOrgId": "1"
"GrafanaOrgId": "42"
```

**How to Find Your Org ID:**
1. Log into Grafana
2. Go to **Configuration → Preferences**
3. Look for **Organization** section
4. The ID is shown in the URL: `https://grafana.example.com/org/1`

**Multi-Org Environments:**
- If you have multiple organizations, specify the correct org ID
- The API key must have access to the specified organization

---

### **ActiveAlertsPath** (Required)

**Type:** `string`  
**Description:** API endpoint path for fetching active alerts.  
**Default:** `"/api/alertmanager/grafana/api/v2/alerts"`  
**Validation:** Must not be empty or whitespace.

**Examples:**
```json
"ActiveAlertsPath": "/api/alertmanager/grafana/api/v2/alerts"
```

**Grafana Version Compatibility:**
- **Grafana 9.x+:** `/api/alertmanager/grafana/api/v2/alerts` ✅
- **Grafana 8.x:** `/api/alertmanager/grafana/api/v2/alerts` ✅
- **Grafana 7.x and earlier:** Not supported (legacy alerting)

**Common Mistakes:**
- ❌ Missing leading slash: `"api/alertmanager/..."`
- ❌ Including base URL: `"https://grafana.example.com/api/..."`
- ✅ Correct: `"/api/alertmanager/grafana/api/v2/alerts"`

---

### **PollingIntervalSeconds** (Optional)

**Type:** `integer`  
**Description:** How often to poll Grafana for alerts (in seconds).  
**Default:** `60`  
**Minimum:** `5`  
**Validation:** Must be >= 5 seconds.

**Examples:**
```json
"PollingIntervalSeconds": 60   // Poll every 1 minute (default)
"PollingIntervalSeconds": 30   // Poll every 30 seconds
"PollingIntervalSeconds": 300  // Poll every 5 minutes
```

**Recommendations:**
- **High-traffic environments:** 30-60 seconds
- **Low-traffic environments:** 60-300 seconds
- **Development/testing:** 10-30 seconds

**Performance Considerations:**
- Lower intervals = more API calls = higher load on Grafana
- Higher intervals = slower alert detection
- Balance between responsiveness and resource usage

---

### **EnableSoundNotifications** (Optional)

**Type:** `boolean`  
**Description:** Enable or disable sound notifications for new critical alerts.  
**Default:** `true`

**Examples:**
```json
"EnableSoundNotifications": true   // Enable sounds (default)
"EnableSoundNotifications": false  // Disable sounds
```

**When to Disable:**
- Shared workspaces (open office)
- After-hours monitoring (don't wake people up)
- Multiple instances running (avoid sound spam)

---

### **SoundFilePath** (Optional)

**Type:** `string`  
**Description:** Path to a custom `.wav` sound file. If empty, uses Windows system sound.  
**Default:** `""` (empty = system sound)  
**Validation:** If specified, file must exist and be a `.wav` file.

**Examples:**
```json
"SoundFilePath": ""                                    // Use Windows system sound (default)
"SoundFilePath": "C:\\Sounds\\critical-alert.wav"     // Custom sound (absolute path)
"SoundFilePath": ".\\sounds\\alert.wav"               // Custom sound (relative path)
```

**Supported Formats:**
- ✅ `.wav` (PCM, uncompressed)
- ❌ `.mp3` (not supported by `SoundPlayer`)
- ❌ `.ogg`, `.flac`, `.aac` (not supported)

**Path Format:**
- Use double backslashes (`\\`) in JSON for Windows paths
- Or use forward slashes (`/`) which work on Windows too
- Relative paths are relative to the executable directory

**Fallback Behavior:**
- If file doesn't exist → Falls back to system sound
- If file is not `.wav` → Falls back to system sound
- If file is corrupted → Falls back to system sound

**Finding Custom Sounds:**
- [Freesound.org](https://freesound.org/) - Free sound effects
- [Zapsplat.com](https://www.zapsplat.com/) - Free sound effects
- Windows system sounds: `C:\Windows\Media\`

---

## 📋 Complete Configuration Examples

### **Example 1: Production Environment**

```json
{
  "GrafanaBaseUrl": "https://grafana.company.com",
  "ApiKey": "glsa_abc123def456ghi789jkl012mno345pqr678stu901vwx234yz",
  "GrafanaOrgId": "1",
  "ActiveAlertsPath": "/api/alertmanager/grafana/api/v2/alerts",
  "PollingIntervalSeconds": 60,
  "EnableSoundNotifications": true,
  "SoundFilePath": "C:\\Sounds\\critical-alert.wav"
}
```

### **Example 2: Development Environment**

```json
{
  "GrafanaBaseUrl": "http://localhost:3000",
  "ApiKey": "glsa_dev_token_here",
  "GrafanaOrgId": "1",
  "ActiveAlertsPath": "/api/alertmanager/grafana/api/v2/alerts",
  "PollingIntervalSeconds": 10,
  "EnableSoundNotifications": false,
  "SoundFilePath": ""
}
```

### **Example 3: Minimal Configuration**

```json
{
  "GrafanaBaseUrl": "https://grafana.example.com",
  "ApiKey": "glsa_your_token_here",
  "GrafanaOrgId": "1",
  "ActiveAlertsPath": "/api/alertmanager/grafana/api/v2/alerts",
  "PollingIntervalSeconds": 60,
  "EnableSoundNotifications": true,
  "SoundFilePath": ""
}
```

### **Example 4: Silent Mode (No Sounds)**

```json
{
  "GrafanaBaseUrl": "https://grafana.example.com",
  "ApiKey": "glsa_your_token_here",
  "GrafanaOrgId": "1",
  "ActiveAlertsPath": "/api/alertmanager/grafana/api/v2/alerts",
  "PollingIntervalSeconds": 60,
  "EnableSoundNotifications": false,
  "SoundFilePath": ""
}
```

### **Example 5: High-Frequency Polling**

```json
{
  "GrafanaBaseUrl": "https://grafana.example.com",
  "ApiKey": "glsa_your_token_here",
  "GrafanaOrgId": "1",
  "ActiveAlertsPath": "/api/alertmanager/grafana/api/v2/alerts",
  "PollingIntervalSeconds": 15,
  "EnableSoundNotifications": true,
  "SoundFilePath": ""
}
```

---

## 🔍 Configuration Validation

The application validates configuration on startup. If validation fails, the application will exit with an error message.

### **Validation Rules**

| Field | Validation |
|-------|------------|
| `GrafanaBaseUrl` | Must be valid HTTP/HTTPS URL |
| `ApiKey` | Must not be empty or whitespace |
| `GrafanaOrgId` | Must not be empty or whitespace |
| `ActiveAlertsPath` | Must not be empty or whitespace |
| `PollingIntervalSeconds` | Must be >= 5 |
| `SoundFilePath` | If specified, file must exist and be `.wav` |

### **Validation Error Messages**

**Invalid URL:**
```
Configuration error: GrafanaBaseUrl must be a valid HTTP or HTTPS URL.
Current value: 'not-a-url'
```

**Empty API Key:**
```
Configuration error: ApiKey cannot be empty.
```

**Invalid Polling Interval:**
```
Configuration error: PollingIntervalSeconds must be at least 5 seconds.
Current value: 2
```

**Missing Sound File:**
```
Configuration error: SoundFilePath does not exist: 'C:\Sounds\missing.wav'
```

**Invalid Sound File Format:**
```
Configuration error: SoundFilePath must be a .wav file.
Current value: 'C:\Sounds\alert.mp3'
```

---

## 🛠️ Troubleshooting Configuration Issues

### **Problem: Application won't start**

**Symptoms:**
- Application exits immediately
- No window appears

**Solutions:**
1. Check `logs/` folder for error messages
2. Verify `config.json` is valid JSON (use [JSONLint](https://jsonlint.com/))
3. Verify all required fields are present
4. Verify `GrafanaBaseUrl` is a valid URL
5. Verify `ApiKey` is not empty

---

### **Problem: No alerts showing**

**Symptoms:**
- Widget shows `🔥 0 | ⚠️ 0 | ℹ️ 0`
- But Grafana has active alerts

**Solutions:**
1. Verify `GrafanaOrgId` matches your organization
2. Verify API key has access to the organization
3. Verify `ActiveAlertsPath` is correct for your Grafana version
4. Check logs for HTTP errors
5. Test API manually:
   ```bash
   curl -H "Authorization: Bearer YOUR_API_KEY" \
        -H "X-Grafana-Org-Id: 1" \
        https://grafana.example.com/api/alertmanager/grafana/api/v2/alerts
   ```

---

### **Problem: Sound not playing**

**Symptoms:**
- Visual alerts work
- No sound plays

**Solutions:**
1. Verify `EnableSoundNotifications` is `true`
2. Verify sound file exists (if custom sound)
3. Verify sound file is `.wav` format
4. Check Windows volume settings
5. Check logs for sound errors
6. Test with system sound (set `SoundFilePath` to `""`)

---

### **Problem: Polling errors (orange border)**

**Symptoms:**
- Widget has orange border
- Tooltip shows error message

**Solutions:**
1. Hover over widget to see error message
2. Check logs for detailed error
3. Verify network connectivity to Grafana
4. Verify API key is still valid
5. Verify Grafana is running and accessible
6. Check firewall/proxy settings

---

## 🔐 Security Best Practices

### **1. Protect config.json**

**Windows File Permissions:**
```powershell
# Remove inheritance
icacls config.json /inheritance:r

# Grant read access only to current user
icacls config.json /grant:r "%USERNAME%:R"
```

### **2. Use Dedicated Service Account**

- Create a dedicated Grafana service account for the application
- Grant **Viewer** role only (read-only)
- Don't use your personal API key

### **3. Rotate API Keys Regularly**

- Rotate API keys every 90 days
- Revoke old keys after rotation
- Update `config.json` with new key

### **4. Use HTTPS**

- Always use HTTPS for `GrafanaBaseUrl`
- Don't disable certificate validation

### **5. Audit Access**

- Monitor API key usage in Grafana
- Review service account permissions regularly

---

## 📦 Installation and Upgrades

### **User Data Location**

**Configuration:** `%APPDATA%\GrafaMon\config.json`  
**Logs:** `%APPDATA%\GrafaMon\logs\`

The installer **does not** manage the `%APPDATA%\GrafaMon\` folder. Your configuration and logs are preserved across:
- ✅ **Upgrades** - New version installation keeps your settings
- ✅ **Reinstalls** - Uninstall and reinstall preserves data
- ✅ **Repairs** - Repair operation doesn't touch user data

### **Installation**

See `INSTALL.md` for complete installation instructions.

**Quick Install:**
```cmd
msiexec /i GrafaMon.msi
```

**Silent Install:**
```cmd
msiexec /i GrafaMon.msi /quiet /norestart
```

### **Upgrading**

The installer automatically detects and upgrades previous versions:
1. Run the new MSI installer
2. Old version is automatically removed
3. New version is installed
4. **Your configuration is preserved** (`%APPDATA%\GrafaMon\config.json`)
5. **Your logs are preserved** (`%APPDATA%\GrafaMon\logs\`)

**No manual steps required!**

### **Clean Uninstall**

To perform a complete clean uninstall (removing all data):

1. **Uninstall via Add/Remove Programs:**
   - Open **Settings** → **Apps** → **Apps & features**
   - Search for "GrafaMon"
   - Click **Uninstall**

2. **Manually delete user data (optional):**
   ```cmd
   rmdir /s /q "%APPDATA%\GrafaMon"
   ```

⚠️ **Warning:** Deleting `%APPDATA%\GrafaMon` will permanently remove your configuration and logs.

### **First-Run Configuration**

When you first run GrafaMon after installation:
1. A configuration wizard will appear
2. Enter your Grafana URL and API key
3. Click "Save" to create `config.json`
4. The application will start monitoring

**No configuration file is installed by the MSI** - it's created on first run.

---

## 🔄 Configuration Changes

### **Applying Configuration Changes**

Configuration changes require an application restart:

1. Close the application
2. Edit `config.json`
3. Save the file
4. Restart the application

**Note:** There is no "hot reload" - changes are only read on startup.

---

## 📝 Configuration Schema (JSON Schema)

For IDE autocomplete and validation, here's the JSON Schema:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": [
    "GrafanaBaseUrl",
    "ApiKey",
    "GrafanaOrgId",
    "ActiveAlertsPath"
  ],
  "properties": {
    "GrafanaBaseUrl": {
      "type": "string",
      "format": "uri",
      "description": "Base URL of your Grafana instance"
    },
    "ApiKey": {
      "type": "string",
      "minLength": 1,
      "description": "Grafana API key (Service Account token)"
    },
    "GrafanaOrgId": {
      "type": "string",
      "minLength": 1,
      "description": "Grafana organization ID"
    },
    "ActiveAlertsPath": {
      "type": "string",
      "minLength": 1,
      "description": "API endpoint path for active alerts"
    },
    "PollingIntervalSeconds": {
      "type": "integer",
      "minimum": 5,
      "default": 60,
      "description": "How often to poll Grafana (in seconds)"
    },
    "EnableSoundNotifications": {
      "type": "boolean",
      "default": true,
      "description": "Enable or disable sound notifications"
    },
    "SoundFilePath": {
      "type": "string",
      "default": "",
      "description": "Path to custom .wav sound file (empty = system sound)"
    }
  }
}
```

---
