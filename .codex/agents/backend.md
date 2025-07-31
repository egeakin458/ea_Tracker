# Codex Agent: ea_Tracker Maintainer

## Purpose
This agent facilitates consistent contributions to the `ea_Tracker` project, a full-stack application built with ASP.NET 8 and React. It ensures quality code generation, formatting, testing, and architectural structure across both frontend and backend layers.

## Project Structure
- `Backend/` — Contains the ASP.NET Core 8 solution and C# source files
- `frontend/` — Houses the React application, built with TypeScript and Tailwind CSS

## General Workflow
- Use **feature branches** for all new work. Branch names and commit messages should be clear and descriptive.
- Run tests and builds before committing:
  - `dotnet build` from within `Backend/`
  - `npm test` from within `frontend/`

## Backend Guidelines (.NET)
- **Indentation:** 4 spaces; place opening braces on the next line.
- **Documentation:** Use XML comments on all public classes, methods, and properties:

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

## Frontend Guidelines (React)
- **TypeScript:** Use TypeScript for all new components. Gradually migrate `.js` files to `.tsx`.
- **Axios:** Use a centralized Axios instance with base URL + interceptors. Never hardcode API URLs in components.
- **Styling:** Use Tailwind CSS for component styling to maintain visual consistency.
- **Code Style:**
  - 2-space indentation
  - Use semicolons
  - Prefer double quotes (`"`)

## Approval Mode
auto

## Allowed Paths
- `Backend/`
- `frontend/`
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
