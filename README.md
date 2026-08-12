# Werewolf Party

Cards for a party game full of lies, deceit, and accusations — a companion app that deals the
roles, runs the night, and keeps track of who is still alive. Everyone plays on their own phone,
including the person who used to run the game.

## Running it

You need Docker. That is all.

```bash
docker compose up --build
```

Then open **http://localhost:8080**. The database is created and migrated on first boot; there
is no setup step.

Everything is served from one origin: nginx serves the web app and proxies `/api` and the
SignalR hub through to the server. That means no CORS to configure, and no backend URL compiled
into the frontend bundle, so the same image runs anywhere.

### Development

`compose.override.yaml` is applied automatically and swaps the static build for the Vite dev
server with hot reload. Rename it away to run the production-shaped stack locally.

To run the pieces directly instead:

```bash
cd apps/server && dotnet run --project WerewolfParty-Server   # http://localhost:5049
cd apps/web    && npm install && npm run dev                  # http://localhost:5173
```

The dev server proxies `/api` to `localhost:5049`, so `WEREWOLF_SERVER_URL` can stay empty.

### Configuration

`.env` is committed with working development values. **Override every secret in production** —
in particular `AUTH_PRIVATE_KEY`, which signs player tokens.

| Variable | Purpose |
| --- | --- |
| `AUTH_PRIVATE_KEY` | Signs player tokens. Generate with `openssl rand -base64 48`. |
| `WEB_PORT` | Host port for the app. Default 8080. |
| `DB_USERNAME` / `DB_PASSWORD` / `DB_NAME` | Postgres credentials. |
| `PUSH_*` | Optional VAPID keys for turn notifications. Without them the app falls back to an in-app prompt. Generate with `npx web-push generate-vapid-keys`. |

## Layout

```
apps/server/   ASP.NET Core API, SignalR hub, night engine
apps/web/      React + Vite frontend
docs/          architecture, API reference, game rules, design notes
```

## How the game works

Rooms are **self-moderated** by default: the server calls each role in turn, notifies the
players who act at that step, and resolves the night itself. A room setting hands the night back
to a human moderator if you prefer the classic flow.

- [docs/game-flow.md](docs/game-flow.md) — the game, start to finish, and every role
- [docs/architecture.md](docs/architecture.md) — how the pieces fit together
- [docs/api-reference.md](docs/api-reference.md) — endpoints and events
- [docs/self-moderated-night.md](docs/self-moderated-night.md) — why the night engine is built
  the way it is, including the parts that exist to stop the game leaking

## Deployment

CI builds both images and pushes them to GHCR on every push to `main`, then updates the host
over SSH. Production runs the published images via the overlay:

```bash
docker compose -f compose.yaml -f compose.prod.yaml pull
docker compose -f compose.yaml -f compose.prod.yaml up -d
```
