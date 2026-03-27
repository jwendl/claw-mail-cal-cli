# OpenClaw Skills — `claw-mail-cal-cli`

This file is the machine-readable skills reference for the **OpenClaw** AI agent. It describes every available command, its arguments and options, expected JSON output, exit codes, prerequisites, and usage examples. Always prefer `--json` output when invoking commands programmatically.

> **Companion document**: This file is a companion to [`README.md`](../README.md) and [`docs/architecture/architecture.md`](architecture/architecture.md). It does not replace them.

---

## Table of Contents

- [Setup Workflow](#setup-workflow)
- [Global Flags](#global-flags)
- [Authentication Behavior](#authentication-behavior)
- [Error Patterns](#error-patterns)
- [Exit Codes](#exit-codes)
- [Commands](#commands)
  - [account add](#account-add)
  - [account list](#account-list)
  - [account set](#account-set)
  - [account delete](#account-delete)
  - [login](#login)
  - [doctor](#doctor)
  - [email list](#email-list)
  - [email read](#email-read)
  - [email send](#email-send)
  - [calendar list](#calendar-list)
  - [calendar read](#calendar-read)
  - [calendar create](#calendar-create)

---

## Setup Workflow

Before any mail or calendar command can be executed, the agent must complete these steps **in order**:

1. **Check prerequisites** — run `doctor` to verify `az login` is active and the configuration is valid.
2. **Add an account** — run `account add <name> <email> [--type personal|work]` once per user.
3. **Authenticate** — run `login <account-name>` and complete the device code flow. This only needs to be done once per account; subsequent commands re-authenticate silently.
4. **Set the default account** — run `account set <name>` so commands that use the default account work without `--account`.
5. **Use commands** — `email list`, `email read`, `email send`, `calendar list`, `calendar read`, `calendar create`.

```
doctor → account add → login → account set → (data commands)
```

---

## Global Flags

| Flag | Type | Description |
|------|------|-------------|
| `--json` | boolean | Write raw JSON to stdout. Use this flag for all agent invocations. Supported on `email list`, `email read`, `calendar list`, and `calendar read`. |
| `--verbosity <level>` | string | Log verbosity: `quiet`, `normal` (default), `verbose`. |
| `--help` | boolean | Print help for any command or sub-command. |
| `--version` | boolean | Print the CLI version. |

---

## Authentication Behavior

- All commands that access Microsoft Graph require a valid authenticated account.
- Authentication uses the **Entra ID device code flow** — the agent must present the displayed URL and code to the user for first-time authentication.
- After successful login, an `AuthenticationRecord` is stored in Azure Key Vault as the secret `auth-record-{account-name}`.
- On subsequent commands the CLI **silently re-authenticates** using the cached record; no user interaction is required.
- If a `401 Unauthorized` response is received, the CLI automatically retries the login flow up to 3 times.
- Access to Azure Key Vault itself uses the **Azure CLI Credential** (`az login` must be active).

### When to call `login` again

- First time a new account is added.
- If the cached `AuthenticationRecord` in Key Vault has been deleted or corrupted.
- If a `401` error persists after the automatic retry attempts.

---

## Error Patterns

| Pattern | Meaning | Recommended action |
|---------|---------|-------------------|
| Exit code `1` with `Error:` message on stderr | Command-level failure (invalid args, not found, send failed). | Read the error message. Correct the arguments or re-run the setup workflow. |
| `Account '<name>' already exists.` | Duplicate account name. | Use `account list` to see existing accounts; skip `account add` if the account already exists. |
| `No accounts found.` | No accounts have been added yet. | Run `account add` then `login`. |
| `No message found matching: <query>` | Subject search returned zero results. | Try a shorter or different subject substring, or use the exact Graph message ID. |
| `No event found matching '<query>'` | Calendar title search returned zero results. | Try a shorter or different title substring. |
| `No account specified.` | `calendar read` was called without `--account` and no default is configured. | Pass `--account <name>` or run `account set <name>`. |
| `One or more checks failed.` | `doctor` detected a missing prerequisite. | Follow the fix hints printed by `doctor`. |
| Authentication / Key Vault errors | `az login` not active, or Key Vault URI not configured. | Run `doctor` to diagnose; ensure `az login` is active and `keyVaultUri` is set. |

---

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success — the command completed without error. |
| `1` | Failure — the command encountered an error. See stderr for details. |

---

## Commands

---

### account add

**WHEN to use**: Use this command once for each Microsoft account you want to manage. Must be called before `login`.

**Syntax**

```
claw-mail-cal-cli account add <name> <email> [--type personal|work]
```

**Arguments and options**

| Parameter | Required | Type | Description |
|-----------|----------|------|-------------|
| `<name>` | Yes | string | A short, unique identifier for the account (e.g. `myaccount`). Used in all subsequent commands. |
| `<email>` | Yes | string | The email address associated with the account. |
| `--type` / `-t` | No | `personal` \| `work` | Account type. `personal` = Microsoft personal account (Hotmail, Outlook.com). `work` = work or school account (Exchange Online). Defaults to `personal`. |

**Prerequisites**: None. Azure Key Vault configuration and `az login` are not required for this command.

**Exit codes**

| Code | Meaning |
|------|---------|
| `0` | Account was added successfully. |
| `1` | An account with that name already exists. |

**Output (human-readable)**

```
✓ Account 'myaccount' added successfully.
```

**Example**

```bash
claw-mail-cal-cli account add myaccount user@outlook.com --type personal
claw-mail-cal-cli account add workaccount user@contoso.com --type work
```

---

### account list

**WHEN to use**: Use to check which accounts are configured before running data commands. Use to verify that an account exists before calling `login` or `account set`.

**Syntax**

```
claw-mail-cal-cli account list
```

**Arguments and options**: None.

**Prerequisites**: None.

**Exit codes**

| Code | Meaning |
|------|---------|
| `0` | Success (even when no accounts exist). |

**Output (human-readable)**

```
 Name          Email                     Type
 ------------- ------------------------- ---------
 myaccount     user@outlook.com          Personal
 workaccount   user@contoso.com          Work
```

**Notes**: `account list` does not support `--json`. Parse the table output if JSON is required (or track accounts in agent state after calling `account add`).

---

### account set

**WHEN to use**: Use after adding and authenticating an account to make it the default for subsequent commands that do not require an explicit account argument.

**Syntax**

```
claw-mail-cal-cli account set <name>
```

**Arguments and options**

| Parameter | Required | Type | Description |
|-----------|----------|------|-------------|
| `<name>` | Yes | string | The name of an existing account to set as the default. |

**Prerequisites**: The named account must already exist (added via `account add`).

**Exit codes**

| Code | Meaning |
|------|---------|
| `0` | Default account updated successfully. |
| `1` | The named account does not exist. |

**Output (human-readable)**

```
✓ Default account set to 'myaccount'.
```

**Example**

```bash
claw-mail-cal-cli account set myaccount
```

---

### account delete

**WHEN to use**: Use to remove an account that is no longer needed.

**Syntax**

```
claw-mail-cal-cli account delete <name>
```

**Arguments and options**

| Parameter | Required | Type | Description |
|-----------|----------|------|-------------|
| `<name>` | Yes | string | The name of the account to delete. |

**Prerequisites**: The named account must already exist.

**Exit codes**

| Code | Meaning |
|------|---------|
| `0` | Account deleted successfully. |
| `1` | The named account was not found. |

**Output (human-readable)**

```
✓ Account 'myaccount' deleted.
```

**Example**

```bash
claw-mail-cal-cli account delete myaccount
```

---

### login

**WHEN to use**: Use once per account, after `account add`, to authenticate and cache credentials in Azure Key Vault. Re-run only if the cached credentials are missing or if a `401` error persists.

**Syntax**

```
claw-mail-cal-cli login <account-name>
```

**Arguments and options**

| Parameter | Required | Type | Description |
|-----------|----------|------|-------------|
| `<account-name>` | Yes | string | The name of an existing account to authenticate. |

**Prerequisites**

- `account add <name>` must have been run for this account.
- `az login` must be active (used to access Azure Key Vault).
- The Azure Key Vault URI must be configured (via `keyVault:vaultUri` env var, user secrets, or `appsettings.json`).
- The Entra ID app registration client ID must be configured (via `entra:clientId`).

**Exit codes**

| Code | Meaning |
|------|---------|
| `0` | Authentication succeeded and credentials are stored in Key Vault. |
| `1` | Authentication failed (device code flow timed out, cancelled, or Key Vault write failed). |

**Authentication flow**

1. The CLI requests a device code from Entra ID.
2. A verification URL and code are printed to stdout.
3. The user opens the URL and enters the code in a browser.
4. The CLI polls until authentication completes (up to 15 minutes).
5. The `AuthenticationRecord` is stored in Key Vault as `auth-record-{account-name}`.

**Output (human-readable)**

```
To sign in, use a web browser to open the page https://microsoft.com/devicelogin and enter the code XXXXXXXXX to authenticate.
✓ Authenticated successfully.
```

**Example**

```bash
claw-mail-cal-cli login myaccount
```

---

### doctor

**WHEN to use**: Use at the start of any session to verify that the environment is correctly configured. Run this first if any command fails unexpectedly.

**Syntax**

```
claw-mail-cal-cli doctor
```

**Arguments and options**: None.

**Prerequisites**: None (this command diagnoses missing prerequisites).

**Exit codes**

| Code | Meaning |
|------|---------|
| `0` | All checks passed. |
| `1` | One or more checks failed. See fix hints in output. |

**Checks performed**

- Azure CLI is installed and `az login` is active.
- Azure Key Vault URI is configured.
- Entra ID client ID is configured.

**Output (human-readable)**

```
Checking environment...

✓ Azure CLI (az login is active)
✓ Key Vault URI (https://my-keyvault.vault.azure.net/)
✓ Entra Client ID (configured)

All checks passed.
```

On failure:

```
✗ Azure CLI: az is not installed or az login has not been run.
  Fix: Install the Azure CLI and run 'az login'.
```

**Example**

```bash
claw-mail-cal-cli doctor
```

---

### email list

**WHEN to use**: Use to retrieve a list of recent emails from the inbox or a named folder. Prefer `--json` for agent processing.

**Syntax**

```
claw-mail-cal-cli email list [folder-name] [--json]
```

**Arguments and options**

| Parameter | Required | Type | Description |
|-----------|----------|------|-------------|
| `[folder-name]` | No | string | The name of the mail folder to list. Defaults to the inbox when omitted. Examples: `inbox`, `sentitems`, `drafts`, `deleteditems`. |
| `--json` | No | boolean | Output raw JSON to stdout instead of a formatted table. |

**Prerequisites**

- An account must be added and authenticated (`account add` + `login`).
- A default account must be set (`account set`).

**Exit codes**

| Code | Meaning |
|------|---------|
| `0` | Success. Returns up to 20 messages (or an empty array). |
| `1` | Authentication failure or Graph API error. |

**JSON output schema** (array of `EmailSummary`)

```json
[
  {
    "from": "sender@example.com",
    "subject": "Meeting Notes",
    "receivedDateTime": "2026-03-26T14:30:00+00:00",
    "isRead": false
  }
]
```

| Field | Type | Description |
|-------|------|-------------|
| `from` | string | Sender email address. |
| `subject` | string | Email subject line. |
| `receivedDateTime` | ISO 8601 string | Date and time the message was received (UTC offset included). |
| `isRead` | boolean | Whether the message has been read. |

**Examples**

```bash
# List inbox messages as JSON (preferred for agent use)
claw-mail-cal-cli email list --json

# List messages from the Sent Items folder
claw-mail-cal-cli email list sentitems --json
```

Expected JSON output:

```json
[
  {
    "from": "alice@example.com",
    "subject": "Project Update",
    "receivedDateTime": "2026-03-26T09:00:00+00:00",
    "isRead": true
  },
  {
    "from": "bob@example.com",
    "subject": "Lunch tomorrow?",
    "receivedDateTime": "2026-03-26T08:15:00+00:00",
    "isRead": false
  }
]
```

---

### email read

**WHEN to use**: Use to retrieve the full content of a specific email by subject substring or exact Graph message ID. Prefer `--json` for agent processing.

**Syntax**

```
claw-mail-cal-cli email read <account-name> <subject-or-id> [--json]
```

**Arguments and options**

| Parameter | Required | Type | Description |
|-----------|----------|------|-------------|
| `<account-name>` | Yes | string | The name of the account to query. |
| `<subject-or-id>` | Yes | string | A partial subject substring (case-insensitive) or the exact Graph message ID. |
| `--json` | No | boolean | Output raw JSON to stdout instead of formatted text. |

**Prerequisites**

- The named account must be added and authenticated (`account add` + `login`).

**Exit codes**

| Code | Meaning |
|------|---------|
| `0` | Message found and returned. |
| `1` | No message found matching the query, or Graph API error. |

**JSON output schema** (`EmailMessage`)

```json
{
  "id": "AAMkAGI...",
  "subject": "Project Update",
  "from": "alice@example.com",
  "to": "user@outlook.com",
  "receivedDateTime": "2026-03-26T09:00:00+00:00",
  "body": "Hi,\n\nHere is the latest project update...\n"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | The unique Graph message ID. |
| `subject` | string | Email subject line. |
| `from` | string | Sender email address. |
| `to` | string | Primary recipient email address. |
| `receivedDateTime` | ISO 8601 string \| null | Date and time the message was received. May be `null` for draft messages. |
| `body` | string | Plain-text body content. |

**Examples**

```bash
# Read by partial subject match
claw-mail-cal-cli email read myaccount "Project Update" --json

# Read by exact Graph message ID
claw-mail-cal-cli email read myaccount "AAMkAGI..." --json
```

---

### email send

**WHEN to use**: Use to send an email from the default authenticated account to a recipient.

**Syntax**

```
claw-mail-cal-cli email send <to> <subject> <content>
```

**Arguments and options**

| Parameter | Required | Type | Description |
|-----------|----------|------|-------------|
| `<to>` | Yes | string | The recipient email address. |
| `<subject>` | Yes | string | The email subject line. |
| `<content>` | Yes | string | The plain-text body content of the email. |

**Prerequisites**

- A default account must be added and authenticated (`account add` + `login` + `account set`).

**Exit codes**

| Code | Meaning |
|------|---------|
| `0` | Email sent successfully. |
| `1` | Send failed (authentication error or Graph API error). |

**Output (human-readable)**

```
✓ Email sent to recipient@example.com
```

**Notes**: `email send` does not support `--json`. On success it returns exit code `0`; on failure it returns exit code `1` (check stderr for details).

**Example**

```bash
claw-mail-cal-cli email send recipient@example.com "Hello" "This is the body."
```

---

### calendar list

**WHEN to use**: Use to retrieve a list of upcoming calendar events. Prefer `--json` for agent processing.

**Syntax**

```
claw-mail-cal-cli calendar list [--json]
```

**Arguments and options**

| Parameter | Required | Type | Description |
|-----------|----------|------|-------------|
| `--json` | No | boolean | Output raw JSON to stdout instead of a formatted table. |

**Prerequisites**

- A default account must be added, authenticated, and set (`account add` + `login` + `account set`).

**Exit codes**

| Code | Meaning |
|------|---------|
| `0` | Success. Returns up to 20 upcoming events (or an empty array if none exist in the next 30 days). |
| `1` | Authentication failure or Graph API error. |

**JSON output schema** (array of `CalendarEventSummary`)

```json
[
  {
    "title": "Team Standup",
    "start": "2026-03-27T09:00:00+00:00",
    "end": "2026-03-27T09:30:00+00:00",
    "isAllDay": false,
    "location": "Conference Room A"
  }
]
```

| Field | Type | Description |
|-------|------|-------------|
| `title` | string | Event subject/title. |
| `start` | ISO 8601 string | Event start date and time with UTC offset. |
| `end` | ISO 8601 string | Event end date and time with UTC offset. |
| `isAllDay` | boolean | Whether the event spans the entire day. |
| `location` | string \| null | Event location display name. `null` if not set. |

**Examples**

```bash
# List upcoming events as JSON (preferred for agent use)
claw-mail-cal-cli calendar list --json
```

Expected JSON output:

```json
[
  {
    "title": "Team Standup",
    "start": "2026-03-27T09:00:00+00:00",
    "end": "2026-03-27T09:30:00+00:00",
    "isAllDay": false,
    "location": "Conference Room A"
  },
  {
    "title": "All Hands",
    "start": "2026-03-28T14:00:00+00:00",
    "end": "2026-03-28T15:00:00+00:00",
    "isAllDay": false,
    "location": null
  }
]
```

---

### calendar read

**WHEN to use**: Use to retrieve the full details of a specific calendar event by title substring or exact Graph event ID. Prefer `--json` for agent processing.

**Syntax**

```
claw-mail-cal-cli calendar read <query> [--account <name>] [--json]
```

**Arguments and options**

| Parameter | Required | Type | Description |
|-----------|----------|------|-------------|
| `<query>` | Yes | string | A partial event title (case-insensitive) or the exact Graph event ID. |
| `--account` / `-a` | No | string | The account name to use. If omitted, the default account from configuration (`~/.claw-mail-cal-cli/config.json`) is used. |
| `--json` | No | boolean | Output raw JSON to stdout instead of formatted text. |

**Prerequisites**

- The account must be added and authenticated (`account add` + `login`).
- Either `--account` is provided or a default account is set.

**Exit codes**

| Code | Meaning |
|------|---------|
| `0` | Event found and returned. |
| `1` | No event found matching the query, no account configured, or Graph API error. |

**JSON output schema** (`CalendarEvent`)

```json
{
  "id": "AAMkAGI...",
  "subject": "Team Standup",
  "start": "2026-03-27T09:00:00",
  "end": "2026-03-27T09:30:00",
  "location": "Conference Room A",
  "organizer": "Alice Smith",
  "attendees": ["alice@example.com", "bob@example.com"],
  "body": "Daily team standup to discuss blockers and progress."
}
```

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | The unique Graph event ID. |
| `subject` | string | Event title/subject. |
| `start` | ISO 8601 string \| null | Start date and time as returned by the Microsoft Graph API. Format may vary (e.g. `2026-03-27T09:00:00` or `2026-03-27T09:00:00.0000000`). May be `null` for all-day events or incomplete Graph data. |
| `end` | ISO 8601 string \| null | End date and time as returned by the Microsoft Graph API. May be `null` for all-day events or incomplete Graph data. |
| `location` | string \| null | Event location display name. `null` if not set. |
| `organizer` | string \| null | Display name of the event organizer. `null` if not available. |
| `attendees` | string[] | Array of attendee display names. Empty array if no attendees. |
| `body` | string \| null | Plain-text body content. `null` if the event has no body. |

**Examples**

```bash
# Read by partial title match
claw-mail-cal-cli calendar read "Standup" --account myaccount --json

# Read using default account
claw-mail-cal-cli calendar read "Team Standup" --json
```

---

### calendar create

**WHEN to use**: Use to create a new calendar event on the default account's primary calendar.

**Syntax**

```
claw-mail-cal-cli calendar create <title> <start> <end> <content>
```

**Arguments and options**

| Parameter | Required | Type | Description |
|-----------|----------|------|-------------|
| `<title>` | Yes | string | The event title/subject. |
| `<start>` | Yes | ISO 8601 string | Event start date and time. Accepted formats: `2026-03-25T09:00:00`, `2026-03-25T09:00:00Z`, `2026-03-25T09:00:00+00:00`. |
| `<end>` | Yes | ISO 8601 string | Event end date and time. Must be after `<start>`. Same format as `<start>`. |
| `<content>` | Yes | string | Plain-text body content for the event. |

**Prerequisites**

- A default account must be added, authenticated, and set (`account add` + `login` + `account set`).

**Validation**

- `<start>` and `<end>` must be valid ISO 8601 datetime strings.
- `<end>` must be strictly after `<start>`.

**Exit codes**

| Code | Meaning |
|------|---------|
| `0` | Event created successfully. The Graph event ID is printed to stdout. |
| `1` | Invalid date format, end is before start, authentication failure, or Graph API error. |

**Output (human-readable)**

```
✓ Calendar event 'Team Standup' created (ID: AAMkAGI...)
```

**Notes**: `calendar create` does not support `--json`. On success the Graph event ID is embedded in the human-readable output message. On failure the exit code is `1` with an error description on stdout.

**Examples**

```bash
# Create a 30-minute meeting
claw-mail-cal-cli calendar create "Team Standup" "2026-03-27T09:00:00" "2026-03-27T09:30:00" "Daily standup"

# Create an all-morning event
claw-mail-cal-cli calendar create "Workshop" "2026-03-28T08:00:00" "2026-03-28T12:00:00" "Architecture workshop"
```
