# Architecture Documentation

This document describes the technical architecture of the GrafaMon - Grafana Alert Monitoring Desktop Notifier application.

---

## 🏗️ Overview

The application follows **Clean Architecture** principles with clear separation of concerns across four layers:

```
┌─────────────────────────────────────────────────────────────┐
│                         WPF UI Layer                        │
│  (MainWindow, AlertWidgetViewModel, Converters, XAML)       │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                     Application Layer                       │
│  (AlertPollingService, SoundNotificationService, Settings)  │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                     │
│     (GrafanaAlertsReader, HTTP Client, JSON Parsing)        │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                       Domain Layer                          │
│         (AlertDetail, AlertCounts, AlertSeverity)           │
└─────────────────────────────────────────────────────────────┘
```

---

## 📦 Layer Responsibilities

### **1. Domain Layer** (`GrafaMon.Domain`)

**Purpose:** Core business entities with no external dependencies.

**Key Types:**
- `AlertDetail` - Immutable record representing a single alert
- `AlertCounts` - Immutable record with critical/warning/info counts
- `AlertSeverity` - Enum (Unknown=0, Info=1, Warning=2, Critical=3)

**Design Principles:**
- ✅ **Immutable** - All types are records or readonly structs
- ✅ **No dependencies** - Pure C# with no external packages
- ✅ **Value objects** - Equality based on values, not identity

**Example:**
```csharp
public sealed record AlertDetail(
    string Environment,
    string MonitorTest,
    string Host,
    string Summary,
    AlertSeverity Severity,
    DateTime LastUpdatedUtc,
    DateTime StartsAtUtc,
    string GrafanaUrl
);
```

---

### **2. Application Layer** (`GrafaMon.Application`)

**Purpose:** Business logic and orchestration.

**Key Components:**

#### **AlertPollingService**
- Polls Grafana at configurable intervals using `PeriodicTimer`
- Raises events: `CountsUpdated`, `DetailsUpdated`, `PollingError`
- Handles cancellation gracefully
- Immediate first fetch (no initial delay)

**Design:**
```csharp
public sealed class AlertPollingService(IGrafanaAlertsReader reader, AppSettings settings)
{
    public event EventHandler<AlertCounts>? CountsUpdated;
    public event EventHandler<IReadOnlyList<AlertDetail>>? DetailsUpdated;
    public event EventHandler<string>? PollingError;

    public async Task RunAsync(CancellationToken cancellationToken);
}
```

#### **SoundNotificationService**
- Plays sound when new critical alerts are detected
- Supports custom `.wav` files or Windows system sounds
- 5-second cooldown to prevent sound spam
- Plays on background thread (non-blocking)

**Design:**
```csharp
public sealed class SoundNotificationService(AppSettings settings, ILogger logger)
{
    public void PlayCriticalAlertSound();
}
```

#### **AppSettings**
- Configuration model loaded from `config.json`
- Validated on startup
- Immutable after load

#### **ConfigurationHelper**
- Loads and validates `config.json`
- Provides detailed error messages for invalid config
- Validates URLs, file paths, API keys

---

### **3. Infrastructure Layer** (`GrafaMon.Infrastructure`)

**Purpose:** External integrations (HTTP, JSON, file system).

**Key Components:**

#### **GrafanaAlertsReader**
- Implements `IGrafanaAlertsReader` interface
- Uses `HttpClient` with 30-second timeout
- Streams JSON responses for efficiency
- Robust error handling with detailed logging
- Maps DTOs to domain models

**Design:**
```csharp
public sealed class GrafanaAlertsReader(HttpClient http, AppSettings settings, ILogger logger) 
    : IGrafanaAlertsReader
{
    public async Task<(AlertCounts Counts, IReadOnlyList<AlertDetail> Details)> 
        GetActiveAlertsAsync(CancellationToken cancellationToken);
}
```

**HTTP Request Flow:**
1. Build URL from `GrafanaBaseUrl` + `ActiveAlertsPath`
2. Add `Authorization: Bearer {ApiKey}` header
3. Add `X-Grafana-Org-Id` header
4. Send request with `HttpCompletionOption.ResponseHeadersRead`
5. Check status code, log error body if failed
6. Stream JSON response
7. Deserialize to `List<GrafanaAlertDto>`
8. Validate each DTO
9. Map to domain models
10. Return counts + details

