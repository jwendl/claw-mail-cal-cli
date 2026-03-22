---
applyTo: "**/*.bicep,**/*.bicepparam"
---

# Bicep Infrastructure as Code Instructions

Follow these guidelines for Bicep templates in this repository.

## Context

- **Project Type**: Bicep Infrastructure as Code
- **Language**: bicep
- **Framework / Libraries**: bicep
- **Architecture**: Clean Architecture, Private Networking, Managed Identity

## API Version Guidelines

**IMPORTANT: Always use the latest stable (non-preview) API versions for all Azure resources.**

- Use the latest **stable** API version (e.g., `@2024-11-30`, not `@2024-05-01-preview`)
- Avoid preview API versions in production code unless absolutely necessary
- When using preview versions, document why the preview API is required
- Regularly update API versions to latest stable releases during code reviews

Example:
```bicep
// ✅ Correct - latest stable version
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  // ...
}

// ❌ Avoid - preview version without justification
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31-preview' = {
  // ...
}
```

## Naming Conventions

- Use `camelCase` for variable, parameter, and output names.
- Use `PascalCase` for resource symbolic names.
- Use short, descriptive names for modules and resources (e.g., `appService`, `vnet`, `storageAccount`).
- Suffix private endpoints with `PrivateEndpoint` (e.g., `storagePrivateEndpoint`).
- Prefix module files with the resource type (e.g., `vnet-main.bicep`).

## Key Tasks

- Write Bicep templates using tool `#editFiles`
- If the user supplied links use the tool `#fetch` to retrieve extra context
- Break up the user's context in actionable items using the `#todos` tool.
- You follow the output from tool `#get_bicep_best_practices` to ensure Bicep best practices
- Double check the Azure Verified Modules input if the properties are correct using tool `#azure_get_azure_verified_module`
- Focus on creating Azure bicep (`*.bicep`) files. Do not include any other file types or formats.

## Pre-flight: Resolve Output Path

- Prompt once to resolve `outputBasePath` if not provided by the user.
- Default path is: `infra/bicep/{goal}`.
- Use `#runCommands` to verify or create the folder (e.g., `mkdir -p <outputBasePath>`), then proceed.

## Testing & Validation

- Use tool `#runCommands` to run the command for restoring modules: `bicep restore` (required for AVM br/public:\*).
- Use tool `#runCommands` to run the command for bicep build (--stdout is required): `bicep build {path to bicep file}.bicep --stdout --no-restore`
- Use tool `#runCommands` to run the command to format the template: `bicep format {path to bicep file}.bicep`
- Use tool `#runCommands` to run the command to lint the template: `bicep lint {path to bicep file}.bicep`
- After any command check if the command failed, diagnose why it's failed using tool `#terminalLastCommand` and retry. Treat warnings from analysers as actionable.
- After a successful `bicep build`, remove any transient ARM JSON files created during testing.

## Final Checklist

- All parameters (`param`), variables (`var`) and types are used; remove dead code.
- AVM versions or API versions match the plan.
- No secrets or environment-specific values hardcoded.
- The generated Bicep compiles cleanly and passes format checks.

## Private Networking

**IMPORTANT: All Azure PaaS resources MUST use Private Endpoints for production environments.**

- **Always use Private Endpoints** for Azure SQL Database, Storage Accounts, Redis Cache, Key Vault, and other PaaS services in production
- **Deploy Private DNS Zones** to enable name resolution for private endpoints within the VNet
- **Disable public network access** when Private Endpoints are configured
- **Use VNet Integration** for App Services and Function Apps to access private resources
- **Default to Private Endpoints** for production environments (can be disabled for dev/test)
- **Avoid public IPs** unless explicitly required for external-facing services

### Implementation Pattern

```bicep
@description('Enable Private Endpoint (recommended for production)')
param enablePrivateEndpoint bool = false

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  properties: {
    publicNetworkAccess: enablePrivateEndpoint ? 'Disabled' : 'Enabled'
  }
}

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-11-01' = if (enablePrivateEndpoint) {
  // Private endpoint configuration
}
```

## Folder Structure

- Place reusable modules in a `modules/` folder at the root of the Bicep project.
- Organize modules by resource type (e.g., `modules/network/vnet-main.bicep`, `modules/storage/storage-account.bicep`).
- Keep environment-specific files (e.g., `main.bicep`, `local.bicepparam`) at the root or in an `env/` folder.

## Role Assignments Organization

**IMPORTANT: All role assignments MUST be placed in a dedicated `modules/role-assignments.bicep` module.**

- **Never define role assignments inline** in `main.bicep` or other resource modules
- **Centralize all RBAC role assignments** in `modules/role-assignments.bicep`
- **Use existing resource references** in the role-assignments module to scope assignments to the correct resources
- **Add explicit `dependsOn`** when calling the role-assignments module to ensure resources exist before role assignments are created

### Implementation Pattern

```bicep
// In main.bicep - Call the role-assignments module
module roleAssignments './modules/role-assignments.bicep' = {
  name: 'role-assignments-deployment'
  params: {
    applicationIdentityId: applicationIdentity.id
    applicationIdentityPrincipalId: applicationIdentity.properties.principalId
    deploymentIdentityId: deploymentIdentity.id
    deploymentIdentityPrincipalId: deploymentIdentity.properties.principalId
    keyVaultName: keyVaultName
    storageAccountName: storageName
    sqlServerName: sqlServerName
  }
  dependsOn: [
    keyVault
    storage
    sqlDatabase
  ]
}

// In modules/role-assignments.bicep
resource keyVaultResource 'Microsoft.KeyVault/vaults@2025-05-01' existing = {
  name: keyVaultName
}

resource applicationKeyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVaultResource.id, applicationIdentityId, 'KeyVaultSecretsUser')
  scope: keyVaultResource
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '4633458b-17de-408a-b874-0445c86b69e6'
    ) // Key Vault Secrets User
    principalId: applicationIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}
```

### Benefits of This Approach

- **Centralized Management**: All role assignments in one place for easier auditing and maintenance
- **Separation of Concerns**: Resource creation separated from permission management
- **Reusability**: Role assignments module can be reused across environments
- **Clarity**: Easy to see all permissions granted in the infrastructure
- **Dependency Management**: Explicit dependencies prevent deployment race conditions

## Other Guidelines

- Use parameter files for environment-specific values.
- Use `existing` keyword for referencing pre-existing resources.
- Use outputs to expose resource IDs and connection strings as needed.
- Add comments to describe complex logic or resource intent.
- Use `local.bicepparam` for environment-specific values.
