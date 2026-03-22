# Project: Command Line for Mail and Calendar Access

## Purpose

This is a commnand line interface to access email, people and calendar items for [OpenClaw](https://github.com/openclaw/openclaw) to use. It doesn't use an MCP server because the authentication mechanisms there are difficult. In the case of this project we will be using Entra's device code flow to enable OpenClaw the chance to prompt the user for authentication whenever it can't access the functionality.

## Requirements

### Example usage

Adds an account

```
claw-mail-cal-cli account add <name> <email>
```

Lists all accounts

```
claw-mail-cal-cli account list
```

Deletes an account

```
claw-mail-cal-cli account delete <name>
```

Sets the current account context

```
claw-mail-cal-cli account set <name>
```

Logs a user into an account

```
claw-mail-cal-cli login <account-name>
```

Fetches mail in the inbox

```
claw-mail-cal-cli email list
```

Fetches mail in a specific folder

```
claw-mail-cal-cli email list <folder-name>
```

Reads a specific mail message

```
claw-mail-cal-cli email read <subject-name | unique-message-id>
```

Sends an email

```
claw-mail-cal-cli email send <to> <subject> <content>
```

Deleting an email

```
NOT AVAILABLE
```

Lists calendar items

```
claw-mail-cal-cli calendar list
```

Reads a specific calendar item

```
claw-mail-cal-cli calendar read <title | unique-calendar-item-id>
```

Creates a calendar item

```
claw-mail-cal-cli calendar create <title> <start-date-time> <end-date-time> <content>
```

### Technology Requirements

- All secrets will be stored in Azure Key Vault (configured with a configuration file)
  - Access to that Key Vault will be through the Azure CLI Credential (which means we will require Azure CLI and for it to be logged in)
- We will cache the access token and refresh token for all accounts in Key Vault
- If any commands are run and there is a 401 unauthorized error we will go through the login flow
- The login flow will use device code flow with delegated auth accesss into Graph
  - Please reference [example-graph-service-client.md](./example-graph-service-client.md) for how to get a Graph Service Client setup properly
  - Please use appropriate scopes for the functionality needed.
- We will use the C# Graph SDK
- As this is a command line application please use Spectre.CLI

## Constraints

- Try to use .NET / Microsoft tech
- The output of this will be a published release for Windows x64 and Ubuntu x64
