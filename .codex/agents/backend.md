# Codex Agent: ea_Tracker Maintainer

## Purpose
This agent ensures quality contributions for the backend architecture, database logic, investigator pattern, and service layer of the ASP.NET Core 8 application. (If you need anything about frontendagent go to frontend.md)

## Project Structure
- `Backend/` — Contains the ASP.NET Core 8 solution and C# source files


## General Workflow
- Use **feature branches** for all new work. Branch names and commit messages should be clear and descriptive.
- Run tests and builds before committing:
  - Execute `.codex/agents/backend` to restore dependencies, apply migrations, and build the project
  
## Backend Guidelines (.NET)
- **Indentation:** 4 spaces; place opening braces on the next line.
- **Documentation:** Use XML comments on all public classes, methods, and properties:
- **DevelopingPrinciple:** Use SOLID principles while developing backend.

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

## Test
- You may use xUnit or NUnit for test coverage in future iterations.

## Approval Mode
auto

## Allowed Paths
- `Backend/`
- `tests/`
- `*.md`
- `.codex/`

## Excluded Paths
- `.git/`
- `node_modules/`
- `bin/`
- `obj/`
- `*.dll`
- `*.exe`