#### **AlertDtoMapper**
- Maps Grafana API DTOs to domain models
- Handles null/missing fields gracefully
- Provides sensible defaults
- Trims whitespace
- Validates date ranges

**Mapping Logic:**
```csharp
public static AlertDetail MapToAlertDetail(GrafanaAlertDto dto)
{
    // Extract with fallbacks
    var severity = ParseSeverity(dto.Labels?.Severity);
    var environment = GetValueOrFallback(dto.Labels?.Environment, "unknown");
    var host = GetValueOrFallback(dto.Labels?.Host, null) 
        ?? GetValueOrFallback(dto.Labels?.Instance, "unknown");
    // ... etc
}
```

#### **DTOs (Data Transfer Objects)**
- `GrafanaAlertDto` - Matches Grafana API response
- `LabelsDto`, `AnnotationsDto`, `StatusDto` - Nested structures
- All properties nullable to handle varying JSON shapes
- Internal visibility (not exposed outside Infrastructure layer)

---

### **4. WPF UI Layer** (`GrafaMon.Wpf`)

**Purpose:** User interface and presentation logic.

**Key Components:**

#### **MainWindow.xaml**
- Borderless, transparent window
- Always on top
- Draggable
- Contains summary widget + detail popup

**XAML Structure:**
```xml
<Window>
  <Grid>
    <!-- Summary Widget (Border with TextBlock) -->
    <Border x:Name="RootBorder">
      <TextBlock Text="{Binding DisplayText}"/>
    </Border>

    <!-- Detail Popup (DataGrid) -->
    <Popup x:Name="DetailsPopup">
      <DataGrid ItemsSource="{Binding AlertDetails}"/>
    </Popup>
  </Grid>
</Window>
```

**Visual Features:**
- **Flashing Animation** - XAML `Storyboard` with `DataTrigger` on `FlashCritical`
- **Error Indicator** - Orange border + tooltip when `HasPollingError` is true
- **Severity Colors** - `SeverityToBackgroundConverter` for row backgrounds
- **Hover Highlighting** - `MultiDataTrigger` for severity-aware hover colors

#### **MainWindow.xaml.cs**
- Code-behind for event handling
- Subscribes to polling service events
- Updates ViewModel via `Dispatcher.Invoke`
- Manages popup show/hide on mouse hover
- Handles window dragging

**Event Flow:**
```
AlertPollingService.CountsUpdated
  ↓
MainWindow._poller_CountsUpdated (background thread)
  ↓
Dispatcher.Invoke (marshal to UI thread)
  ↓
AlertWidgetViewModel.UpdateCounts
  ↓
INotifyPropertyChanged.PropertyChanged
  ↓
WPF Data Binding updates UI
```

#### **AlertWidgetViewModel**
- Implements `INotifyPropertyChanged`
- Exposes properties for data binding
- Manages alert state (counts, details, errors)
- Formats display text
- Tracks last successful poll time

**Key Properties:**
```csharp
public int Critical { get; private set; }
public int Warning { get; private set; }
public int Info { get; private set; }
public bool FlashCritical { get; private set; }
public bool HasPollingError { get; private set; }
public string? LastPollingError { get; private set; }
public DateTime? LastSuccessfulPollUtc { get; private set; }
public string DisplayText { get; }
public ObservableCollection<AlertDetail> AlertDetails { get; }
```

**Update Methods:**
```csharp
public void UpdateCounts(AlertCounts counts);
public void UpdateDetails(IReadOnlyList<AlertDetail> details);
public void UpdatePollingError(string errorMessage);
public void ClearPollingError();
```

#### **SeverityToBackgroundConverter**
- `IValueConverter` for XAML data binding
- Converts severity string to `SolidColorBrush`
- Uses frozen brushes for performance

**Mapping:**
```csharp
"critical" → #8B0000 (Dark Red)
"warning"  → #B8860B (Dark Orange)
"info"     → #00008B (Dark Blue)
default    → #2B2B2B (Dark Gray)
```

