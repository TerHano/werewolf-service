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

### Watching it play itself

A self-moderated game needs five players before it will deal, which makes "does this screen
look right" an awkward question to ask on your own. `playtest` fills a room with bots that take
their own turns:

```bash
cd apps/web && npm run playtest
```

It prints a room URL, deals, and plays the game out — nights, lynches and all — so you can open
that URL and watch every screen the app can show. The bots ask the server what they are allowed
to do each step rather than reasoning about roles, so they keep working as roles change.

| Flag | Effect |
| --- | --- |
| `--loop` | Deal again when a game ends. Good for watching one screen over and over. |
| `--watch-me` | Wait 30 seconds before dealing, so you can join and be dealt in. |
| `--join ABC12` | Add bots to a room you already made, and let you start the game. |
| `--players 7` | How many bots. Default 5, the minimum for a default room. |
| `--url` | Point at something other than `http://localhost:8080`. |
| `--watch` | Serve two dev pages (below) on `http://localhost:7777`. |

If the badge reaches you — it passes to the first player who dies — the bots stop and wait for
you to run the day, because at that point it is your game to run.

#### Seeing all of it at once

`--watch` adds two pages that no amount of joining a room can give you:

- **`localhost:7777/phones`** — every player's screen side by side, live. Each frame is the
  real app signed in as that player, so you see the werewolf choosing and the doctor waiting
  at the same moment, and hot reload reaches all of them at once.
- **`localhost:7777`** — the whole table: who holds which card, whose step is running, what
  each of them just did, and a running log.

Both are served by the playtest script and not by the app. They exist outside it on purpose:
between them they show every card on the table, which is the one thing the game itself is
built never to be able to do. The phones page works by giving each player their own hostname
(`bea.localhost`, `cal.localhost`, …) so each frame gets its own session — the app needs no
way to impersonate anyone, and gets none.

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
