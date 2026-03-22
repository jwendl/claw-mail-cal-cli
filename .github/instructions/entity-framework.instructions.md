---
applyTo: "**/Data/**/*.cs,**/Repositories/**/*.cs,**/*DbContext.cs"
---

# Entity Framework Core Instructions

Follow these guidelines for Entity Framework Core in this repository.

## Indentation

- **Always use tabs (not spaces) for indentation.** Tab width is set to 4 spaces for display.

## General Guidelines

- Use InMemory database for local development with Entity Framework and SQL tasks and unit tests.
- Use SQL Server / Azure SQL for staging and production environments.
- Use migrations to manage database schema changes.

## Best Practices

- Use async methods for all database operations
- Use `AsNoTracking()` for read-only queries to improve performance
- Use explicit loading or eager loading instead of lazy loading
- Always dispose DbContext properly (use dependency injection with scoped lifetime)
