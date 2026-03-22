# claw-mail-cal-cli

An experimental command-line interface intended to provide access to email, people, and calendar items via Microsoft Graph. Designed for use by [OpenClaw](https://github.com/openclaw/openclaw) to provide mail and calendar capabilities without the complexity of MCP server authentication.

Planned authentication will use Entra ID's **device code flow**, with OAuth tokens stored securely in **Azure Key Vault** for subsequent reuse. These capabilities are under active development; see the [project roadmap](docs/roadmap.md) for current status.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Configuration](#configuration)
- [Usage](#usage)
  - [Account Management](#account-management)
  - [Login](#login)
  - [Email](#email)
  - [Calendar](#calendar)
- [Authentication Flow](#authentication-flow)
- [Technology Stack](#technology-stack)
- [Development](#development)
- [Contributing](#contributing)
- [License](#license)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) (logged in via `az login`)
- An Azure Key Vault instance (for token storage)
- A Microsoft Entra ID app registration with the following delegated permissions:
  - `Mail.ReadWrite`
  - `Calendars.ReadWrite`
  - `People.Read`

## Installation

### Build from Source

```bash
# Restore dependencies
dotnet restore src/ClawMailCalCli/ClawMailCalCli.csproj

# Build
dotnet build src/ClawMailCalCli/ClawMailCalCli.csproj --configuration Release

# Run
dotnet run --project src/ClawMailCalCli/ClawMailCalCli.csproj -- <command>
```

### Publish a Self-Contained Binary

**Windows x64:**

```bash
dotnet publish src/ClawMailCalCli/ClawMailCalCli.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --output ./publish/win-x64
```

**Ubuntu x64:**

```bash
dotnet publish src/ClawMailCalCli/ClawMailCalCli.csproj \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  --output ./publish/linux-x64
```

## Configuration

The CLI requires access to an Azure Key Vault to store and retrieve OAuth tokens. Configure the Key Vault URL before first use. Access to Key Vault is provided via the **Azure CLI Credential**, so you must run `az login` prior to using the tool.

## Usage

### Account Management

Add a new account:

```
claw-mail-cal-cli account add <name> <email>
```

List all configured accounts:

```
claw-mail-cal-cli account list
```

Set the active account context:

```
claw-mail-cal-cli account set <name>
```

Delete an account:

```
claw-mail-cal-cli account delete <name>
```

### Login

Authenticate an account using Entra ID device code flow:

```
claw-mail-cal-cli login <account-name>
```

You will be prompted to visit a verification URL and enter a code. Once authenticated, the access and refresh tokens are stored in Azure Key Vault. Subsequent commands will use the cached tokens automatically, refreshing them as needed.

### Email

List emails in your inbox:

```
claw-mail-cal-cli email list
```

List emails in a specific folder:

```
claw-mail-cal-cli email list <folder-name>
```

Read a specific email:

```
claw-mail-cal-cli email read <subject-name or unique-message-id>
```

Send an email:

```
claw-mail-cal-cli email send <to> <subject> <content>
```

> **Note:** Deleting emails is not available.

### Calendar

List calendar events:

```
claw-mail-cal-cli calendar list
```

Read a specific calendar event:

```
claw-mail-cal-cli calendar read <title-or-unique-calendar-item-id>
```

Create a calendar event:

```
claw-mail-cal-cli calendar create <title> <start-date-time> <end-date-time> <content>
```

## Authentication Flow

1. You run a command or explicitly call `login <account>`.
2. The CLI requests a device code from Entra ID.
3. You are prompted to visit a verification URL and enter the code.
4. The CLI polls Entra ID until authentication is complete.
5. Access and refresh tokens are stored in Azure Key Vault as `{account}_access_token` and `{account}_refresh_token`.
6. Subsequent commands retrieve tokens from Key Vault without requiring re-authentication.
7. If a `401 Unauthorized` error is received from Microsoft Graph, the CLI automatically retries the login flow (up to 3 times).

For a detailed diagram, see [Authentication Flow](docs/architecture/architecture.md#authentication-flow).

## Technology Stack

| Component | Technology |
|-----------|------------|
| Language | C# 13 / .NET 10 |
| CLI Framework | Spectre.Console.Cli |
| Graph Access | Microsoft Graph SDK (C#) |
| Authentication | Azure Identity (`DeviceCodeCredential`) |
| Token Storage | Azure Key Vault (via Azure CLI Credential) |
| Output Formatting | Spectre.Console |
| Target Platforms | Windows x64, Ubuntu x64 |

## Development

### Restore Dependencies

```bash
dotnet restore src/ClawMailCalCli/ClawMailCalCli.csproj
```

### Build

```bash
dotnet build src/ClawMailCalCli/ClawMailCalCli.csproj --configuration Release
```

### Run Tests

```bash
dotnet test tests/ --configuration Release --verbosity normal
```

### Format Code

```bash
dotnet format src/
```

### Architecture

See [`docs/architecture/architecture.md`](docs/architecture/architecture.md) for a full description of the system design, component overview, and sequence diagrams.

See [`docs/architecture/requirements.md`](docs/architecture/requirements.md) for project requirements and constraints.

## Contributing

Contributions are welcome! Contribution guidelines specific to this repository are being documented; in the meantime, please open an issue to discuss substantial changes before submitting a pull request.

## License

This project is licensed under the terms of the [LICENSE](LICENSE) file in the root of this repository.
