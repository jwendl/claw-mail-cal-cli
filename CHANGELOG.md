# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Account management commands: `account add`, `account list`, `account set`, `account delete`
- Login command using Entra ID device code flow with `AuthenticationRecord` stored in Azure Key Vault
- Email commands: `email list` (inbox and named folders), `email read`, `email send`
- Calendar commands: `calendar list`, `calendar read`
- `doctor` command to verify environment and configuration prerequisites
- Self-contained single-file publish targets for Windows x64 and Ubuntu x64
- CI workflow with build, unit tests, and format checks
- Release workflow that publishes binaries to GitHub Releases
- Developer setup guide in `docs/architecture/`

## [0.1.0] - 2025-01-01

### Added
- Initial project skeleton with .NET 10 / C# 13
- Spectre.Console.Cli command structure
- Azure Identity device code authentication
- Azure Key Vault integration for secure token storage
- Microsoft Graph SDK integration for mail and calendar access

[Unreleased]: https://github.com/jwendl/claw-mail-cal-cli/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/jwendl/claw-mail-cal-cli/releases/tag/v0.1.0
