# Contributing to GrafaMon

Thank you for your interest in contributing to GrafaMon! We welcome contributions from the community.

## 🤝 How to Contribute

### Reporting Bugs

If you find a bug, please create an issue on GitHub with:
- **Clear title** - Describe the issue briefly
- **Steps to reproduce** - How to trigger the bug
- **Expected behavior** - What should happen
- **Actual behavior** - What actually happens
- **Environment** - OS version, .NET version, Grafana version
- **Logs** - Include relevant log files if available

### Suggesting Features

Feature requests are welcome! Please create an issue with:
- **Clear description** - What feature you'd like
- **Use case** - Why this feature would be useful
- **Examples** - Screenshots or mockups if applicable

### Pull Requests

We love pull requests! Here's how to contribute code:

1. **Fork the repository**
   ```bash
   git clone https://github.com/simon-weston/GrafaMon.git
   ```

2. **Create a feature branch**
   ```bash
   git checkout -b feature/amazing-feature
   ```

3. **Make your changes**
   - Follow the existing code style
   - Use Guard class for parameter validation
   - Write XML documentation for public APIs
   - Add unit tests for new functionality

4. **Run tests**
   ```bash
   dotnet test src/GrafaMon.Tests/GrafaMon.Tests.csproj
   ```

5. **Commit your changes**
   ```bash
   git commit -m "feat: add amazing feature"
   ```
   
   Follow [Conventional Commits](https://www.conventionalcommits.org/) format:
   - `feat:` - New feature
   - `fix:` - Bug fix
   - `docs:` - Documentation changes
   - `refactor:` - Code refactoring
   - `test:` - Adding tests
   - `chore:` - Maintenance tasks

6. **Push to your fork**
   ```bash
   git push origin feature/amazing-feature
   ```

7. **Open a Pull Request**
   - Provide a clear description of your changes
   - Link to any related issues
   - Ensure all tests pass
   - Request review from maintainers

## 💻 Development Setup

### Prerequisites

- Windows 10 1809+ or Windows 11
- .NET 8 SDK
- Visual Studio 2022 or JetBrains Rider
- WiX Toolset (for installer builds)

### Building the Project

```powershell
# Restore dependencies
dotnet restore src/GrafaMon.sln

# Build
./Build-app.ps1

# Run tests
dotnet test src/GrafaMon.Tests/GrafaMon.Tests.csproj
```

## 📏 Code Style Guidelines

### General Principles

- **Clean Architecture** - Maintain separation of concerns
- **SOLID Principles** - Follow SOLID design principles
- **Guard Validation** - Use Guard class for all parameter validation
- **XML Documentation** - Document all public APIs

### C# Style

- Use **C# 12** language features
- Prefer **records** for immutable data
- Use **sealed classes** unless designed for inheritance
- Use **nullable reference types**
- Follow **Microsoft C# coding conventions**

### Example: Using Guard Class

```csharp
public void ProcessData(string data, int maxLength)
{
    // ✅ Good - Use Guard for validation
    Guard.AgainstNullOrWhiteSpace(data);
    Guard.AgainstNegativeOrZero(maxLength);
    
    // ... implementation
}
```

### Testing

- Write unit tests for all new features
- Aim for 100% coverage on critical components
- Use NUnit framework
- Follow Arrange-Act-Assert pattern

```csharp
[Test]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var input = "test";
    
    // Act
    var result = Method(input);
    
    // Assert
    Assert.That(result, Is.EqualTo(expected));
}
```

## 🛡️ Guard Class Usage

When adding validation:

1. **Check if a Guard method exists** - Review [Guard documentation](./src/docs/GUARD_USAGE.md)
2. **Use existing Guard methods** - Don't create manual validation
3. **Propose new Guard methods** - If needed, add to Guard class with tests

## 📝 Documentation

- Update **README.md** if you add user-facing features
- Update **ARCHITECTURE.md** if you change architecture
- Add examples to **./src/docs/** if you add new Guard methods
- Update **CHANGELOG.md** with your changes

## 🔒 Security

- **Never commit secrets** - API keys, passwords, tokens
- **Use DPAPI** - For encrypting sensitive data
- **Report vulnerabilities privately** - See SECURITY.md

## 📄 License

By contributing, you agree that your contributions will be licensed under the GPLv3 License.

## ❓ Questions?

- **GitHub Discussions** - For questions and general discussion
- **GitHub Issues** - For bugs and feature requests

Thank you for contributing to GrafaMon! 🎉
