# Developer Setup Guide

This guide walks you through setting up **claw-mail-cal-cli** for local development from scratch.

## Prerequisites

| Requirement | Minimum Version | Notes |
|---|---|---|
| .NET SDK | 10.0 | Required to build and run the CLI |
| Azure CLI | 2.50+ | Required for Key Vault access and authentication |
| Azure subscription | — | Required for Azure Key Vault |

---

## Step 1 — Install the .NET 10 SDK

Download and install the .NET 10 SDK from the official Microsoft page:

```
https://dotnet.microsoft.com/en-us/download/dotnet/10.0
```

Verify the installation:

```bash
dotnet --version
```

Expected output: `10.0.x` or later.

---

## Step 2 — Install the Azure CLI

Follow the official installation guide for your operating system:

```
https://learn.microsoft.com/en-us/cli/azure/install-azure-cli
```

Verify the installation:

```bash
az --version
```

Expected output includes a line such as `azure-cli  2.58.0`.

---

## Step 3 — Authenticate with Azure CLI

Log in with your Azure account:

```bash
az login
```

This opens a browser window for interactive sign-in. After a successful login, verify the active subscription:

```bash
az account show
```

If you have multiple subscriptions, set the one that contains your Key Vault:

```bash
az account set --subscription "<subscription-id-or-name>"
```

---

## Step 4 — Create an Azure Key Vault

claw-mail-cal-cli stores OAuth tokens in Azure Key Vault. Create a vault in your subscription:

```bash
az keyvault create \
  --name "<your-unique-vault-name>" \
  --resource-group "<your-resource-group>" \
  --location "<your-region>"
```

> **Note:** Key Vault names must be globally unique (3–24 alphanumeric characters and hyphens).

Grant your own identity access to read and write secrets:

```bash
az keyvault set-policy \
  --name "<your-unique-vault-name>" \
  --upn "<your-email@example.com>" \
  --secret-permissions get set list delete
```

Note the vault URI from the output. It will look like:

```
https://<your-unique-vault-name>.vault.azure.net/
```

---

## Step 5 — Register an Entra App for Device Code Flow

claw-mail-cal-cli authenticates users via the Entra ID device code flow. Follow these steps to register an application:

1. Go to the [Azure Portal](https://portal.azure.com) and navigate to **Microsoft Entra ID → App registrations**.
2. Click **New registration**.
3. Enter a name (e.g., `claw-mail-cal-cli`).
4. Under **Supported account types**, select **Accounts in this organizational directory only** (or **Multitenant** if needed).
5. Under **Redirect URI**, select **Public client / native (mobile & desktop)** and enter:
   ```
   https://login.microsoftonline.com/common/oauth2/nativeclient
   ```
6. Click **Register**.
7. On the app **Overview** page, copy the **Application (client) ID** and **Directory (tenant) ID**.
8. Navigate to **Authentication** → enable **Allow public client flows** → **Save**.
9. Navigate to **API permissions** → **Add a permission** → **Microsoft Graph** → **Delegated permissions**.
10. Add the following permissions:
    - `Mail.ReadWrite`
    - `Mail.Send`
    - `Calendars.ReadWrite`
    - `User.Read`
11. Click **Grant admin consent** if you are an administrator (or ask your admin to do so).

---

## Step 6 — Create the Configuration File

Create the configuration directory and file:

```bash
mkdir -p ~/.claw-mail-cal-cli
```

Create `~/.claw-mail-cal-cli/config.json` with the following content, replacing the placeholder values:

```json
{
  "keyVaultUri": "https://<your-unique-vault-name>.vault.azure.net/"
}
```

> **Note:** The `defaultAccount` field is optional at this stage. It will be set automatically when you run `claw-mail-cal-cli account set <name>`.

---

## Step 7 — Configure `appsettings.json`

Open `src/ClawMailCalCli/appsettings.json` and update the `entra` and `keyVault` sections with your Entra app registration details:

```json
{
  "entra": {
    "tenantId": "<your-tenant-id>",
    "clientId": "<your-app-client-id>"
  },
  "keyVault": {
    "vaultUri": "https://<your-unique-vault-name>.vault.azure.net/"
  }
}
```

> **Security:** Never commit real tenant IDs, client IDs, or vault URIs to source control in a shared repository. For team environments, use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) or environment variables instead.

To configure using environment variables instead of editing `appsettings.json`:

```bash
export entra__tenantId="<your-tenant-id>"
export entra__clientId="<your-app-client-id>"
export keyVault__vaultUri="https://<your-unique-vault-name>.vault.azure.net/"
```

---

## Step 8 — Build and Run the Doctor Check

Build the project to confirm everything compiles:

```bash
dotnet build src/ClawMailCalCli/ClawMailCalCli.csproj --configuration Release
```

Run the `doctor` command to verify your environment:

```bash
dotnet run --project src/ClawMailCalCli/ClawMailCalCli.csproj -- doctor
```

Expected output when all checks pass:

```
Checking environment...

✓ Azure CLI installed (v2.58.0)
✓ Azure CLI logged in (user@example.com)
✓ Config file found (~/.claw-mail-cal-cli/config.json)
✓ Key Vault reachable (https://my-kv.vault.azure.net/)
✓ Default account set (work)

All checks passed.
```

If any check fails, the output includes a **Fix** hint explaining the corrective action.

---

## Step 9 — Add an Account and Log In

Add a named account (this stores the account name and email in a local SQLite database):

```bash
dotnet run --project src/ClawMailCalCli/ClawMailCalCli.csproj -- account add myaccount user@example.com
```

Authenticate the account using the device code flow:

```bash
dotnet run --project src/ClawMailCalCli/ClawMailCalCli.csproj -- login myaccount
```

Follow the prompt to open the displayed URL in a browser and enter the code.

Set the account as the default so commands use it automatically:

```bash
dotnet run --project src/ClawMailCalCli/ClawMailCalCli.csproj -- account set myaccount
```

---

## Running Tests

Run the unit test suite:

```bash
dotnet test tests/ClawMailCalCli.Tests/ --configuration Release --filter "Category=Unit"
```

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| `doctor` shows ✗ Azure CLI installed | Azure CLI not on `PATH` | Install Azure CLI and restart your terminal |
| `doctor` shows ✗ Azure CLI logged in | No active Azure session | Run `az login` |
| `doctor` shows ✗ Config file found | `~/.claw-mail-cal-cli/config.json` missing or invalid | Create the file with a valid `keyVaultUri` |
| `doctor` shows ✗ Key Vault reachable | Network issue, wrong URI, or missing Key Vault access policy | Verify the URI and your access policy |
| `login` hangs or fails | Incorrect `entra:tenantId` or `entra:clientId` | Double-check your Entra app registration |
| Build error: `keyVault:vaultUri` not configured | `appsettings.json` is missing the vault URI | Update `appsettings.json` or set the `keyVault__vaultUri` environment variable |

---

## Next Steps

- Explore available commands: `claw-mail-cal-cli --help`
- Read calendar events: `claw-mail-cal-cli calendar list`
- Send an email: `claw-mail-cal-cli email send user@example.com "Subject" "Body"`
