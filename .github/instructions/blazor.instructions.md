---
applyTo: "**/*.razor,**/*.razor.cs"
---

# Blazor Component Instructions

Follow these conventions for all Blazor components in this repository.

## Context

- **Project Type**: Blazor App
- **Language**: C#
- **Framework / Libraries**: .NET 10
- **Architecture**: Clean Architecture

## General Guidelines

- **Indentation**: Always use tabs (not spaces) for indentation. Tab width is set to 4 spaces for display.
- Always use Fluent Blazor in this nuget package Microsoft.FluentUI.AspNetCore.Components components for UI consistency and styling.
- Use Fluent Blazor Icons for all icons from this nuget package Microsoft.FluentUI.AspNetCore.Components.Icons.
- Follow PascalCase for component names, method names, and public members.
- Use camelCase for private fields and local variables.
- Prefix interface names with "I" (e.g., IUserService).
- Utilize Blazor's built-in features for component lifecycle (e.g., OnInitializedAsync, OnParametersSetAsync).
- Use data binding effectively with @bind.
- Leverage Dependency Injection for services in Blazor.
- Structure Blazor components and services following Separation of Concerns.
- Always use the latest version C#, currently C# 13 features like record types, pattern matching, and global usings.
- When creating a project and using dotnet new use blazor as the template.

## Fluent Blazor Guidelines

- Use enumerations when they exist. For example use Orientation.Horizontal instead of "Horizontal".
- Attempt to use a Fluent Blazor component before creating a custom one.
- Use Fluent Blazor components for common UI elements like buttons, forms, modals, and navigation.
- Follow Fluent UI design principles for layout, spacing, and typography.
- Any Model objects used by the User Interface should be a plain old CLR object (POCO) and not a record or Entity Framework entity.

**Always use Microsoft.FluentUI.AspNetCore.Components** - never use raw HTML elements.

| Instead of | Use |
|------------|-----|
| `<h1>`, `<h2>` | `<FluentLabel Typo="Typo.H1">` |
| `<button>` | `<FluentButton>` |
| `<ul>`, `<ol>` | `<FluentStack Orientation="Orientation.Vertical">` |
| `<input>` | `<FluentTextField>` or `<FluentNumberField>` |
| `<select>` | `<FluentSelect>` |

## Render Modes
- Use `@rendermode InteractiveServer` for interactive pages
- Use `@rendermode @(new InteractiveServerRenderMode(prerender: false))` for OAuth callbacks

## Component Patterns
- Use `@key` directive in loops to prevent component reuse issues
- Prefer explicit component parameters over cascading values
- Use `CascadingParameter` for auth state only

## Component Styling
- **Always use separate `.razor.css` files for component styles** - never use inline `<style>` tags in `.razor` files
- CSS isolation is automatically applied by Blazor - styles in `.razor.css` files are scoped to their component
- Place the `.razor.css` file in the same directory as the corresponding `.razor` file
- Example: `GameCard.razor` → `GameCard.razor.css`
- Keeps markup clean and separates concerns between structure (Razor markup) and presentation (CSS)

## State Management
- Call `StateHasChanged()` after modifying component state
- Use `await InvokeAsync(StateHasChanged)` from background threads
- Handle null states gracefully in render logic

## Loading States
```razor
@if (isLoading)
{
    <FluentProgress Width="200px" />
}
else
{
    <!-- Content -->
}
```

## FluentButton Appearances
- `Appearance.Accent` for primary actions
- `Appearance.Outline` for secondary actions
- `Appearance.Stealth` for subtle actions

## FluentBadge Appearances
Only use: `Appearance.Accent`, `Appearance.Lightweight`, or `Appearance.Neutral`

## FluentIcon Usage
```razor
@using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons

<FluentIcon Value="@(Icons.Regular.Size20.Home)" />
```

## Dialogs
```razor
<FluentDialog Modal="true" TrapFocus="true">
    <!-- Content -->
</FluentDialog>
```

## Required Imports (_Imports.razor)

```razor
@using Microsoft.FluentUI.AspNetCore.Components
@using Microsoft.FluentUI.AspNetCore.Components.Extensions
@using Emoji = Microsoft.FluentUI.AspNetCore.Components.Emoji
@using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons
```

## Theme Picker

- Implement a theme picker to allow users to switch their theme choice as per the Fluent Blazor UI documentation.
- Store the user's theme preference in local storage or a cookie to persist across sessions.
- Allow for choice of Light mode, Dark mode or System Default.

## Authentication

- Use Entra ID (Azure AD) for authentication and authorization.
- Have a login page that uses Entra ID for user authentication.

## Error Handling and Validation

- Implement proper error handling for Blazor pages and API calls.
- Use logging for error tracking in the backend and consider capturing UI-level errors in Blazor with tools like ErrorBoundary.
- Implement validation using FluentValidation or DataAnnotations in forms.

## Performance Optimization

- Utilize Blazor server-side or WebAssembly optimally based on the project requirements.
- Use asynchronous methods (async/await) for API calls or UI actions that could block the main thread.
- Optimize Razor components by reducing unnecessary renders and using StateHasChanged() efficiently.
- Minimize the component render tree by avoiding re-renders unless necessary, using ShouldRender() where appropriate.
- Use EventCallbacks for handling user interactions efficiently, passing only minimal data when triggering events.

## Caching Strategies

