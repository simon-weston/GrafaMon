# Guard Class API Reference

Complete API reference for all Guard validation methods.

---

## Namespace

```csharp
GrafaMon.Application.Guards
```

---

## Class Declaration

```csharp
public static class Guard
```

A static utility class providing parameter validation guard clauses.

---

## Methods

### Null Validation

#### AgainstNull&lt;T&gt;

```csharp
public static void AgainstNull<T>(
    [NotNull] T? value,
    [CallerArgumentExpression(nameof(value))] string? paramName = null)
    where T : class
```

**Description:**  
Throws `ArgumentNullException` if the value is null.

**Type Parameters:**
- `T` - The type of the parameter (must be a reference type)

**Parameters:**
- `value` - The value to check
- `paramName` - The parameter name (automatically captured via `CallerArgumentExpression`)

**Exceptions:**
- `ArgumentNullException` - Thrown when value is null

**Example:**
```csharp
public void ProcessData(ILogger logger, AppSettings settings)
{
    Guard.AgainstNull(logger);
    Guard.AgainstNull(settings);
    
    logger.Information("Processing with settings: {Settings}", settings);
}
```

---

### String Validation

#### AgainstNullOrWhiteSpace

```csharp
public static void AgainstNullOrWhiteSpace(
    [NotNull] string? value,
    [CallerArgumentExpression(nameof(value))] string? paramName = null)
```

**Description:**  
Throws `ArgumentException` if the string is null, empty, or contains only whitespace.

**Parameters:**
- `value` - The string value to check
- `paramName` - The parameter name (automatically captured)

**Exceptions:**
- `ArgumentException` - Thrown when string is null, empty, or whitespace

**Example:**
```csharp
public void SaveConfig(string configPath)
{
    Guard.AgainstNullOrWhiteSpace(configPath);
    File.WriteAllText(configPath, config);
}
```

---

#### AgainstNullOrEmpty

```csharp
public static void AgainstNullOrEmpty(
    [NotNull] string? value,
    [CallerArgumentExpression(nameof(value))] string? paramName = null)
```

**Description:**  
Throws `ArgumentException` if the string is null or empty. **Allows whitespace**.

**Parameters:**
- `value` - The string value to check
- `paramName` - The parameter name (automatically captured)

**Exceptions:**
- `ArgumentException` - Thrown when string is null or empty

**Example:**
```csharp
public void ValidatePassword(string password)
{
    Guard.AgainstNullOrEmpty(password);  // "   " is valid
    // Further validation...
}
```

---

#### AgainstTooLong

```csharp
public static void AgainstTooLong(
    string value,
    int maxLength,
    [CallerArgumentExpression(nameof(value))] string? paramName = null)
```

**Description:**  
Throws `ArgumentException` if the string exceeds the maximum allowed length.

**Parameters:**
- `value` - The string value to check
- `maxLength` - The maximum allowed length
- `paramName` - The parameter name (automatically captured)

**Exceptions:**
- `ArgumentException` - Thrown when string is null, whitespace, or exceeds max length
- `ArgumentOutOfRangeException` - Thrown when maxLength is ? 0

**Example:**
```csharp
public void SetDescription(string description)
{
    Guard.AgainstTooLong(description, 500);  // Max 500 characters
    _description = description;
}
```

---

### Numeric Validation

#### AgainstNegativeOrZero

```csharp
public static void AgainstNegativeOrZero(
    int value,
    [CallerArgumentExpression(nameof(value))] string? paramName = null)
```

**Description:**  
Throws `ArgumentOutOfRangeException` if the value is less than or equal to zero.

**Parameters:**
- `value` - The integer value to check
- `paramName` - The parameter name (automatically captured)

**Exceptions:**
- `ArgumentOutOfRangeException` - Thrown when value ? 0

**Example:**
```csharp
public void SetPollingInterval(int intervalSeconds)
{
    Guard.AgainstNegativeOrZero(intervalSeconds);
    _interval = TimeSpan.FromSeconds(intervalSeconds);
}
```

---

#### AgainstNegative

```csharp
public static void AgainstNegative(
    int value,
    [CallerArgumentExpression(nameof(value))] string? paramName = null)
```

**Description:**  
Throws `ArgumentOutOfRangeException` if the value is negative. **Allows zero**.

**Parameters:**
- `value` - The integer value to check
- `paramName` - The parameter name (automatically captured)

