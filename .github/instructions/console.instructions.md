---
applyTo: "**/Program.cs,**/*Console*.cs"
---

# Console Application Instructions

Follow these guidelines for console applications in this repository.

## Indentation

- **Always use tabs (not spaces) for indentation.** Tab width is set to 4 spaces for display.

## Spectre.Console for Output

**Use Spectre.Console for all console output** instead of `Console.WriteLine()`:

- Use `AnsiConsole.MarkupLine()` for rich formatted output
- Use `AnsiConsole.Status()` for progress indicators
- Use `AnsiConsole.Write(new Table())` for tabular data
- Use `AnsiConsole.Confirm()` for yes/no prompts

## Spectre.Console.Cli for Argument Parsing

**Use Spectre.Console.Cli for command-line argument parsing**:

- Define commands as classes inheriting from `Command<TSettings>` or `AsyncCommand<TSettings>`
- Use settings classes with `[CommandOption]` and `[CommandArgument]` attributes

## Example Usage

```csharp
using Spectre.Console;
using Spectre.Console.Cli;

// Output with markup
AnsiConsole.MarkupLine("[green]✓[/] Operation completed successfully");
AnsiConsole.MarkupLine("[red]✗[/] Operation failed: {0}", errorMessage);

// Status spinner
await AnsiConsole.Status().StartAsync("Processing...", async ctx =>
{
    await DoWorkAsync();
});
```
