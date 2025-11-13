# Backend.Tests (unit tests) — README

This README explains how to set up the test project, what NuGet packages the tests use, and how to run the tests and the backend in this workspace (Windows / cmd.exe).

Summary
- Project: `Backend.Tests` (target framework: .NET 9.0)
- Purpose: unit/integration tests for the `Backend` project (project reference at `..\Backend\Backend.csproj`).

Prerequisites
- .NET SDK 9.x installed. Verify with:

```cmd
dotnet --list-sdks
```

- Recommended IDE: Visual Studio (2022/2023) or Rider. CLI is sufficient for running tests.

Quick setup
1. Restore packages

```cmd
cd C:\Users\Bianca Milea\Desktop\uni\III\DotNET_3B3_UniShare_Project\Backend.Tests
dotnet restore
```

2. Build

```cmd
dotnet build
```

Run tests
- Run all tests (build will run by default):

```cmd
dotnet test
```

Run the backend application
- The tests project references the main `Backend` project. To run the backend (if present at `..\Backend\Backend.csproj`), from this folder run:

```cmd
dotnet run --project ..\Backend\Backend.csproj --configuration Debug
```

Packages used (from `Backend.Tests.csproj`)
- coverlet.collector (6.0.2) — collects code coverage data during `dotnet test`.
- FluentAssertions (8.8.0) — assertion library for readable test assertions.
- FluentValidation (12.1.0) — validation library used by code under test or to validate DTOs in tests.
- MediatR (13.1.0) — mediator pattern library (used by the app; tests may exercise handlers).
- Microsoft.EntityFrameworkCore.InMemory (9.0.11) — in-memory EF Core provider for tests.
- Microsoft.NET.Test.Sdk (17.12.0) — test host / SDK integration for running tests.
- Moq (4.20.72) — mocking library for creating test doubles.
- xunit (2.9.2) and xunit.runner.visualstudio (2.8.2) — xUnit test framework and runner integration.