#### **ObservableCollectionExtensions**
- Extension method `ReplaceAll<T>()` for efficient bulk updates
- Clears and adds items in one operation
- Triggers single `CollectionChanged` event

---

## 🔄 Data Flow

### **Startup Flow**

```
1. App.xaml.cs → Application_Startup
   ↓
2. ConfigurationHelper.LoadAndValidate("config.json")
   ↓
3. ServiceCollection → Register dependencies
   ↓
4. ServiceProvider.GetRequiredService<MainWindow>()
   ↓
5. MainWindow.Show()
   ↓
6. AlertPollingService.RunAsync(cancellationToken)
```

### **Polling Flow**

```
1. PeriodicTimer.WaitForNextTickAsync() → Tick
   ↓
2. GrafanaAlertsReader.GetActiveAlertsAsync()
   ↓
3. HttpClient.SendAsync() → Grafana API
   ↓
4. JsonSerializer.DeserializeAsync<List<GrafanaAlertDto>>()
   ↓
5. AlertDtoMapper.MapToAlertDetail() → Domain models
   ↓
6. AlertPollingService raises events:
   - CountsUpdated(AlertCounts)
   - DetailsUpdated(IReadOnlyList<AlertDetail>)
   ↓
7. MainWindow event handlers (background thread)
   ↓
8. Dispatcher.Invoke() → Marshal to UI thread
   ↓
9. AlertWidgetViewModel.UpdateCounts/UpdateDetails
   ↓
10. INotifyPropertyChanged → WPF updates UI
```

### **Error Flow**

```
1. GrafanaAlertsReader throws exception
   ↓
2. AlertPollingService catches exception
   ↓
3. AlertPollingService.PollingError event raised
   ↓
4. MainWindow._poller_PollingError (background thread)
   ↓
5. Dispatcher.Invoke() → Marshal to UI thread
   ↓
6. AlertWidgetViewModel.UpdatePollingError(message)
   ↓
7. HasPollingError = true
   ↓
8. WPF DataTrigger → Orange border + tooltip
```

### **Sound Notification Flow**

```
1. AlertPollingService.CountsUpdated event
   ↓
2. MainWindow checks if new critical alert detected
   ↓
3. SoundNotificationService.PlayCriticalAlertSound()
   ↓
4. Check cooldown (2 seconds since last sound)
   ↓
5. Task.Run() → Background thread
   ↓
6. SoundPlayer.LoadAsync() + PlaySync()
   ↓
7. Sound plays without blocking UI
```

---

## 🎨 Design Patterns

### **1. Dependency Injection**
- Constructor injection throughout
- `Microsoft.Extensions.DependencyInjection`
- Registered in `App.xaml.cs`

**Example:**
```csharp
services.AddSingleton<AppSettings>(settings);
services.AddSingleton<ILogger>(logger);
services.AddHttpClient<IGrafanaAlertsReader, GrafanaAlertsReader>();
services.AddSingleton<AlertPollingService>();
services.AddSingleton<SoundNotificationService>();
services.AddSingleton<AlertWidgetViewModel>();
services.AddSingleton<MainWindow>();
```

### **2. Repository Pattern**
- `IGrafanaAlertsReader` interface abstracts data access
- `GrafanaAlertsReader` implements concrete HTTP logic
- Easy to mock for testing

### **3. MVVM (Model-View-ViewModel)**
- **Model:** Domain entities (`AlertDetail`, `AlertCounts`)
- **View:** XAML (`MainWindow.xaml`)
- **ViewModel:** `AlertWidgetViewModel` with `INotifyPropertyChanged`

### **4. Event-Driven Architecture**
- Loose coupling between layers
- `AlertPollingService` raises events
- `MainWindow` subscribes to events
- Easy to add new subscribers

### **5. Immutable Domain Models**
- All domain types are `record` or readonly
- Thread-safe by design
- Prevents accidental mutations

### **6. Sealed Classes**
- All classes are `sealed` unless designed for inheritance
- Performance optimization (devirtualization)
- Clear intent (not designed for extension)

---

## 🔒 Thread Safety

### **UI Thread Marshaling**
All UI updates are marshaled to the UI thread using `Dispatcher.Invoke`:

