# Codex Agent: ea_Tracker Maintainer

## Purpose
Ensure high-quality backend development using SOLID principles, clean OOP (encapsulation, inheritance, polymorphism), and modular architecture. This agent maintains ASP.NET Core 8 code, service layers, investigator patterns, and database logic using MySQL.

## Stack
- ASP.NET Core 8
- C#
- Entity Framework Core 8
- MySQL (via Pomelo.EntityFrameworkCore.MySql)
- Swagger (via Swashbuckle.AspNetCore)
- DotNetEnv for .env config support
- xUnit or NUnit (for future tests)

## Responsibilities
- Maintain clean separation between controllers, services, and repositories
- Use SOLID principles in all services and investigator implementations
- Follow dependency injection patterns using interfaces
- Apply proper error handling and logging in external operations
- Enforce consistent Investigator base pattern

## Object-Oriented Guidelines
- **Encapsulation:** Expose functionality through public interfaces; keep internals private.
- **Inheritance:** Base classes like `Investigator` should encapsulate common logic.
- **Polymorphism:** Use interfaces for services (`IUserService`, `ITaskService`) and implement with DI.
- **Abstraction:** Controllers must call services, not repositories or DbContext directly.


## General Workflow
- Use **feature branches** for all new work. Branch names and commit messages should be clear and descriptive.
- Run tests and builds before committing:
  - Execute `.codex/agents/backend` to restore dependencies, apply migrations, and build the project
  

-

  ```csharp
  /// <summary>
  /// Brief description of the method.
  /// </summary>
  ```

- **Investigators:**
  - Each `Investigator` implementation must define a unique `ID` or `Name`.
  - Prefer using the template pattern for consistent lifecycle management:

    ```csharp
    public abstract class Investigator
    {
        public void Start()
        {
            Log("Investigator started");
            try { OnStart(); }
            finally { Log("Investigator finished"); }
        }

        protected abstract void OnStart();
        public abstract void Stop();

        protected void Log(string message) =>
            Console.WriteLine($"[{DateTime.Now}] {message}");
    }
    ```

- **Error Handling:** Wrap long-running or external operations in `try/catch` blocks to prevent unhandled exceptions.

Code Style
Indentation: 4 spaces

Braces: K&R style (next line)

Comments: Use XML-style comments for public members

csharp

/// <summary>
/// Brief summary of what this method does.
/// </summary>

## Tests
Use xUnit or NUnit

Structure tests under tests/

Write coverage for services, investigators, and important business logic

## Approval Mode
auto

## Allowed Paths
Backend/

tests/

*.cs

*.csproj

.codex/

*.md

## Excluded Paths
.git/

node_modules/

bin/

obj/

*.dll

*.exe