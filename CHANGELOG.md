# Changelog

All notable changes to GrafaMon (Grafana Alert Monitoring Desktop Notifier) will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- Force refresh button for manual alert updates
- Window position persistence (save/restore on exit/startup)
- Multiple Grafana instance support
- Context menu in detail screen with quick links (HTTPS/RDP to host)

## Build v26.49.1 - 2026-02-18

### Added

#### Core Features
- **Real-time Grafana alert monitoring** with visual notifications
- **Always-on-top summary widget** showing alert counts (Critical, Warning, Info)
- **Detailed alert table** on hover with severity-based highlighting
- **Sound notifications** for critical alerts with 5-second cooldown protection
- **Configurable polling intervals** (minimum 5 seconds, default 60 seconds)
- **Custom .wav sound file support** with fallback to Windows system sounds
- **Double-click to open** alert details in Grafana browser

#### Visual Features
- **Flashing animation** for critical alerts (animated red border)
- **Severity-based row highlighting** (Critical=Red, Warning=Orange, Info=Blue)
- **Error indicators** with orange border and tooltip when polling fails
- **Severity-aware hover effects** that brighten on mouse hover
- **Clock symbol** indicating polling status and last update time

#### Architecture & Code Quality
- **Clean Architecture** design with layered structure
- **Guard validation framework** with 19 validation methods (66% code reduction)
- **Dependency Injection** using Microsoft.Extensions.DependencyInjection
- **MVVM pattern** with AlertWidgetViewModel
- **Event-driven architecture** for loose coupling

#### Security Features
- **DPAPI encryption** for Grafana API keys
- **Secure configuration management** with automatic encryption/decryption
- **Input validation** using Guard framework throughout codebase
- **No plain-text credentials** in logs (API keys masked)

#### Testing
- **170+ comprehensive unit tests** with NUnit
- **100% test coverage** for Guard class (120 tests)
- **100% coverage** for AlertDtoMapper (18 tests)
- **100% coverage** for SecureConfigurationManager (14 tests)

#### Documentation
- Comprehensive README with installation and configuration
- Guard framework documentation suite 
- Architecture, Configuration, and Installation guides
- Contributing guidelines and Code of Conduct

### Security
- DPAPI encryption for API key storage
- Input validation prevents injection attacks
- HTTPS enforcement for Grafana connections
- No secrets in logs or source code

### Performance
- Frozen WPF brushes for faster rendering
- Stream-based JSON parsing for lower memory usage
- Efficient async polling with PeriodicTimer
- Guard methods with <2 nanosecond overhead

## Links
- [GitHub Repository](https://github.com/simon-weston/GrafaMon)
- [Issue Tracker](https://github.com/simon-weston/GrafaMon/issues)
- [Releases](https://github.com/simon-weston/GrafaMon/releases)

[v26.49.1]: https://github.com/simon-weston/GrafaMon/releases/tag/v26.49.1