```csharp
private void _poller_CountsUpdated(object? sender, AlertCounts counts)
{
    _rootBorder.Dispatcher.Invoke(() =>
    {
        _vm.UpdateCounts(counts);
    });
}
```

### **Background Operations**
- HTTP requests: Background thread (via `HttpClient`)
- JSON parsing: Background thread (via `JsonSerializer`)
- Sound playback: Background thread (via `Task.Run`)
- Polling loop: Background thread (via `PeriodicTimer`)

### **Immutable Data**
- Domain models are immutable (thread-safe)
- No shared mutable state between threads

---

## 🚀 Performance Optimizations

### **1. Frozen Brushes**
```csharp
private static readonly SolidColorBrush CriticalBrush = 
    new SolidColorBrush(Color.FromRgb(139, 0, 0)).AsFrozen();
```
- Frozen brushes are immutable and thread-safe
- Can be shared across threads
- Faster rendering

### **2. HttpCompletionOption.ResponseHeadersRead**
```csharp
await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
```
- Starts processing response before entire body is downloaded
- Reduces memory usage
- Faster time-to-first-byte

### **3. Stream-Based JSON Parsing**
```csharp
await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
var alertDtos = await JsonSerializer.DeserializeAsync<List<GrafanaAlertDto>>(stream, JsonOptions, cancellationToken);
```
- Parses JSON as it's downloaded
- Lower memory footprint
- Faster for large responses

### **4. ObservableCollection Bulk Updates**
```csharp
AlertDetails.ReplaceAll(sortedDetails);
```
- Single `CollectionChanged` event instead of N events
- Reduces UI redraws
- Faster for large lists

### **5. PeriodicTimer**
```csharp
_timer = new PeriodicTimer(period);
while (await _timer.WaitForNextTickAsync(cancellationToken))
{
    await PollOnce(cancellationToken);
}
```
- More efficient than `System.Timers.Timer`
- Async-friendly
- Proper cancellation support

### **6. Static Readonly JsonSerializerOptions**
```csharp
private static readonly JsonSerializerOptions JsonOptions = new() { ... };
```
- Created once, reused for all requests
- Zero allocations per request

---

## 🧪 Testing Strategy

### **Unit Tests**
- **AlertDtoMapper** 
- Tests null handling, whitespace, date parsing, severity mapping

### **Integration Tests** (Future)
- Mock Grafana API responses
- Test end-to-end polling flow
- Test error scenarios

### **Manual Testing Checklist**
- ✅ Normal operation (alerts display correctly)
- ✅ No alerts (empty state)
- ✅ Network failure (error indicator)
- ✅ Invalid API key (error indicator)
- ✅ Malformed JSON (graceful degradation)
- ✅ Sound playback (custom + system sounds)
- ✅ Window dragging
- ✅ Popup show/hide
- ✅ Flashing animation
- ✅ Severity colors
- ✅ Hover highlighting

---

## 📊 Logging Strategy

### **Structured Logging with Serilog**
```csharp
_logger.Debug("Fetching alerts from: {GrafanaUrl}", url);
_logger.Information("Application started");
_logger.Warning("Skipped {SkippedCount} malformed alerts", skippedCount);
_logger.Error(ex, "Failed to parse Grafana alert JSON response");
```

### **Log Enrichment**
- Thread ID
- Source context (class name)
- Timestamp

### **Log Sinks**
- File (rolling daily, 30-day retention)
- Console (during development)

---

## 🔐 Security Considerations

### **API Key Storage**
- Stored in `config.json` (encrypted)
- Logged as masked (`***`) in logs

### **HTTPS**
- Always use HTTPS for Grafana URL
- Certificate validation enabled by default

### **Input Validation**
- All config values validated on startup
- URLs validated for correct format
- File paths validated for existence

---

## 🗺️ Future Architecture Improvements

### **1. State Persistence**
- Save window position
- Save acknowledged alerts
- Save user preferences

### **2. Multi-Instance Support**
- Monitor multiple Grafana instances
- Aggregate alerts from multiple sources

### **3. Alert Filtering**
- User-defined filters (environment, severity, etc.)
- Regex-based filtering

### **4. Alert Actions**
- Acknowledge alerts
- Snooze alerts
- Open in browser

---