- Implement in-memory caching for frequently used data, especially for Blazor Server apps. Use IMemoryCache for lightweight caching solutions.
- For Blazor WebAssembly, utilize localStorage or sessionStorage to cache application state between user sessions.
- Consider Distributed Cache strategies (like Redis or SQL Server Cache) for larger applications that need shared state across multiple users or clients.
- Cache API calls by storing responses to avoid redundant calls when data is unlikely to change, thus improving the user experience.

## State Management Libraries

- Use Blazor's built-in Cascading Parameters and EventCallbacks for basic state sharing across components.
- Implement advanced state management solutions using libraries like Fluxor or BlazorState when the application grows in complexity.
- For client-side state persistence in Blazor WebAssembly, consider using Blazored.LocalStorage or Blazored.SessionStorage to maintain state between page reloads.
- For server-side Blazor, use Scoped Services and the StateContainer pattern to manage state within user sessions while minimizing re-renders.

## API Design and Integration

- Use HttpClient or other appropriate services to communicate with external APIs or your own backend.
- Implement error handling for API calls using try-catch and provide proper user feedback in the UI.

## Testing and Debugging

- All unit testing and integration testing should be done in Visual Studio Enterprise.
- Test Blazor components and services using xUnit.
- Use Moq for mocking dependencies during tests.
- Debug Blazor UI issues using browser developer tools and Visual Studio's debugging tools for backend and server-side issues.
- For performance profiling and optimization, rely on Visual Studio's diagnostics tools.

## Security and Authentication

- Implement Authentication and Authorization in the Blazor app where necessary using ASP.NET Identity or JWT tokens for API authentication.
- Use HTTPS for all web communication and ensure proper CORS policies are implemented.

## Secrets Management

- **Never hardcode secrets** (API keys, connection strings, client secrets) in `appsettings.json` or source code.
- Use **User Secrets** for local development in Visual Studio:
  1. Add `<UserSecretsId>` to the `.csproj` file
  2. Right-click the project → "Manage User Secrets"
  3. Store sensitive values in `secrets.json` (stored in `%APPDATA%\Microsoft\UserSecrets\`)
- Use **Azure Key Vault** or environment variables for staging/production environments.
- Keep placeholder values in `appsettings.json` to document expected configuration structure:
  ```json
  {
    "AzureAdB2C": {
      "TenantId": "your-tenant-id",
      "ClientId": "your-client-id"
    }
  }
  ```
- Add `appsettings.*.json` (except `appsettings.json`) to `.gitignore` if they contain environment-specific secrets.
- Document required secrets in a `README.md` or `appsettings.Development.json.template` file.

## API Documentation

- Use Swagger/OpenAPI for API documentation for your backend API services.
- Ensure XML documentation for models and API methods for enhancing Swagger documentation.

## Examples

### App.razor

Please ensure to include the loading theme script in your `App.razor` file to support theme switching:

```razor
<script src="_content/Microsoft.FluentUI.AspNetCore.Components/js/loading-theme.js" type="text/javascript"></script>
<loading-theme storage-name="theme"></loading-theme>
```

### Login.razor

Include a login page that uses Entra for authentication, but allows for folks to choose from different Entra tenants.

### MainLayout.razor

Use the example below as a guideline for your `MainLayout.razor` file:

```razor
@rendermode InteractiveServer

@inherits LayoutComponentBase

<FluentDesignTheme StorageName="theme" />

<FluentLayout>
	<FluentHeader>
		<FluentSpacer />
		<ProfileMenu />
	</FluentHeader>
	<FluentStack Class="main" Orientation="Orientation.Horizontal" Width="100%">
		<NavMenu />
		<FluentBodyContent Class="body-content">
			<div class="content">
				@Body
			</div>
		</FluentBodyContent>
	</FluentStack>
	<FluentFooter>
		<a href="https://www.fluentui-blazor.net" target="_blank">Documentation and demos</a>
		<FluentSpacer />
		<a href="https://learn.microsoft.com/en-us/aspnet/core/blazor" target="_blank">About Blazor</a>
	</FluentFooter>
</FluentLayout>

<FluentToastProvider />
<FluentDialogProvider />
<FluentTooltipProvider />
<FluentMessageBarProvider />
<FluentMenuProvider />

<div id="blazor-error-ui">
	An unhandled error has occurred.
	<a href="" class="reload">Reload</a>
	<a class="dismiss">🗙</a>
</div>
```

### Other Razor Files

- Please use <FluentLayout>, <FluentHeader>, <FluentFooter>, <FluentBodyContent>, <FluentStack> and other Fluent Blazor components wherever possible.
- Please use loading indicators from Fluent Blazor when loading data asynchronously.
  - Example:

```razor
<FluentLayout>
	<FluentHeader>
		...
	</FluentHeader>
	<FluentStack Orientation="Orientation.Vertical">
    @if (isLoading)
		{
			<FluentProgress Width="200px" />
		}
  </FluentStack>
</FluentLayout>

@code {
	private bool isLoading = false;

  private async Task SendMessage()
	{
    isLoading = true;
		await InvokeAsync(StateHasChanged);
  }
}
```

### HTML Guidelines

- Use `<FluentLabel Typo="Typo.H2">` for headings instead of `<h1>`, `<h2>`, etc.
- Use `<FluentButton>` for buttons instead of `<button>`.
- Use `<FluentStack Orientation="Orientation.Vertical">` for lists instead of `<ul>` or `<ol>`.
