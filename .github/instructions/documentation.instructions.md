# Documentation Instructions

Follow these conventions for creating and organizing documentation in this repository.

## Documentation Organization

All documentation should be placed in the `docs/` directory, organized by category:

### Documentation Categories

| Category | Location | Description | Examples |
|----------|----------|-------------|----------|
| **Diagrams** | `docs/diagrams/` | Architecture diagrams (Eraser.io source + exported PNGs) | `system-architecture.eraserdiagram`, `system-architecture.png` |
| **Architecture** | `docs/architecture/` | Technical architecture, deployment, infrastructure, setup guides | `architecture.md`, `deployment.md`, `github-oidc-setup.md` |
| **Research** | `docs/research/` | Investigation, analysis, and research documents | `eraser-mcp-investigation.md`, `missing-work-analysis.md` |
| **Plan** | `docs/plan/` | Planning, workflow, GitHub issues tracking, and guides | `workflow.md`, `github-issues-guide.md` |
| **Summary Reports** | `docs/summary-reports/` | One-time summaries, implementation reports, fix summaries | `implementation-summary.md`, `us-21-implementation-summary.md` |

### Allowed Subfolders

Only these 5 subfolders are allowed under `docs/`:
- `docs/diagrams/`
- `docs/architecture/`
- `docs/research/`
- `docs/plan/`
- `docs/summary-reports/`

## Naming Conventions

- Use **kebab-case.md** for all documentation files (e.g., `github-oidc-setup.md`, `us-21-implementation-summary.md`)
- **Exception**: Standard GitHub files at the repository root keep their conventional names: `README.md`, `CONTRIBUTING.md`, `CHANGELOG.md`, `LICENSE.md`, `CODE_OF_CONDUCT.md`
- `docs/README.md` also keeps the `README.md` name as a standard GitHub file
- Include descriptive prefixes/suffixes for clarity:
  - `us-XX-` for user story documentation
  - `*-fix.md` for problem/solution documentation
  - `*-summary.md` for work summaries
  - `*-guide.md` for step-by-step guides
  - `*-setup.md` for configuration guides

## When Creating New Documentation

### Determine the correct folder:
- **Deployment, infrastructure, or setup topic?** → `docs/architecture/`
- **Investigation or research topic?** → `docs/research/`
- **Planning, workflow, or issue tracking?** → `docs/plan/`
- **One-time summary, fix report, or implementation report?** → `docs/summary-reports/`
- **Diagram source or image?** → `docs/diagrams/`

### Use descriptive kebab-case names:
- `docs/architecture/bicep-deployment-fix.md` - Deployment fix/solution
- `docs/summary-reports/dependabot-fix-summary.md` - Dependency fix summary
- `docs/summary-reports/us-21-implementation-summary.md` - User story implementation
- `docs/research/eraser-mcp-investigation.md` - Research/investigation
- `docs/plan/github-issues-guide.md` - Planning/workflow guide

### Never place documentation at repository root unless it's:
- `README.md` - Main repository readme
- `CONTRIBUTING.md` - Contribution guidelines
- Other repository-level metadata files (CHANGELOG.md, LICENSE.md, CODE_OF_CONDUCT.md)

### Structure of Fix/Summary Documentation

When documenting a fix or completed work, include:

```markdown
# [Title of Issue/Fix]

## Issue Description
[Clear description of the problem]

## Root Cause
[Technical explanation of why the issue occurred]

## Solution
[Detailed explanation of the fix]

### Key Changes
[Bulleted list of specific changes made]

## Files Modified
[List of files changed with brief descriptions]

## Validation
[How the fix was tested and validated]

## Benefits
[Improvements gained from the fix]

## References
[Links to related documentation, issues, or external resources]
```

## Examples

### Good Documentation Placement

✅ `docs/architecture/bicep-deployment-fix.md` - Deployment fix  
✅ `docs/summary-reports/us-21-implementation-summary.md` - User story work  
✅ `docs/summary-reports/dependabot-fix-summary.md` - Dependency fix  
✅ `docs/diagrams/system-architecture.eraserdiagram` - Architecture diagram  
✅ `docs/research/missing-work-analysis.md` - Investigation/research  
✅ `docs/plan/workflow.md` - Planning/workflow  

### Bad Documentation Placement

❌ `docs/deployment-issues/BICEP-DEPLOYMENT-FIX.md` - `deployment-issues/` folder no longer exists; use `docs/architecture/`  
❌ `docs/user-stories/US-22-IMPLEMENTATION-SUMMARY.md` - `user-stories/` folder no longer exists; use `docs/summary-reports/`  
❌ `docs/SOME-RANDOM-FIX.md` - Should be categorized in the appropriate subfolder  
❌ `BICEP_DEPLOYMENT_FIX.md` - Wrong case; use kebab-case: `bicep-deployment-fix.md`  

## Migration of Existing Files

If you find misplaced documentation files:
1. Use `git mv` to move them to the appropriate category folder with kebab-case name
2. Update any internal relative path references that changed
3. Commit with a clear message: "Move [filename] to [new location] for better organization"
