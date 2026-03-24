# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- Release workflow now supports manual triggers via `workflow_dispatch` in addition to tag pushes, allowing maintainers to publish releases directly from the GitHub Actions UI by supplying a version number

## [0.1.0] - 2025-01-01

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

[Unreleased]: https://github.com/jwendl/claw-mail-cal-cli/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/jwendl/claw-mail-cal-cli/releases/tag/v0.1.0
