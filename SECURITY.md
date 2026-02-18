# Security Policy

## Supported Versions

We release patches for security vulnerabilities for the following versions:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please report them via email to: **simon@weston.me.uk**

You should receive a response within 96 hours. If for some reason you do not,
please follow up via email to ensure we received your original message.

Please include the following information:

- Type of issue (e.g., buffer overflow, SQL injection, cross-site scripting)
- Full paths of source file(s) related to the issue
- Location of the affected source code (tag/branch/commit or direct URL)
- Any special configuration required to reproduce the issue
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the issue, including how an attacker might exploit it

## Preferred Languages

We prefer all communications to be in English.

## Disclosure Policy

When we receive a security bug report, we will:

1. Confirm the problem and determine affected versions
2. Prepare fixes for all supported versions
3. Release new versions as soon as possible

## Security Best Practices for Users

### API Key Security
- Store your Grafana API keys securely
- Use GrafaMon's built-in DPAPI encryption
- Rotate API keys regularly (every 90 days recommended)
- Use read-only service account tokens

### Configuration Security
- Protect config.json file permissions
- Don't share configuration files containing API keys
- Review logs for sensitive information before sharing

### System Security
- Keep .NET Runtime updated
- Keep Windows updated
- Use HTTPS for Grafana connections
- Enable firewall for network protection

## Known Security Considerations

### API Key Storage
GrafaMon uses DPAPI (Data Protection API) to encrypt API keys in the
configuration file. This provides user-level encryption on Windows. The
encrypted data can only be decrypted by the same user on the same machine.

**Note:** If you move the configuration file to another machine or user
account, you will need to re-enter the API key.

### Network Communication
All communication with Grafana should use HTTPS. HTTP connections are supported
but not recommended for production use.

## Acknowledgments

We appreciate security researchers who responsibly disclose vulnerabilities to
help keep GrafaMon and its users safe.

Researchers who report valid security issues will be acknowledged in release
notes (unless they prefer to remain anonymous).
