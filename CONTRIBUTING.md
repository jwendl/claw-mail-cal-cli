# Contributing to claw-mail-cal-cli

Thank you for your interest in contributing to claw-mail-cal-cli! This document provides guidelines for both human developers and AI agents working on this project.

## Table of Contents

- [For AI Agents](#for-ai-agents)
- [For Human Developers](#for-human-developers)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Testing Requirements](#testing-requirements)
- [Changelog](#changelog)
- [Pull Request Process](#pull-request-process)
- [Issue Guidelines](#issue-guidelines)

---

## For AI Agents

This repository is optimized for agentic coding practices. Follow these guidelines for effective collaboration.

### 📋 Before You Start

**Required reading** (in order):
1. [`.github/instructions/`](.github/instructions/) - **CRITICAL**: All code must follow these standards
2. [`docs/architecture/requirements.md`](docs/architecture/requirements.md) - High-level project goals and constraints
3. [`docs/architecture/architecture.md`](docs/architecture/architecture.md) - System design and patterns
4. [`.github/copilot-instructions.md`](.github/copilot-instructions.md) - GitHub Copilot configuration

**Optional reading** (for context):
- [`docs/summary-reports/user-story-status-index.md`](docs/summary-reports/user-story-status-index.md) - Consolidated user story status index
- [`docs/summary-reports/`](docs/summary-reports/) - Implementation summaries and fix reports
- [`docs/architecture/agents.md`](docs/architecture/agents.md) - Specialized agent persona definitions
- [`.github/instructions/`](.github/instructions/) - File-specific coding rules

### 🔄 Agent Workflow

1. **Pick a Task**
   - Check [GitHub Issues](https://github.com/jwendl/claw-mail-cal-cli/issues) for open tasks
   - Review the [User Story Status Index](docs/summary-reports/user-story-status-index.md) for project progress
   - Or browse completed implementations in [`docs/summary-reports/`](docs/summary-reports/) for context

2. **Create a Feature Branch**
   ```bash
   # Always branch from main
   git checkout main
   git pull origin main
   git checkout -b feature/user-story-XX-description
   # or
   git checkout -b fix/issue-123-description
   ```

   **Branch naming conventions:**
   - `feature/` - New functionality (e.g., `feature/user-story-12-steam-analytics`)
   - `fix/` - Bug fixes (e.g., `fix/issue-45-swipe-gesture`)
   - `refactor/` - Code improvements (e.g., `refactor/simplify-recommendation-service`)
   - `docs/` - Documentation updates (e.g., `docs/update-setup-instructions`)
   - `test/` - Test additions (e.g., `test/add-wishlist-repository-tests`)

3. **Understand the Context**
   - Read the user story or issue description thoroughly
   - Review related code files mentioned in the task
   - Check for existing tests that need updating
   - Understand dependencies on other components

4. **Make Minimal Changes**
   - Focus on **one** user story or issue per branch
   - Change only what's necessary to implement the feature
   - Do not refactor unrelated code
   - Do not fix unrelated bugs or issues

5. **Follow Coding Standards**
   - **ALWAYS** refer to [`.github/instructions/`](.github/instructions/)
   - Use C# 13 features (primary constructors, simplified collections, record types)
   - Use Fluent UI components (never raw HTML elements)
   - One class per file
   - Constructor injection only
   - Guard Azure Functions logs with `logger.IsEnabled()`
   - Format code: `dotnet format src/`

6. **Write Tests**
   - **Required** for all business logic changes
   - Use xUnit + Moq + FluentAssertions
   - Follow Arrange-Act-Assert pattern
   - Test naming: `MethodName_Scenario_ExpectedResult`
   - See [`.github/instructions/tests.instructions.md`](.github/instructions/tests.instructions.md)
   - Target 80%+ coverage on services

7. **Validate Your Changes**
   ```bash
   # Build
   dotnet build src/ClawMailCalCli/ClawMailCalCli.csproj --configuration Release
   
   # Run tests
   dotnet test tests/ --configuration Release --verbosity normal
   
   # Format code
   dotnet format src/
   
   # Verify no uncommitted changes after format
   git status
   ```

8. **Commit with Descriptive Messages**
   ```bash
   git add .
   git commit -m "feat: implement Steam play hours analytics (User Story #12)"
   # or
   git commit -m "fix: resolve swipe gesture not triggering on mobile (Issue #45)"
   ```

   **Commit message format:**
   - `feat:` - New feature
   - `fix:` - Bug fix
   - `refactor:` - Code restructuring (no behavior change)
   - `test:` - Adding/updating tests
   - `docs:` - Documentation changes
   - `chore:` - Build, config, or tooling changes

9. **Push and Create Pull Request**
   ```bash
   git push origin feature/user-story-XX-description
   ```

   Then create a PR on GitHub:
   - Use the PR template (auto-filled)
   - **Link to the issue using closing keywords** (see below)
   - Fill out the checklist completely
   - Request review from appropriate reviewers

### 🔗 Linking Pull Requests to Issues

**GitHub automatically closes issues when PRs are merged** if you use the right keywords in your PR description.

**Supported closing keywords** (case-insensitive):
- `Closes #123` - Generic closing (works for any issue type)
- `Fixes #456` - Best for bug fixes
- `Resolves #789` - Best for tasks and user stories

**Examples:**

```markdown
## Related Issues

Fixes #123
Closes #456, #789
Resolves jwendl/another-repo#100
```

**Non-closing references** (won't auto-close):
- `Related to #123`
- `Part of #456`
- `See also #789`

**Best practices:**
- Use closing keywords in the **PR description**, not just commits
- One PR should close one primary issue (single responsibility)
- Use "Related to" for dependencies or cross-references
- Issues close automatically when PR is merged to `main`

**For AI Agents:**
Always use the appropriate closing keyword in your PR description to ensure the issue is automatically closed when your work is merged. This keeps the issue tracker clean and up-to-date.

10. **Never Push Directly to `main`**
    - **All changes** must go through pull requests
    - This ensures code review and CI checks
    - Branch protection enforces this rule

### ✅ Agent Pre-Flight Checklist

Before creating a pull request, verify:

- [ ] Read [`.github/instructions/`](.github/instructions/)
- [ ] Created a feature branch (not working on `main`)
- [ ] Code compiles: `dotnet build --configuration Release`
- [ ] Tests pass: `dotnet test --configuration Release`
- [ ] Code formatted: `dotnet format src/`
- [ ] No secrets in code (check `appsettings.json`)
- [ ] One class per file
- [ ] Used Fluent UI components (no raw HTML)
- [ ] Added/updated tests for changed logic
- [ ] Commit messages are descriptive
- [ ] PR description filled out with checklist

### 🚫 Common Agent Mistakes to Avoid

1. **Pushing directly to `main`** ❌
   - Always work on feature branches
   - Create pull requests for review

2. **Hardcoding secrets** ❌
   - Never put API keys, passwords, or tokens in `appsettings.json`
   - Use User Secrets for local dev, Key Vault for production

3. **Ignoring coding standards** ❌
   - Always follow [`.github/instructions/`](.github/instructions/)
   - Use `dotnet format` before committing

4. **Multiple classes in one file** ❌
   - One public class per file
   - Move nested classes to separate files if they're used elsewhere

5. **Using raw HTML instead of Fluent UI** ❌
   - Use `<FluentButton>` not `<button>`
   - Use `<FluentLabel Typo="Typo.H1">` not `<h1>`

6. **Skipping tests** ❌
   - All business logic needs unit tests
   - Target 80%+ coverage on services

7. **Modifying unrelated code** ❌
   - Stay focused on the task at hand
   - Don't refactor unrelated files

8. **Large, monolithic PRs** ❌
   - Keep PRs small and focused
   - One user story or issue per PR

---

## For Human Developers

### Getting Started

1. **Fork the repository** (for external contributors)
   ```bash
   # Clone your fork
   git clone https://github.com/YOUR_USERNAME/claw-mail-cal-cli.git
   cd claw-mail-cal-cli
   
   # Add upstream remote
   git remote add upstream https://github.com/jwendl/claw-mail-cal-cli.git
   ```

2. **Set up your development environment**
   - Install .NET 10 SDK
   - Install Visual Studio 2025 or VS Code
   - Configure User Secrets (see [README.md](README.md#secrets-management))

3. **Pick an issue or create one**
   - Check [GitHub Issues](https://github.com/jwendl/claw-mail-cal-cli/issues)
   - Or review completed implementations in [`docs/summary-reports/`](docs/summary-reports/)
   - Check the [User Story Status Index](docs/summary-reports/user-story-status-index.md) for a full list of stories and their status
   - Comment on the issue to claim it

### Development Tips

- Use the Visual Studio debugger for complex issues
- Run tests frequently during development
- Use `dotnet watch` for auto-rebuild during development:
  ```bash
  dotnet watch --project src/ClawMailCalCli/ClawMailCalCli.csproj
  ```

---

## Development Workflow

See [`workflow.md`](docs/plan/workflow.md) for detailed process documentation.

**Summary:**
1. Branch from `main`
2. Make changes following coding standards
3. Write tests
4. Run build, test, and format
5. Commit with descriptive messages
6. Push to feature branch
7. Create pull request to `main`
8. Address code review feedback
9. Merge after approval and passing CI

---

## Coding Standards

**All code must follow [`.github/instructions/`](.github/instructions/).**

Key points:
- C# 13 features (primary constructors, simplified collections, record types)
- PascalCase for types/methods, camelCase for locals/parameters
- One class per file
- Fluent UI components only (no raw HTML)
- Constructor injection only
- `async`/`await` for all I/O operations
- Guard Azure Functions logs with `logger.IsEnabled()`
- No `#region` directives
- Explicit braces for control statements

**File-specific rules:**
- C# files: [`.github/instructions/csharp.instructions.md`](.github/instructions/csharp.instructions.md)
- Blazor components: [`.github/instructions/blazor.instructions.md`](.github/instructions/blazor.instructions.md)
- Azure Functions: [`.github/instructions/azure-functions.instructions.md`](.github/instructions/azure-functions.instructions.md)
- Tests: [`.github/instructions/tests.instructions.md`](.github/instructions/tests.instructions.md)

---

## Testing Requirements

### Unit Tests (Required)

All business logic must have unit tests:
- Services: 80%+ coverage
- Repositories: 70%+ coverage
- Entities: Test validation logic

**Framework:**
- xUnit for test framework
- Moq for mocking
- FluentAssertions for assertions

**Test structure:**
```csharp
public class GameRecommendationServiceTests
{
    private readonly Mock<IGameRepository> _repositoryMock;
    private readonly GameRecommendationService _sut;

    public GameRecommendationServiceTests()
    {
        _repositoryMock = new Mock<IGameRepository>();
        _sut = new GameRecommendationService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetRecommendationsAsync_WithNoGames_ReturnsEmptyList()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        // Act
        var result = await _sut.GetRecommendationsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }
}
```

### Integration Tests (Recommended)

For critical flows:
- Authentication and authorization
- Database operations
- External API integrations (with mocked HTTP)

---

## Changelog

Every pull request that changes observable behavior **must** update [CHANGELOG.md](CHANGELOG.md).

### What counts as a behavior-changing PR?

- New commands or command options
- Changes to command output or formatting
- Bug fixes that alter how the tool behaves
- Security fixes
- Removed or deprecated functionality
- Changes to configuration file format or environment variables

### How to update the changelog

1. Open `CHANGELOG.md` at the repository root.
2. Under the `## [Unreleased]` section, add your change under the appropriate heading:
   - **Added** — new features
   - **Changed** — changes to existing functionality
   - **Fixed** — bug fixes
   - **Removed** — removed features
   - **Security** — security-related changes
3. Write a concise, user-facing description. Avoid internal implementation details.
4. Reference the related issue number if applicable (e.g., `(#42)`).

**Example:**

```markdown
## [Unreleased]

### Added
- `email search` command to search messages by subject or body text (#42)

### Fixed
- `calendar list` no longer crashes when no events exist in the selected time range (#37)
```

### What does NOT need a changelog entry?

- Refactoring with no behavior change
- Documentation-only updates
- Test additions or changes
- CI/CD infrastructure changes

---

## Pull Request Process

### 1. Create Pull Request

Use the PR template (auto-filled when creating PR). Include:
- **Title**: Concise summary (e.g., "feat: implement Steam analytics (User Story #12)")
- **Description**: What changed and why
- **Linked Issues**: Reference user story or issue number
- **Checklist**: Complete all items

### 2. Code Review

PRs require at least one approval before merging. Reviewers check:
- Code follows [`.github/instructions/`](.github/instructions/)
- Tests are comprehensive and passing
- No secrets committed
- Changes are focused and minimal
- Architecture patterns are followed

### 3. CI Checks

All PRs must pass:
- Build (Release configuration)
- Tests (all must pass)
- Format check (`dotnet format --verify-no-changes`)

### 4. Merge

After approval and passing CI:
- Squash and merge to `main`
- Delete feature branch

---

## Issue Guidelines

### Creating Issues

Use issue templates for consistency:
- **User Story**: New feature request
- **Bug Report**: Something not working
- **Task**: Technical work (refactoring, infrastructure, etc.)

### User Story Template

```markdown
## User Story
As a **[user type]**, I want **[goal]** so that **[benefit]**.

## Acceptance Criteria
- [ ] Criterion 1
- [ ] Criterion 2

## Technical Notes
[Any implementation details, dependencies, or constraints]

## Related Issues
- Depends on #XX
- Blocks #YY
```

### Bug Report Template

```markdown
## Description
[Clear description of the bug]

## Steps to Reproduce
1. Go to '...'
2. Click on '...'
3. Scroll down to '...'
4. See error

## Expected Behavior
[What should happen]

## Actual Behavior
[What actually happens]

## Environment
- OS: [e.g., Windows 11, macOS 14]
- Browser: [e.g., Chrome 120, Edge 120]
- .NET Version: [e.g., .NET 10.0]
```

---

## Questions?

- **Documentation**: Check [README.md](README.md) and [`docs/`](docs/)
- **Issues**: [GitHub Issues](https://github.com/jwendl/claw-mail-cal-cli/issues)
- **Discussions**: [GitHub Discussions](https://github.com/jwendl/claw-mail-cal-cli/discussions)

---

**Thank you for contributing to claw-mail-cal-cli!** 📧
