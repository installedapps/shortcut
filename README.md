# Shortcut

WIP scaffold for a simple AI-based photo assistant. Upload a reference photograph and Shortcut
returns Lightroom and Darktable settings you can use as a starting point for a similar look.

The current analysis service is intentionally placeholder/heuristic. It proves the frontend,
backend, API contract, tests, and database shape, but it does not yet call a real vision model.

## Stack

- Frontend: React, TanStack Query, Vite, TypeScript, plain CSS
- Backend: .NET minimal API
- Database: PostgreSQL, with a JSONB settings payload

The result view uses a hamburger menu to switch between Lightroom settings and Darktable module
settings. Darktable output recommends one display transform at a time, then lists starting values for
AgX, color balance rgb, color equalizer, and tone equalizer.

## Current WIP Scope

- Upload a photograph through the React frontend.
- Send the image to the .NET API at `/api/analyses`.
- Return separate `lightroomSettings` and `darktableSettings` payloads.
- Persist analysis records to PostgreSQL when `ConnectionStrings__ShortcutDb` is configured.
- Fall back to an in-memory repository when no database connection string is provided.

## Next Step

Replace `HeuristicPhotoAnalysisService` with a real AI-backed `IPhotoAnalysisService`
implementation. Keep API keys in backend configuration only, for example:

```sh
OpenAI__ApiKey="your-key-here"
```

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
npm install
npm run dev
```

Open `http://localhost:5173`.

## Test

```sh
cd frontend
npm test
```

```sh
cd backend
DOTNET_CLI_HOME=/tmp/dotnet_home dotnet test
```
