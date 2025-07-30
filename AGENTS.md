# AGENTS Contribution Guidelines

This document describes the conventions and rules for working on the **ea_Tracker** project, which is composed of an ASP.NET 8 back‑end and a React front‑end. Following these guidelines will help ensure a consistent structure and a healthy code base.

## General Workflow

The repository separates server and client code to keep responsibilities clear.

- The `.NET` solution must live under the `Backend/` directory, and the React application under `frontend/`.
- Always create feature branches when developing new work. Use descriptive branch names and commit messages to explain the intent of your changes.
- Before committing, run the project’s tests locally:
  - Execute `dotnet build` from within the `Backend/` folder to verify the server builds successfully.
  - Run `npm test` from within the `frontend/` folder to ensure the client passes all tests.

## Backend (.NET) Guidelines

The server side uses ASP.NET 8. Consistency in formatting and documentation improves readability and maintainability.

- **Indentation:** Use **4 spaces** for indentation, and place opening braces on the next line rather than on the same line.
- **Documentation:** Document every class, method and property using C# XML comments:

  ```csharp
  /// <summary>
  /// Brief description here.
  /// </summary>
  ```

- **Investigators:** Each `Investigator` implementation should expose a unique `ID` or `Name` property for tracking and identification.
- **Template pattern:** Consider basing `Investigator` types on a template pattern to standardize start/stop behaviour. The following example illustrates the pattern:

  ```csharp
  public abstract class Investigator
  {
      public void Start()
      {
          Log("Investigator started");
          try
          {
              OnStart();
          }
          finally
          {
              Log("Investigator finished");
          }
      }

      protected abstract void OnStart();

      public abstract void Stop();

      protected void Log(string message) => Console.WriteLine($"[{DateTime.Now}] {message}");
  }
  ```

- **Exception handling:** Wrap long‑running operations in `try/catch` blocks to avoid unhandled exceptions and improve stability.

## Frontend Guidelines

The front‑end is a React application. The following rules ensure consistent coding style and maintainability.

- **TypeScript:** Use **TypeScript** for all new components, and gradually migrate existing JavaScript files to TypeScript.
- **HTTP clients:** Configure a single Axios instance with a base URL and interceptors. Avoid hardcoding URLs directly in components.
- **Styling:** Style components using **Tailwind CSS**. This ensures uniform design and responsive layouts.
- **Code style:** Follow the existing coding style:
  - Use **2 spaces** for indentation.
  - End statements with semicolons.
  - Prefer **double quotes** (`"`) for strings.