**Exceptions:**
- `ArgumentOutOfRangeException` - Thrown when value < 0

**Example:**
```csharp
public void SetRetryCount(int retryCount)
{
    Guard.AgainstNegative(retryCount);  // 0 is valid
    _retryCount = retryCount;
}
```

---

#### AgainstLessThan&lt;T&gt;

```csharp
public static void AgainstLessThan<T>(
    T value,
    T min,
    [CallerArgumentExpression(nameof(value))] string? paramName = null)
    where T : IComparable<T>
```

**Description:**  
Throws `ArgumentOutOfRangeException` if the value is less than the minimum (exclusive).

**Type Parameters:**
- `T` - Any type implementing `IComparable<T>`

**Parameters:**
- `value` - The value to check
- `min` - The minimum allowed value (inclusive)
- `paramName` - The parameter name (automatically captured)

**Exceptions:**
- `ArgumentOutOfRangeException` - Thrown when value < min

**Example:**
```csharp
public void SetPollingInterval(int intervalSeconds)
{
    Guard.AgainstLessThan(intervalSeconds, 5);  // Must be >= 5
    _interval = TimeSpan.FromSeconds(intervalSeconds);
}
```

---

#### AgainstOutOfRange&lt;T&gt;

```csharp
public static void AgainstOutOfRange<T>(
    T value,
    T min,
    T max,
    [CallerArgumentExpression(nameof(value))] string? paramName = null)
    where T : IComparable<T>
```

**Description:**  
Throws `ArgumentOutOfRangeException` if the value is outside the specified range.

**Type Parameters:**
- `T` - Any type implementing `IComparable<T>`

**Parameters:**
- `value` - The value to check
- `min` - The minimum allowed value (inclusive)
- `max` - The maximum allowed value (inclusive)
- `paramName` - The parameter name (automatically captured)

**Exceptions:**
- `ArgumentOutOfRangeException` - Thrown when value < min or value > max

**Example:**
```csharp
public void SetPercentage(int percentage)
{
    Guard.AgainstOutOfRange(percentage, 0, 100);
    _percentage = percentage;
}
```

---

### Network Validation

#### AgainstInvalidPort

```csharp
public static void AgainstInvalidPort(
    int port,
    [CallerArgumentExpression(nameof(port))] string? paramName = null)
```

**Description:**  
Throws `ArgumentOutOfRangeException` if the port number is not in the valid TCP/UDP range (1-65535).

**Parameters:**
- `port` - The port number to validate
- `paramName` - The parameter name (automatically captured)

**Exceptions:**
- `ArgumentOutOfRangeException` - Thrown when port < 1 or port > 65535

**Example:**
```csharp
public void ConnectToServer(string host, int port)
{
    Guard.AgainstInvalidPort(port);
    _client.Connect(host, port);
}
```

---

#### AgainstInvalidUrl

```csharp
public static void AgainstInvalidUrl(
    string url,
    [CallerArgumentExpression(nameof(url))] string? paramName = null)
```

**Description:**  
Throws `ArgumentException` if the URL is not a valid HTTP or HTTPS URL.

**Parameters:**
- `url` - The URL string to validate
- `paramName` - The parameter name (automatically captured)

**Exceptions:**
- `ArgumentException` - Thrown when URL is null, whitespace, or not a valid HTTP/HTTPS URL

**Example:**
```csharp
public void SetGrafanaUrl(string grafanaBaseUrl)
{
    Guard.AgainstInvalidUrl(grafanaBaseUrl);
    _baseUrl = grafanaBaseUrl;
}
```

---

#### AgainstInvalidEmail

```csharp
public static void AgainstInvalidEmail(
    string email,
    [CallerArgumentExpression(nameof(email))] string? paramName = null)
```

**Description:**  
Throws `ArgumentException` if the email address format is invalid.

**Validation Rules:**
- Must contain exactly one '@' symbol
- '@' cannot be at start or end
- Must have at least one '.' after '@'
- '.' cannot be at start or end of domain

**Parameters:**
- `email` - The email address to validate
- `paramName` - The parameter name (automatically captured)

**Exceptions:**
- `ArgumentException` - Thrown when email is null, whitespace, or invalid format

**Example:**
```csharp
public void RegisterUser(string email)
{
    Guard.AgainstInvalidEmail(email);
    _userEmail = email;
}
```

**Note:** This is a basic format check. For production email validation, consider using a comprehensive regex or library.

