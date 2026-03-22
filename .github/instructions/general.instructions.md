---
applyTo: "**/*"
---

# General GitHub Copilot Instructions

Follow these general guidelines when working with this repository.

## GitHub Copilot General Guidelines

- Don't ask for permission before doing something, just go ahead and do the action.
- If you need to make a decision, do so based on the information available to you.
- If you are unsure about something, make a reasonable assumption and proceed.
- Always accept changes to files that are already in the repository.
- Always continue to the next logical step unless explicitly told to stop.
- Prefer multi-step completions: generate, test, refactor, optimize.
- Use modern C# syntax and avoid verbose boilerplate.

## Visual Studio Project Guidelines

- Always clean up files created by project scaffolding that are not needed.
  - This includes files like `Class1.cs`, `UnitTest1.cs`, `WeatherForecast.cs`, etc.

## Editor Guidelines

- **Indentation**: Always use tabs for indentation, with tab width set to 4 spaces for display.
- **Never use spaces for indentation** - all code files must use tabs consistently.
- The `.editorconfig` file enforces this standard across the codebase.

## File Extension Conventions

Use longer, human-readable file extensions wherever valid alternatives exist:

- `.yaml` instead of `.yml` for YAML files
- `.jpeg` instead of `.jpg` for JPEG image files
- Other extensions remain unchanged (e.g., `.cs`, `.png`, `.md`, `.bicep`)

## Iteration & Review

- Copilot output should be reviewed and modified before committing.
- If code isn't following these instructions, regenerate with more context or split the task.
- Use /// XML documentation comments to clarify intent for Copilot and future devs.
- Use Rider or Visual Studio code inspections to catch violations early.
