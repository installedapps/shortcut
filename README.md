# Shortcut

WIP scaffold for a simple AI-based photo assistant. Upload a reference photograph and Shortcut
returns Lightroom and Darktable settings you can use as a starting point for a similar look.

The backend can call Kimi/Moonshot for AI-backed photo analysis when a local API key is configured.
Without a key, it falls back to a deterministic local heuristic service for offline development.

## Stack

- Frontend: React, TanStack Query, Vite, TypeScript, plain CSS
- Backend: .NET minimal API
- Database: PostgreSQL, with a JSONB settings payload

The result view uses a hamburger menu to switch between Lightroom settings and Darktable module
settings. Darktable output uses AgX as the display transform, then lists starting values only for
AgX, local contrast, color balance RGB, color equalizer, and tone equalizer.

## Current WIP Scope

- Upload a photograph through the React frontend.
- Send the image to the .NET API at `/api/analyses`.
- Return separate `lightroomSettings` and `darktableSettings` payloads.
- Persist analysis records to PostgreSQL when `ConnectionStrings__ShortcutDb` is configured.
- Fall back to an in-memory repository when no database connection string is provided.

## API keys

Do not commit API keys. Store a Kimi/Moonshot key in local user-secrets or an environment variable.

Using .NET user-secrets:

```sh
cd backend/src/Shortcut.Api
dotnet user-secrets set "Kimi:ApiKey" "your-key-here"
```

Or using an environment variable for a single shell:

```sh
export Kimi__ApiKey="your-key-here"
```

The backend also accepts `Moonshot:ApiKey` / `Moonshot__ApiKey` and `MOONSHOT_API_KEY`.

## Run locally

Start PostgreSQL:

```sh
docker compose up -d postgres
```

Run the API:

```sh
cd backend/src/Shortcut.Api
ConnectionStrings__ShortcutDb="Host=localhost;Port=5432;Database=shortcut;Username=shortcut;Password=shortcut" dotnet run --urls http://localhost:5088
```

Run the frontend:

```sh
cd frontend
pnpm install
pnpm dev
```

Open `http://localhost:5173`.

## Error logs

The API writes one log file per analysis error to `logs/errors` under the API content root. Each
file includes the UTC timestamp, trace ID, error type, user-facing message, request path, upload
metadata, and exception details when available.

Override the directory with:

```sh
LogFiles__ErrorDirectory="/tmp/shortcut-errors"
```

## Test

```sh
cd frontend
pnpm test
```

```sh
cd backend
DOTNET_CLI_HOME=/tmp/dotnet_home dotnet test
```
