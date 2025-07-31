# ea_Tracker

This project combines an ASP.NET Core backend with a React frontend.

## Connection String Configuration

The backend no longer stores the database connection string in `appsettings.json`.
Instead, set an environment variable named `DEFAULT_CONNECTION` before running the
application or building the backend.

Example:

```bash
export DEFAULT_CONNECTION="server=localhost;database=ea_tracker_db;user=root;password=yourpassword;"
```

The `Program.cs` file reads this variable at startup and throws an error if it is missing.

## Building

Run the following commands from the project root:

```bash
cd Backend
dotnet build
```

```bash
cd ../frontend
npm test
```

