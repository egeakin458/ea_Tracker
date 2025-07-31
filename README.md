# ea_Tracker

This project combines an ASP.NET Core backend with a React frontend.

## Connection String Configuration

The backend no longer stores the database connection string in `appsettings.json`.
You can provide the `DEFAULT_CONNECTION` value either directly as an environment
variable or through a `secret.env` file used for local development.

Example environment variable:

```bash
export DEFAULT_CONNECTION="server=localhost;database=ea_tracker_db;user=root;password=yourpassword;"
```

When running locally, create a file named `secret.env` in the repository root
containing the connection string:

```bash
DEFAULT_CONNECTION="server=localhost;database=ea_tracker_db;user=root;password=yourpassword;"
```

`Program.cs` loads this file at startup so the environment variable is available
automatically. The application will still throw an error if `DEFAULT_CONNECTION`
is missing after loading the file.

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

## License

This project is licensed under the [MIT License](LICENSE).

