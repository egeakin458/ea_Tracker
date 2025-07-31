# AGENTS Contribution Guidelines

This document describes the conventions and rules for working on the **ea_Tracker** project, which is composed of an ASP.NET 8 back‑end and a React front‑end. Following these guidelines will help ensure a consistent structure and a healthy code base. (This is the frontendagent.md) If you need anything about the backendagent go to (backend.md)

## Project Structure
- `frontend/` — Houses the React application, built with TypeScript and Tailwind CSS

## General Workflow

The repository separates server and client code to keep responsibilities clear.

- The React application under `frontend/`.
- Always create feature branches when developing new work. Use descriptive branch names and commit messages to explain the intent of your changes.
- Before committing, run the project’s tests locally:
- Before npm start control the backends state, is it okay to start etc. 
- Run `.codex/agents/frontend` which installs dependencies and then executes `CI=true npm test --silent -- --passWithNoTests`.
- Clarify Axios setup
 Axios instance should live:
 Place it in `frontend/src/lib/axios.ts` or a similar shared location. 

## Approval Mode
auto

## Frontend Guidelines

The front‑end is a React application. The following rules ensure consistent coding style and maintainability.

- **TypeScript:** Use **TypeScript** for all new components, and gradually migrate existing JavaScript files to TypeScript.
- **HTTP clients:** Configure a single Axios instance with a base URL and interceptors. Avoid hardcoding URLs directly in components.
- **Styling:** Style components using **Tailwind CSS**. This ensures uniform design and responsive layouts.
- **Code style:** Follow the existing coding style:
  - Use **2 spaces** for indentation.
  - End statements with semicolons.
  - Prefer **double quotes** (`"`) for strings.

