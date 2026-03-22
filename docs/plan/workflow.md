# Development Workflow

This document describes the pull request-based development workflow for PTT Game Library. **All code changes must go through pull requests** - direct pushes to `main` are not allowed.

## Table of Contents

- [Branch Strategy](#branch-strategy)
- [Feature Development Workflow](#feature-development-workflow)
- [Pull Request Workflow](#pull-request-workflow)
- [Code Review Guidelines](#code-review-guidelines)
- [Continuous Integration](#continuous-integration)
- [Release Process](#release-process)

---

## Branch Strategy

### Main Branch

- **`main`** - Production-ready code
  - Protected branch (no direct pushes)
  - All changes via pull request
  - Requires passing CI checks
  - Requires code review approval

### Feature Branches

Create feature branches for all work:

```bash
# Always start from latest main
git checkout main
git pull origin main

# Create feature branch
git checkout -b <prefix>/<description>
```

**Branch naming conventions:**

| Prefix | Purpose | Example |
|--------|---------|---------|
| `feature/` | New functionality | `feature/user-story-12-steam-analytics` |
| `fix/` | Bug fixes | `fix/issue-45-swipe-gesture` |
| `refactor/` | Code improvements | `refactor/simplify-recommendation-service` |
| `docs/` | Documentation | `docs/update-setup-instructions` |
| `test/` | Test additions | `test/add-wishlist-repository-tests` |
| `chore/` | Build/tooling | `chore/update-dependencies` |

**Branch naming tips:**
- Use lowercase with hyphens
- Include issue/story number if applicable
- Be descriptive but concise
- Examples:
  - ✅ `feature/user-story-22-wishlist-view`
  - ✅ `fix/issue-123-null-reference-exception`
  - ❌ `my-branch`
  - ❌ `feature`

---

## Feature Development Workflow

### 1. Pick a Task

**From GitHub Issues:**
- Check [Issues tab](https://github.com/jwendl/ptt-game-library/issues)
- Filter by label: `good first issue`, `help wanted`, or your area
- Comment on the issue to claim it

**For Context:**
- Review completed implementations in [`docs/summary-reports/`](../summary-reports/)
- Each summary contains implementation details and testing guides

### 2. Create Feature Branch

```bash
# Ensure main is up to date
git checkout main
git pull origin main

# Create and checkout feature branch
git checkout -b feature/user-story-XX-description

# Or for bug fixes
git checkout -b fix/issue-123-description
```

### 3. Read Documentation

**Required reading:**
1. [`.github/instructions/`](../../.github/instructions/) - Code style and conventions
2. Task description (user story or issue)
3. Related code files and existing tests

**File-specific rules:**
- C# files: [`.github/instructions/csharp.instructions.md`](../../.github/instructions/csharp.instructions.md)
- Blazor: [`.github/instructions/blazor.instructions.md`](../../.github/instructions/blazor.instructions.md)
- Azure Functions: [`.github/instructions/azure-functions.instructions.md`](../../.github/instructions/azure-functions.instructions.md)
- Tests: [`.github/instructions/tests.instructions.md`](../../.github/instructions/tests.instructions.md)

### 4. Make Changes

**Principles:**
- Make minimal, focused changes
- One user story or issue per branch
- Follow [`.github/instructions/`](../../.github/instructions/) strictly
- Don't refactor unrelated code
- Don't fix unrelated bugs

**Code style:**
- Use C# 13 features (primary constructors, simplified collections)
- One class per file
- Use Fluent UI components (no raw HTML)
- Constructor injection only
- Guard Azure Functions logs with `logger.IsEnabled()`
- No `#region` directives

### 5. Write Tests

**Required for:**
- All business logic changes
- New services, repositories, or entities
- Bug fixes (add regression test)

**Test framework:**
- xUnit for test framework
- Moq for mocking
- FluentAssertions for assertions

**Test structure:**
```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetAsync()).ReturnsAsync(expectedData);
    var sut = new Service(mockRepo.Object);

    // Act
    var result = await sut.DoSomethingAsync();

    // Assert
    result.Should().NotBeNull();
}
```

See [`.github/instructions/tests.instructions.md`](../../.github/instructions/tests.instructions.md).

### 6. Validate Changes

**Run validation commands:**

```bash
# 1. Restore dependencies (if new packages added)
dotnet restore src/PttGameLibrary.Web/PttGameLibrary.Web.csproj

# 2. Build (must succeed)
dotnet build src/PttGameLibrary.Web/PttGameLibrary.Web.csproj --configuration Release --no-restore

# 3. Run tests (all must pass)
dotnet test tests/ --configuration Release --verbosity normal

# 4. Format code (auto-fix style issues)
dotnet format src/

# 5. Verify no changes after format
git status
```

**Validation checklist:**
- [ ] Code compiles with no errors
- [ ] All tests pass
- [ ] Code is formatted (`dotnet format`)
- [ ] No secrets in code
- [ ] No uncommitted changes after format

### 7. Commit Changes

**Commit message format:**
```
<type>: <short summary>

<optional detailed description>

Resolves #<issue-number>
Related to #<story-number>
```

**Types:**
- `feat:` - New feature
- `fix:` - Bug fix
- `refactor:` - Code restructuring (no behavior change)
- `test:` - Adding/updating tests
- `docs:` - Documentation changes
- `chore:` - Build, config, or tooling changes

**Examples:**
```bash
git add .
git commit -m "feat: implement Steam play hours analytics

- Add SteamAnalyticsService with play hour calculations
- Add unit tests for analytics service
- Update GameRecommendationService to use play hours

Resolves #12"
```

```bash
git commit -m "fix: resolve null reference exception in swipe gesture

- Add null check before accessing game properties
- Add regression test

Resolves #45"
```

### 8. Push to Remote

```bash
# First time pushing branch
git push -u origin feature/user-story-XX-description

# Subsequent pushes
git push
```

---

## Pull Request Workflow

### 1. Create Pull Request

**On GitHub:**
1. Navigate to repository
2. Click "Pull requests" → "New pull request"
3. Select your feature branch
4. Fill out PR template (auto-populated)

**PR title format:**
```
<type>: <short summary> (<user story #XX | issue #YY>)
```

**Examples:**
- `feat: implement Steam analytics (User Story #12)`
- `fix: resolve swipe gesture null reference (Issue #45)`
- `docs: update setup instructions`

### 2. Fill Out PR Description

The PR template includes:

```markdown
## Description
[Clear description of changes]

## Related Issues
- Closes #XX
- Related to #YY

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update

## Checklist
- [ ] My code follows the coding standards in `.github/instructions/`
- [ ] I have performed a self-review of my code
- [ ] I have commented my code where necessary
- [ ] I have updated documentation as needed
- [ ] My changes generate no new warnings
- [ ] I have added tests that prove my fix is effective or that my feature works
- [ ] New and existing unit tests pass locally
- [ ] I have run `dotnet format src/` and committed any changes

## Testing
[Describe how you tested your changes]

## Screenshots (if applicable)
[Add screenshots for UI changes]
```

### 3. Request Review

**Reviewers:**
- For UI changes: Request review from Blazor experts
- For backend logic: Request review from backend developers
- For security changes: Request review from security-focused reviewers
- AI agents: PR will be auto-reviewed by automated checks

### 4. Address Feedback

**Code review comments:**
- Respond to all comments
- Make requested changes
- Push updates to same branch
- PR updates automatically
- Request re-review when ready

**Common feedback:**
- Coding standard violations → Fix per [`.github/instructions/`](../../.github/instructions/)
- Missing tests → Add unit tests
- Unrelated changes → Remove from PR
- Hardcoded secrets → Move to User Secrets

### 5. Wait for CI Checks

**Automated checks:**
- ✅ Build (Release configuration)
- ✅ Tests (all must pass)
- ✅ Format check (`dotnet format --verify-no-changes`)

**If CI fails:**
- Review failure logs
- Fix issues locally
- Push updates
- CI re-runs automatically

### 6. Merge

**After approval and passing CI:**
1. Maintainer squashes and merges PR
2. Feature branch is deleted automatically
3. Changes are now in `main`

**Post-merge:**
- Update local `main`:
  ```bash
  git checkout main
  git pull origin main
  ```
- Delete local feature branch:
  ```bash
  git branch -d feature/user-story-XX-description
  ```

---

## Code Review Guidelines

### For Reviewers

**Focus areas:**
1. **Coding standards** - Does code follow [`.github/instructions/`](../../.github/instructions/)?
2. **Functionality** - Does it solve the problem described?
3. **Tests** - Are changes covered by tests?
4. **Security** - Any hardcoded secrets or vulnerabilities?
5. **Architecture** - Does it fit the overall design?
6. **Performance** - Any obvious bottlenecks?

**Review checklist:**
- [ ] Code follows coding standards
- [ ] Changes are focused and minimal
- [ ] Tests are comprehensive
- [ ] No secrets committed
- [ ] One class per file
- [ ] Fluent UI components used (no raw HTML)
- [ ] Proper error handling
- [ ] Async/await used for I/O operations
- [ ] Azure Functions logs guarded

**Review comments:**
- Be constructive and specific
- Reference coding standards when applicable
- Ask questions to understand intent
- Suggest alternatives if you have concerns
- Approve when all issues addressed

### For Authors

**Responding to review:**
- Address all comments (or explain why not)
- Make requested changes promptly
- Ask for clarification if needed
- Request re-review when ready
- Be open to feedback

---

## Continuous Integration

### CI Workflow

GitHub Actions runs on every push and PR:

**Jobs:**
1. **Build and Test**
   - Restore dependencies
   - Build in Release mode
   - Run all tests
   - Upload test results

2. **Format Check**
   - Verify code formatting
   - Fails if `dotnet format` would change files

**Configuration:** [`.github/workflows/ci.yaml`](../../.github/workflows/ci.yaml)

### CI Failure Handling

**If CI fails:**
1. Review failure logs on GitHub Actions tab
2. Reproduce locally:
   ```bash
   dotnet build --configuration Release
   dotnet test --configuration Release
   dotnet format src/ --verify-no-changes
   ```
3. Fix issues
4. Push updates
5. CI re-runs automatically

**Common CI failures:**
- Build errors → Fix compilation issues
- Test failures → Fix failing tests
- Format check → Run `dotnet format src/` and commit

---

## Release Process

### Versioning

We use [Semantic Versioning](https://semver.org/):
- `MAJOR.MINOR.PATCH` (e.g., `1.2.3`)
- `MAJOR` - Breaking changes
- `MINOR` - New features (backward compatible)
- `PATCH` - Bug fixes (backward compatible)

### Creating a Release

1. **Update version** in project files
2. **Update CHANGELOG.md** with release notes
3. **Create release tag**:
   ```bash
   git tag -a v1.2.3 -m "Release version 1.2.3"
   git push origin v1.2.3
   ```
4. **Create GitHub Release** with release notes
5. **Deploy to production** (manual or automated)

---

## Summary

**Key principles:**
- ✅ All changes via pull request
- ✅ Never push directly to `main`
- ✅ Follow coding standards strictly
- ✅ Write tests for all logic changes
- ✅ One issue/story per branch
- ✅ Keep changes focused and minimal
- ✅ Get code review approval
- ✅ Ensure CI passes before merge

**Questions?**
- See [`CONTRIBUTING.md`](../../CONTRIBUTING.md)
- Check [GitHub Discussions](https://github.com/jwendl/ptt-game-library/discussions)
- Review documentation in [`docs/`](./)

---

**Happy coding!** 🎮
