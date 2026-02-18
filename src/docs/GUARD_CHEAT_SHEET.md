# Guard Cheat Sheet

**Ultra-compact reference - one method, one line**

```csharp
using GrafaMon.Application.Guards;
```

## All Methods (19)

```csharp
// Null & String
Guard.AgainstNull(value)                              // value == null
Guard.AgainstNullOrWhiteSpace(text)                   // string.IsNullOrWhiteSpace(text)
Guard.AgainstNullOrEmpty(text)                        // string.IsNullOrEmpty(text)
Guard.AgainstTooLong(text, maxLength)                 // text.Length > maxLength

// Numeric
Guard.AgainstNegativeOrZero(num)                      // num <= 0
Guard.AgainstNegative(num)                            // num < 0
Guard.AgainstLessThan(num, min)                       // num < min
Guard.AgainstOutOfRange(num, min, max)                // num < min || num > max

// Network
Guard.AgainstInvalidPort(port)                        // port < 1 || port > 65535
Guard.AgainstInvalidUrl(url)                          // !Uri.TryCreate(url, HTTP/HTTPS)
Guard.AgainstInvalidEmail(email)                      // invalid email format

// File System
Guard.AgainstFileNotFound(filePath)                   // !File.Exists(filePath)
Guard.AgainstDirectoryNotFound(dirPath)               // !Directory.Exists(dirPath)
Guard.AgainstInvalidPath(path)                        // path contains invalid chars
Guard.AgainstInvalidFileExtension(path, ext)          // Path.GetExtension(path) != ext

// Collection & Other
Guard.AgainstNullOrEmptyCollection(collection)        // collection == null || !Any()
Guard.AgainstUndefinedEnum(enumValue)                 // !Enum.IsDefined(enumValue)
Guard.AgainstEmptyGuid(guid)                          // guid == Guid.Empty
Guard.AgainstCondition(condition, message)            // !condition
```

## Common Patterns

```csharp
// Constructor
Guard.AgainstNull(logger);
Guard.AgainstNull(settings);

// Config
Guard.AgainstNullOrWhiteSpace(apiKey);
Guard.AgainstInvalidUrl(baseUrl);
Guard.AgainstLessThan(interval, 5);

// Files
Guard.AgainstFileNotFound(configPath);
Guard.AgainstInvalidFileExtension(soundFile, ".wav");

// Custom
Guard.AgainstCondition(isValid, "Must be valid");
```

## Exception Types

| Guard | Throws |
|-------|--------|
| AgainstNull | ArgumentNullException |
| AgainstNullOrWhiteSpace/Empty | ArgumentException |
| AgainstNegative/Zero/LessThan/OutOfRange | ArgumentOutOfRangeException |
| AgainstInvalidPort | ArgumentOutOfRangeException |
| AgainstInvalidUrl/Email/Path/FileExtension | ArgumentException |
| AgainstFileNotFound | FileNotFoundException |
| AgainstDirectoryNotFound | DirectoryNotFoundException |
| AgainstNullOrEmptyCollection | ArgumentException |
| AgainstUndefinedEnum | ArgumentException |
| AgainstEmptyGuid | ArgumentException |
| AgainstCondition | InvalidOperationException |

## Before ? After

```csharp
// Before (8 lines)
if (string.IsNullOrWhiteSpace(configPath))
    throw new ArgumentException("...", nameof(configPath));
if (!File.Exists(configPath))
    throw new FileNotFoundException($"...");

// After (2 lines)
Guard.AgainstNullOrWhiteSpace(configPath);
Guard.AgainstFileNotFound(configPath);
```

**75% less code! ??**

---

See [GUARD_QUICK_REFERENCE.md](./src/docs/GUARD_QUICK_REFERENCE.md) or [GUARD_USAGE.md](./src/docs/GUARD_USAGE.md) for more details.
