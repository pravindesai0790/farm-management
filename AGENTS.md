# Repository Guidelines

## Project Structure & Module Organization

The repository is organized as a layered .NET solution:

- `backend/FarmManagement.sln` is the solution entry point.
- `backend/src/FarmManagement.Domain` contains core business concepts and rules.
- `backend/src/FarmManagement.Application` contains use cases and application orchestration; it references Domain.
- `backend/src/FarmManagement.Infrastructure` contains persistence and external-service implementations, including Entity Framework Core and PostgreSQL integration.
- `backend/src/FarmManagement.API` is the ASP.NET Core HTTP host and local API client entry point (`FarmManagement.API.http`).
- `frontend/`, `docker/`, `docs/`, and `scripts/` are reserved for future client, deployment, documentation, and automation work. Keep generated `bin/` and `obj/` output out of commits.

## Build, Test, and Development Commands

Run commands from `backend/` with the .NET 10 SDK installed:

```powershell
dotnet restore FarmManagement.sln
dotnet build FarmManagement.sln
dotnet run --project src/FarmManagement.API/FarmManagement.API.csproj
dotnet test FarmManagement.sln
```

`dotnet test` is the standard verification command; it currently succeeds only when test projects are added to the solution. The API exposes development-only OpenAPI metadata and uses the configured HTTPS development profile.

## Coding Style & Naming Conventions

Use standard C# conventions: four-space indentation, nullable reference types enabled, implicit usings enabled, `PascalCase` for types and public members, and `camelCase` for local variables and parameters. Prefer focused classes and keep domain logic independent of ASP.NET Core or database concerns. Follow the existing SDK-style `.csproj` layout and format changed C# files with `dotnet format` when available.

## Testing Guidelines

No test projects or coverage thresholds are present yet. Add tests under a parallel `backend/tests/` directory, name projects and files after the production unit (for example, `FarmManagement.Domain.Tests`), and use descriptive method names such as `Create_WhenInputIsInvalid_ReturnsError`. Run the solution test command before submitting changes.

## Commit & Pull Request Guidelines

The history currently contains only `Initial commit`, so no detailed convention is established. Use short, imperative subjects (for example, `Add livestock registration endpoint`). Pull requests should explain the behavior change, identify validation commands run, link the relevant issue, and include request/response examples or screenshots when API or UI behavior changes.

## Security & Configuration Tips

Keep secrets and local connection strings out of tracked configuration. Use environment variables or user-secrets for development values, and review `appsettings.*.json` changes carefully before opening a pull request.
