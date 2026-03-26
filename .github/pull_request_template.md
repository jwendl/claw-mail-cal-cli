## Description

Clear description of what this PR changes and why.

## Related Issues

> **Required** — every PR must link to at least one issue using a closing keyword so the issue is automatically closed when this PR is merged to `main`.

<!-- Replace the number below. Use `Closes` for user stories/tasks, `Fixes` for bugs. -->
Closes #

**Supported closing keywords** (case-insensitive): `closes`, `fixes`, `resolves`

**Non-closing references** (won't auto-close the issue):
- Related to #
- Part of #
- See also #

## Type of Change

- [ ] 🐛 Bug fix (non-breaking change which fixes an issue)
- [ ] ✨ New feature (non-breaking change which adds functionality)
- [ ] 💥 Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] 📝 Documentation update
- [ ] ♻️ Code refactoring (no functional changes)
- [ ] ✅ Test additions/updates
- [ ] 🔧 Build/infrastructure changes

## Coding Standards Checklist

**Required** - All items must be checked:

- [ ] Code follows [`.github/instructions/`](../.github/instructions/)
- [ ] Used C# 13 features (primary constructors, simplified collections, record types)
- [ ] One class per file (no multiple classes in one file)
- [ ] Used Fluent UI components (no raw HTML: `<button>`, `<h1>`, etc.)
- [ ] Constructor injection only (no service locator pattern)
- [ ] Azure Functions logs guarded with `logger.IsEnabled()` checks
- [ ] No `#region` directives
- [ ] Explicit braces for all control statements
- [ ] No hardcoded secrets (API keys, passwords, connection strings)
- [ ] Code formatted with `dotnet format src/`

## Testing Checklist

**Required for code changes:**

- [ ] I have performed a self-review of my code
- [ ] I have added unit tests that prove my fix is effective or feature works
- [ ] New and existing unit tests pass locally
- [ ] Test coverage is 80%+ for new services/business logic
- [ ] Integration tests added for critical flows (if applicable)
- [ ] Manual testing completed (for UI changes)

## Build & CI Checklist

**Required** - Verify before creating PR:

- [ ] Build succeeds: `dotnet build --configuration Release`
- [ ] Tests pass: `dotnet test --configuration Release --verbosity normal`
- [ ] Code formatted: `dotnet format src/` (no changes after running)
- [ ] No compiler warnings introduced
- [ ] CI checks pass on GitHub Actions

## Documentation Checklist

- [ ] Updated README.md (if setup/usage changed)
- [ ] Updated architecture docs (if architecture changed)
- [ ] Updated API docs/comments (if public interfaces changed)
- [ ] Created user story summary in [`docs/user-stories/`](../docs/summary-reports/user-stories-status.md) (if implementing a user story)
- [ ] Added XML documentation comments for public APIs

## Testing Summary

**How was this tested?**

Describe the tests you ran to verify your changes:
- [ ] Unit tests (describe which areas)
- [ ] Integration tests (describe scenarios)
- [ ] Manual testing (describe steps)
- [ ] Browser testing (list browsers if UI change)

**Test configuration:**
- OS: [e.g., Windows 11, macOS 14, Ubuntu 22.04]
- .NET Version: [e.g., .NET 10.0]
- Browser (if applicable): [e.g., Chrome 120, Edge 120]

## Screenshots (if applicable)

Add screenshots for UI changes:

**Before:**
[Screenshot of old behavior]

**After:**
[Screenshot of new behavior]

## Breaking Changes

**Does this PR introduce breaking changes?**
- [ ] No
- [ ] Yes (explain below)

**If yes, describe the breaking changes and migration path:**

[Explanation]

## Additional Notes

Any additional information reviewers should know:
- Performance considerations
- Security implications
- Database migrations needed
- Deployment notes
- Known limitations

---

## For Reviewers

**Focus areas for review:**
- [ ] Code follows coding standards
- [ ] Architecture patterns are correct
- [ ] No security vulnerabilities
- [ ] Tests are comprehensive
- [ ] Error handling is appropriate
- [ ] Performance is acceptable

---

**For AI Agents:** See [`CONTRIBUTING.md`](../CONTRIBUTING.md#for-ai-agents) for workflow guidelines.
