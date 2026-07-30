# Werewolf Party — Server

Backend for **Werewolf Party**, a companion app for playing the social deduction game
*Werewolf* (a.k.a. Mafia) **in person**. The app does not replace the face-to-face game — it
replaces the paper cards, the moderator's notepad, and the arguments about who healed whom.

One person creates a room, everyone else joins on their own phone with a 5-character code,
the app deals the role cards, and the moderator gets a guided night-by-night control panel
that tracks actions, deaths, and win conditions.

- **This repo:** ASP.NET Core 8 minimal API + SignalR + PostgreSQL.
- **Frontend repo:** `werewolf-party-vite` (React + TypeScript + Vite).

## Documentation

| Document | Contents |
| --- | --- |
| [docs/game-flow.md](docs/game-flow.md) | How a game runs: lobby, dealing, night calls, day vote, win conditions, and every role's abilities |
| [docs/architecture.md](docs/architecture.md) | Layers, request flow, authentication, real-time events, database schema |
| [docs/api-reference.md](docs/api-reference.md) | Every REST endpoint and SignalR event |

## What the app does

**For players**

- Join a room by code or shared link — no account, no install.
- Pick a nickname and an avatar.
- Receive a secret role card on your own phone once the moderator deals.

**For the moderator**

- Configure which roles are in play and how many werewolves.
- Deal roles to everyone (the moderator is dealt out of the game and runs it).
- Step through a **night call** wizard, one role at a time, queueing each player's action.
- End the night and see exactly who died — after heals, kills, and revives are resolved.
- Run the day's **chopping block** vote, or abstain.
- Get told when a faction has won, and optionally show everyone a night-by-night replay
  of the whole game.
- Hand the moderator role to someone else, or kick a player.

## Tech stack

| Concern | Choice |
| --- | --- |
| Runtime | .NET 8 (`net8.0`) |
| HTTP | ASP.NET Core minimal APIs |
| Real-time | SignalR (`/Events` hub) |
| Database | PostgreSQL via EF Core 9 + Npgsql |
| Auth | Anonymous JWT bearer tokens (HMAC-SHA256) |
| Mapping | AutoMapper |
| Validation | FluentValidation |
| API docs | Swashbuckle / Swagger (Development only) |

## Running locally

### 1. Start PostgreSQL

The compose file in this repo brings up both the API image and a Postgres container. For
local development against the source you usually only want the database:

```bash
docker compose up -d app_db
```

Or point `ConnectionStrings:DefaultConnection` at any Postgres instance you already have.

### 2. Apply migrations

```bash
dotnet ef database update --project WerewolfParty-Server
```

`script.sql` in the repo root is an equivalent idempotent SQL script if you would rather
apply the schema by hand.

### 3. Run the API

```bash
dotnet run --project WerewolfParty-Server --launch-profile http
```

The API listens on `http://localhost:5049` and opens Swagger UI at
`http://localhost:5049/swagger`. Swagger is only registered in the Development
environment.

### 4. Run the frontend

Start `werewolf-party-vite` with `npm run dev`; it serves on `http://localhost:5173`,
which is the origin allowed by `appsettings.Development.json`.

## Configuration

Settings come from `appsettings.{Environment}.json` or environment variables (double
underscore for nesting, e.g. `Auth__PrivateKey`).

| Key | Purpose |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | Postgres connection string. Startup **throws** if missing. |
| `AllowedOrigins` | CORS origin for the frontend. Credentials are allowed, so this must be a real origin, not `*`, in any deployed environment. |
| `Auth:PrivateKey` | Symmetric HMAC-SHA256 signing key for player tokens. Startup throws if missing. |
| `Auth:Issuer` / `Auth:Audience` | JWT issuer and audience, both validated on every request. |

> The key committed in `appsettings.json` / `appsettings.Development.json` is a local
> development placeholder. Deployed environments must supply their own via environment
> variables — anyone holding the key can mint a token for any player id.

## Deployment

`.github/workflows/docker-image.yml` runs on pushes and PRs to `main`:

1. Builds `WerewolfParty-Server/Dockerfile` (multi-stage; publishes to
   `mcr.microsoft.com/dotnet/aspnet:8.0`).
2. Pushes the image to `ghcr.io/<owner>/<image>` — `main` is tagged `latest`.
3. SSHes into the host and runs `docker pull` + `docker compose up -d` in the project
   directory.

The runtime compose stack (`docker-compose.yml`) is the API container plus a Postgres
container on a shared bridge network, with all configuration supplied as environment
variables from a `.env` file (`ALLOWED_ORIGINS`, `DB_USERNAME`, `DB_PASSWORD`, `DB_NAME`,
`AUTH_AUDIENCE`, `AUTH_ISSUER`, `AUTH_PRIVATE_KEY`).

## Project layout

```
WerewolfParty-Server/
├── API/            Minimal API endpoint registration (Room, Player, Game)
├── Service/        Game and room orchestration, JWT issuing
├── Role/           One class per role — decides which actions that role may take
├── Repository/     EF Core data access
├── Entities/       Database entities (table-mapped)
├── DTO/            Request/response shapes
├── Mappers/        AutoMapper profiles
├── Hubs/           SignalR hub + typed client interface
├── Enum/           RoleName, ActionType, GameState, WinCondition, …
├── Validator/      FluentValidation validators
├── Migrations/     EF Core migrations
└── Exceptions/     Domain exceptions + global exception handler
```
