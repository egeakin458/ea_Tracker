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
./.codex/agents/backend
```

```bash
./.codex/agents/frontend
```

## API Endpoints

- `GET /api/investigations` – list available investigators
- `POST /api/investigations/{id}/start` – start a single investigator
- `POST /api/investigations/{id}/stop` – stop a single investigator
- `GET /api/investigations/{id}/results` – fetch investigation logs

## Codex Automation

This repository is configured to use [OpenAI Codex CLI](https://platform.openai.com/docs/assistants/cli-reference) for automated development workflows.

- Codex agents are defined in `.codex/agents/`:
  - `backend.md` governs the ASP.NET Core backend
  - `frontend.md` governs the React/TypeScript frontend

- Agents operate in **auto approval mode** and follow strict formatting and documentation rules.
- Contribution suggestions or refactors may be generated and applied automatically by Codex.
- The CLI uses `.codex/agents.toml` to map file paths to their appropriate agent rules.

To work with Codex locally:
```bash
npm test
cd Backend && dotnet build
```

## License

This project is licensed under the [MIT License](LICENSE).