---

### File System Validation

#### AgainstFileNotFound

```csharp
public static void AgainstFileNotFound(
    string filePath,
    [CallerArgumentExpression(nameof(filePath))] string? paramName = null)
```

**Description:**  
Throws `FileNotFoundException` if the file does not exist.

**Parameters:**
- `filePath` - The file path to check
- `paramName` - The parameter name (automatically captured)

**Exceptions:**
- `ArgumentException` - Thrown when filePath is null or whitespace
- `FileNotFoundException` - Thrown when file does not exist

**Example:**
```csharp
public AppSettings LoadConfig(string configPath)
{
    Guard.AgainstFileNotFound(configPath);
    return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(configPath));
}
```

---

#### AgainstDirectoryNotFound

```csharp
public static void AgainstDirectoryNotFound(
    string directoryPath,
    [CallerArgumentExpression(nameof(directoryPath))] string? paramName = null)
```

**Description:**  
Throws `DirectoryNotFoundException` if the directory does not exist.

**Parameters:**
- `directoryPath` - The directory path to check
- `paramName` - The parameter name (automatically captured)

**Exceptions:**
- `ArgumentException` - Thrown when directoryPath is null or whitespace
- `DirectoryNotFoundException` - Thrown when directory does not exist

**Example:**
```csharp
public void WriteLogsToDirectory(string logDirectory)
{
    Guard.AgainstDirectoryNotFound(logDirectory);
    File.WriteAllText(Path.Combine(logDirectory, "app.log"), logs);
}
```

---

#### AgainstInvalidPath

```csharp
public static void AgainstInvalidPath(
    string path,
    [CallerArgumentExpression(nameof(path))] string? paramName = null)
```

**Description:**  
Throws `ArgumentException` if the path contains invalid characters.

**Invalid Characters:**
- All characters from `Path.GetInvalidPathChars()`
- Additional: `<`, `>`, `|`, `"`, `*`, `?`

**Parameters:**
- `path` - The file or directory path to validate
- `paramName` - The parameter name (automatically captured)

**Exceptions:**
- `ArgumentException` - Thrown when path is null, whitespace, or contains invalid characters

**Example:**
```csharp
public void CreateFile(string filePath)
{
    Guard.AgainstInvalidPath(filePath);
    File.Create(filePath);
}
```

---

#### AgainstInvalidFileExtension

```csharp
public static void AgainstInvalidFileExtension(
    string filePath,
    string expectedExtension,
    [CallerArgumentExpression(nameof(filePath))] string? paramName = null)
```

**Description:**  
Throws `ArgumentException` if the file extension doesn't match the expected extension.

**Parameters:**
- `filePath` - The file path to check
- `expectedExtension` - The expected file extension (e.g., ".wav", ".json")
- `paramName` - The parameter name (automatically captured)

**Exceptions:**
- `ArgumentException` - Thrown when filePath or expectedExtension is null/whitespace, or extension doesn't match

**Example:**
```csharp
public void LoadSoundFile(string soundFilePath)
{
    Guard.AgainstInvalidFileExtension(soundFilePath, ".wav");
    using var player = new SoundPlayer(soundFilePath);
    player.Play();
}
```

**Note:** Extension comparison is case-insensitive.

---

### Collection Validation

#### AgainstNullOrEmptyCollection&lt;T&gt;

```csharp
public static void AgainstNullOrEmptyCollection<T>(
    System.Collections.Generic.IEnumerable<T>? collection,
    [CallerArgumentExpression(nameof(collection))] string? paramName = null)
```

**Description:**  
Throws `ArgumentException` if the collection is null or empty.

**Type Parameters:**
- `T` - The type of elements in the collection

**Parameters:**
- `collection` - The collection to check
- `paramName` - The parameter name (automatically captured)

**Exceptions:**
- `ArgumentException` - Thrown when collection is null or contains no elements

**Example:**
```csharp
public void ProcessAlerts(IEnumerable<AlertDetail> alerts)
{
    Guard.AgainstNullOrEmptyCollection(alerts);
    
    foreach (var alert in alerts)
    {
        ProcessAlert(alert);
    }
}
```

**Note:** Uses LINQ `Any()` method, so may enumerate the collection once.

---

### Enum Validation

#### AgainstUndefinedEnum&lt;TEnum&gt;

