# GitHub Copilot Instructions

This directory contains path-specific coding standards and best practices for the PTT Game Library project. These files replace the deprecated `docs/CODING_STANDARDS.md` and are automatically applied by GitHub Copilot based on file patterns.

## Instruction Files

| File | Applies To | Description |
|------|------------|-------------|
| `general.instructions.md` | `**/*` | General GitHub Copilot guidelines, editor settings, and iteration practices |
| `csharp.instructions.md` | `**/*.cs` | C# coding standards including naming, structure, DI, async/await, and testing |
| `blazor.instructions.md` | `**/*.razor`, `**/*.razor.cs` | Blazor component guidelines, Fluent UI usage, state management, and security |
| `azure-functions.instructions.md` | `**/PttGameLibrary.Functions/**/*.cs` | Azure Functions specific guidelines including logging guards and timer triggers |
| `tests.instructions.md` | `**/tests/**/*.cs` | Testing conventions using xUnit, Moq, and FluentAssertions |
| `nuget.instructions.md` | `**/*.csproj`, `**/*.slnx`, `**/*.sln` | NuGet package version guidelines |
| `console.instructions.md` | `**/Program.cs`, `**/*Console*.cs` | Console application guidelines using Spectre.Console |
| `entity-framework.instructions.md` | `**/Data/**/*.cs`, `**/Repositories/**/*.cs`, `**/*DbContext.cs` | Entity Framework Core best practices |
| `http-client.instructions.md` | `**/*Client.cs`, `**/*Service.cs`, `**/Services/**/*.cs` | HTTP client best practices using Refit and Polly |
| `bicep.instructions.md` | `**/*.bicep`, `**/*.bicepparam` | Bicep Infrastructure as Code guidelines |
| `diagrams.instructions.md` | `**/*.eraserdiagram` | Architecture diagram guidelines using Eraser.io |

## Migration from CODING_STANDARDS.md

The content from `docs/CODING_STANDARDS.md` has been distributed across these instruction files for better organization and automatic application based on file type. The mapping is as follows:

### Content Distribution

- **GitHub Copilot General Guidelines** → `general.instructions.md`
- **Visual Studio Project Guidelines** → `general.instructions.md`
- **Editor Guidelines** → `general.instructions.md`
- **C# Style Guidelines** → `csharp.instructions.md`
- **NuGet Packages** → `nuget.instructions.md`
- **Console Applications** → `console.instructions.md`
- **Naming Conventions** → `csharp.instructions.md`
- **Dependency Injection** → `csharp.instructions.md`
- **Unit Testability** → `csharp.instructions.md`
- **Unit Test Guidelines** → `tests.instructions.md`
- **Code Structure** → `csharp.instructions.md`
- **Iteration & Review** → `general.instructions.md`
- **Azure Functions Guidelines** → `azure-functions.instructions.md`
- **Entity Framework Core Guidelines** → `entity-framework.instructions.md`
- **Blazor Guidelines** → `blazor.instructions.md`
- **HTTP Client Best Practices** → `http-client.instructions.md`
- **Bicep Style Guidelines** → `bicep.instructions.md`
- **Architecture Diagrams** → `diagrams.instructions.md`

## How GitHub Copilot Uses These Files

GitHub Copilot automatically applies these instructions when working with files that match the `applyTo` patterns specified in each file's YAML frontmatter. Each instruction file starts with a frontmatter block like:

```markdown
---
applyTo: "**/*.cs"
---
```

The `applyTo` field uses glob patterns to determine which files the instructions should apply to. For example:

- When editing a `.cs` file, `csharp.instructions.md` is applied
- When editing a `.razor` file, `blazor.instructions.md` is applied
- When editing files in `tests/`, `tests.instructions.md` is applied
- Azure Functions files get both `csharp.instructions.md` and `azure-functions.instructions.md`

All instruction files are also referenced in `copilot.yaml` to ensure they are loaded by GitHub Copilot in Visual Studio and the GitHub Copilot CLI.

This ensures that the right coding standards are applied automatically without needing to reference a large monolithic document.

## References

All documentation has been updated to reference `.github/instructions/` instead of `docs/CODING_STANDARDS.md`:

- `README.md`
- `CONTRIBUTING.md`
- `docs/plan/workflow.md`
- `docs/QUICKSTART.md`
- `.github/copilot-instructions.md`
- Issue templates
- Pull request templates
- Various docs/ files

The original `docs/CODING_STANDARDS.md` has been deprecated and marked for future removal.
