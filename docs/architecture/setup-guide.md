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

Grant your own identity access to read and write secrets. Azure Key Vault supports two permission models; use the section that matches your vault's configuration:

#### Option A — Azure RBAC (recommended for new vaults)

If your vault uses Azure RBAC (the default for new vaults created in the portal after 2022), assign the **Key Vault Secrets Officer** role. First, look up your principal object ID:

```bash
az ad signed-in-user show --query id --output tsv
```

Then assign the role, scoping it to the vault:

```bash
az role assignment create \
  --role "Key Vault Secrets Officer" \
  --assignee "<your-object-id>" \
  --scope "/subscriptions/<subscription-id>/resourceGroups/<your-resource-group>/providers/Microsoft.KeyVault/vaults/<your-unique-vault-name>"
```

To verify which permission model your vault uses:

```bash
az keyvault show --name "<your-unique-vault-name>" --query "properties.enableRbacAuthorization"
```

`true` means RBAC is enabled; `false` (or absent) means access policies are in use.

#### Option B — Vault access policies

If your vault uses access policies, run:

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

`appsettings.json` only needs the Key Vault URI. The Entra app client ID and tenant ID are stored **directly in Key Vault** as secrets, so they are never needed in configuration files.

Open `src/ClawMailCalCli/appsettings.json` and set:

```json
{
  "keyVault": {
    "vaultUri": "https://<your-unique-vault-name>.vault.azure.net/"
  }
}
```

To use an environment variable instead:

```bash
export keyVault__vaultUri="https://<your-unique-vault-name>.vault.azure.net/"
```

> **Security:** Never commit real vault URIs to source control in a shared repository. For team environments, use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) or environment variables.

### Store Entra credentials in Key Vault

The Entra application client ID and tenant ID are read from Key Vault secrets at runtime. The secret names are prefixed by account type:

| Account type | Client ID secret | Tenant ID secret |
|---|---|---|
| Personal (Hotmail / Outlook.com) | `hotmail-client-id` | `hotmail-tenant-id` |
| Work / school (Exchange Online) | `exchange-client-id` | `exchange-tenant-id` |

Create each secret in your vault with `az keyvault secret set`:

```bash
VAULT_NAME="<your-unique-vault-name>"

# Personal accounts (Hotmail / Outlook.com)
az keyvault secret set --vault-name "$VAULT_NAME" \
  --name "hotmail-client-id" --value "<your-entra-app-client-id>"
az keyvault secret set --vault-name "$VAULT_NAME" \
  --name "hotmail-tenant-id" --value "common"

# Work / school accounts (Exchange Online)
az keyvault secret set --vault-name "$VAULT_NAME" \
  --name "exchange-client-id" --value "<your-entra-app-client-id>"
az keyvault secret set --vault-name "$VAULT_NAME" \
  --name "exchange-tenant-id" --value "<your-organisation-tenant-id>"
```

> **Tip:** If all your accounts use the same Entra app registration (common for personal setups), use the same client ID for both `hotmail-client-id` and `exchange-client-id`.

---

## Step 8 — Publish a Portable Executable

Instead of using `dotnet run`, publish a self-contained, portable executable so you can run the tool directly without the .NET SDK present at runtime.

**Linux x64:**

```bash
dotnet publish src/ClawMailCalCli/ClawMailCalCli.csproj \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  --output ./publish/linux-x64
```

The executable is at `./publish/linux-x64/claw-mail-cal-cli`. Make it runnable:

```bash
chmod +x ./publish/linux-x64/claw-mail-cal-cli
```

**Windows x64:**

```powershell
dotnet publish src/ClawMailCalCli/ClawMailCalCli.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output .\publish\win-x64
```

The executable is at `.\publish\win-x64\claw-mail-cal-cli.exe`.

---

> **Tip:** Add the published directory to your `PATH` (or create a shell alias) so you can call `claw-mail-cal-cli` from any directory.

---

Run the `doctor` command to verify your environment:

**Linux:**
```bash
./publish/linux-x64/claw-mail-cal-cli doctor
```

**Windows:**
```powershell
.\publish\win-x64\claw-mail-cal-cli.exe doctor
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

The examples below use the Linux executable path. Replace `./publish/linux-x64/claw-mail-cal-cli` with `.\publish\win-x64\claw-mail-cal-cli.exe` on Windows.

Add a named account (this stores the account name and email in a local SQLite database):

```bash
./publish/linux-x64/claw-mail-cal-cli account add myaccount user@example.com
```

Authenticate the account using the device code flow:

```bash
./publish/linux-x64/claw-mail-cal-cli login myaccount
```

Follow the prompt to open the displayed URL in a browser and enter the code.

Set the account as the default so commands use it automatically:

```bash
./publish/linux-x64/claw-mail-cal-cli account set myaccount
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

Once the executable is on your `PATH` (or aliased), use it directly:

- Explore available commands: `claw-mail-cal-cli --help`
- Read calendar events: `claw-mail-cal-cli calendar list`
- Send an email: `claw-mail-cal-cli email send user@example.com "Subject" "Body"`