```csharp
public static void AgainstUndefinedEnum<TEnum>(
    TEnum value,
    [CallerArgumentExpression(nameof(value))] string? paramName = null)
    where TEnum : struct, Enum
```

**Description:**  
Throws `ArgumentException` if the enum value is not defined in the enum type.

**Type Parameters:**
- `TEnum` - The enum type (must be a value type and an enum)

**Parameters:**
- `value` - The enum value to check
- `paramName` - The parameter name (automatically captured)

**Exceptions:**
- `ArgumentException` - Thrown when enum value is not defined in TEnum

**Example:**
```csharp
public enum LogLevel { Debug, Info, Warning, Error }

public void SetLogLevel(LogLevel level)
{
    Guard.AgainstUndefinedEnum(level);  // Prevents (LogLevel)99
    _logLevel = level;
}
```

---

### Other Validation

#### AgainstEmptyGuid

```csharp
public static void AgainstEmptyGuid(
    Guid guid,
    [CallerArgumentExpression(nameof(guid))] string? paramName = null)
```

**Description:**  
Throws `ArgumentException` if the GUID is empty (`Guid.Empty`).

**Parameters:**
- `guid` - The GUID to check
- `paramName` - The parameter name (automatically captured)

**Exceptions:**
- `ArgumentException` - Thrown when GUID equals `Guid.Empty`

**Example:**
```csharp
public void TrackRequest(Guid correlationId)
{
    Guard.AgainstEmptyGuid(correlationId);
    _logger.Information("Processing request {CorrelationId}", correlationId);
}
```

---

#### AgainstCondition

```csharp
public static void AgainstCondition(bool condition, string message)
```

**Description:**  
Throws `InvalidOperationException` if the condition is false. Use for complex custom validations.

**Parameters:**
- `condition` - The condition to check (must be true)
- `message` - The error message to include in the exception

**Exceptions:**
- `InvalidOperationException` - Thrown when condition is false

**Example:**
```csharp
public void ValidateLogLevel(string logLevel)
{
    var validLevels = new[] { "debug", "information", "warning", "error", "fatal" };
    
    Guard.AgainstCondition(
        Array.Exists(validLevels, l => l.Equals(logLevel, StringComparison.OrdinalIgnoreCase)),
        $"LogLevel must be one of: {string.Join(", ", validLevels)}. Got: {logLevel}"
    );
}
```

---

## CallerArgumentExpression

All Guard methods use the `[CallerArgumentExpression]` attribute to automatically capture parameter names. This eliminates the need to manually specify `nameof(parameter)`:

```csharp
// Automatically captures "logger"
Guard.AgainstNull(logger);

// Equivalent to (but more concise than):
Guard.AgainstNull(logger, nameof(logger));
```

**Requirements:**
- C# 10.0 or later
- .NET 6.0 or later

---

## Exception Types

| Guard Method | Exception Type |
|-------------|----------------|
| AgainstNull | `ArgumentNullException` |
| AgainstNullOrWhiteSpace | `ArgumentException` |
| AgainstNullOrEmpty | `ArgumentException` |
| AgainstTooLong | `ArgumentException` |
| AgainstNegativeOrZero | `ArgumentOutOfRangeException` |
| AgainstNegative | `ArgumentOutOfRangeException` |
| AgainstLessThan | `ArgumentOutOfRangeException` |
| AgainstOutOfRange | `ArgumentOutOfRangeException` |
| AgainstInvalidPort | `ArgumentOutOfRangeException` |
| AgainstInvalidUrl | `ArgumentException` |
| AgainstInvalidEmail | `ArgumentException` |
| AgainstFileNotFound | `FileNotFoundException` |
| AgainstDirectoryNotFound | `DirectoryNotFoundException` |
| AgainstInvalidPath | `ArgumentException` |
| AgainstInvalidFileExtension | `ArgumentException` |
| AgainstNullOrEmptyCollection | `ArgumentException` |
| AgainstUndefinedEnum | `ArgumentException` |
| AgainstEmptyGuid | `ArgumentException` |
| AgainstCondition | `InvalidOperationException` |

---

## See Also

- [Guard Usage Guide](../src/docs/GUARD_USAGE.md) - Examples and best practices
- [Guard Tests](../src/GrafaMon.Tests/Guards/GuardTests.cs) - 120+ test examples
- [Guard Source Code](../src/GrafaMon.Application/Guards/Guard.cs) - Implementation
