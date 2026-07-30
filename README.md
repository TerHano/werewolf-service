# Werewolf Party — Web Client

Frontend for **Werewolf Party**, a companion app for playing the social deduction game
*Werewolf* (a.k.a. Mafia) **in person**. Everyone plays on their own phone: the app deals
the role cards, and the moderator gets a guided control panel for running the night, the
morning deaths, and the day's vote.

Backend: `WerewolfParty-Server` (ASP.NET Core 8 + SignalR + PostgreSQL). Game rules,
role abilities, and action resolution all live there — this client renders what the server
says is allowed. See that repo's `docs/` for the game flow and API reference.

## Getting started

```bash
npm install
npm run dev
```

The dev server runs on `http://localhost:5173`, which is the origin the backend allows by
default in its Development configuration. Start the backend first — the app cannot render
anything until it has obtained a player token and opened a SignalR connection.

### Environment

Vite is configured with `envPrefix: ["WEREWOLF"]`, so environment variables must start with
`WEREWOLF` (note: **no** `VITE_` prefix, and they are read as `import.meta.env.WEREWOLF_*`).

```
# .env
WEREWOLF_SERVER_URL=http://localhost:5049
```

### Scripts

| Script | Purpose |
| --- | --- |
| `npm run dev` | Vite dev server |
| `npm run dev-host` | Same, exposed on the local network — handy for testing on real phones |
| `npm run build` | Type-check (`tsc -b`) then build |
| `npm run preview` | Serve the production build |
| `npm run lint` | ESLint (includes `eslint-plugin-i18next` to catch untranslated literals) |
| `npm run typegen` | Regenerate Chakra theme typings from `src/theme.ts` |
| `npm run generate-types` | Generate `src/types/api.d.ts` from the backend's Swagger doc (backend must be running) |

## Stack

| Concern | Choice |
| --- | --- |
| Framework | React 18 + TypeScript, built by Vite 6 (SWC) |
| UI | Chakra UI v3, Tabler/React icons, Swiper (avatar picker) |
| Routing | TanStack Router — file-based, generated into `src/routeTree.gen.ts` |
| Server state | TanStack Query |
| Real-time | `@microsoft/signalr` |
| Forms | react-hook-form |
| i18n | i18next / react-i18next — English, Spanish, Chinese |

## How it fits together

### Identity

There are no accounts. On first load the root route calls `POST /api/player/get-id`, gets a
JWT, and stores it in a `session` cookie (`src/util/cookie.ts`). Every request sends it as a
bearer token; the SignalR client supplies it via `accessTokenFactory`. Clearing the cookie
means becoming a new player.

### Data access

Two thin wrappers over TanStack Query sit on top of a single `fetch` helper:

- **`src/util/api.ts`** — `getApi<T>()` adds the bearer token, applies a 10-second timeout,
  unwraps the backend's `{ success, data, errorMessages }` envelope, and throws an `Error`
  carrying the first error message when `success` is false.
- **`useApiQuery`** — reads. Takes a query key, an endpoint path (relative to
  `${WEREWOLF_SERVER_URL}/api/`), and options. Default `staleTime` is 5s.
- **`useApiMutation`** — writes. Takes an endpoint, a method, and a list of query keys to
  invalidate on success.

Everything else in `src/hooks/` is a named wrapper around one of those two, one per backend
endpoint — `usePlayers`, `useAssignedRole`, `useStartGame`, `useEndNight`,
`useVotePlayerOut`, and so on. To find the server call behind a screen, follow the hook.

### Real-time updates

`SocketProvider` (`src/context/SocketProvider.tsx`) owns the single SignalR connection to
`/Events`. It **gates the whole app**: until the connection reports `Connected`, children
are replaced by a connecting spinner or a disconnected state with a reconnect button.
Automatic reconnection is enabled.

`useSocketConnection` is how components subscribe. Pass only the callbacks you care about;
the hook registers and cleans up the matching handlers, and also exposes `joinRoom`,
`getConnectionId` and `attemptReconnection`.

```tsx
useSocketConnection({
  onDayOrTimeUpdated: () => refetchDayDetails(),
  onWinConditionMet: () => refetch(),
});
```

Server events carry almost no data — they are cache-invalidation signals. The standard
pattern is: an event fires, the handler refetches or invalidates the relevant query.

Available callbacks: `onLobbyUpdated`, `onModeratorUpdated`, `onPlayerKicked`,
`onRoomRoleSettingsUpdated`, `onGameStateChanged`, `onDayOrTimeUpdated`,
`onWinConditionMet`, `onGameRestart`, `onReconnect`.

### Routing and screen flow

```
/                    MainMenu — create a room, or enter a code to join
/room/$roomId        The room
```

`/room/$roomId` has a loader that checks the room exists (redirecting to `/` if not) and
whether the caller has already joined. From there the rendering cascade is:

```
$roomId.tsx
├─ not yet joined ──► AddEditPlayerModal        (nickname + avatar, then JoinRoom over the hub)
└─ joined
   ├─ GameState.Lobby ──► Lobby                 room code, moderator card, role settings,
   │                                            player list, start-game button
   └─ GameState.CardsDealt ──► GameRoom
      ├─ win condition set ──► WinConditionPage  (+ GameSummaryTimeline if enabled)
      ├─ is moderator ──────► ModeratorView
      │                       ├─ night ──► NightCall      role-call stepper
      │                       └─ day ────► ChoppingBlock  deaths banner + lynch vote
      └─ otherwise ─────────► PlayerView         your secret role card
```

The moderator/player split is decided by `useIsModerator` — the same route renders a
completely different experience for the person running the game.

### The night call

`NightCall` is the heart of the moderator experience. It fetches every player's role and
available actions (`useAllPlayerRoles`), sorts them by `roleCallPriority` from
`useRoles`, and presents a Chakra stepper: werewolves first, then each role flagged
`showInModeratorRoleCall`, then a "night complete" card.

Each step renders a `PlayerActionCard` whose buttons come straight from the server's
`RoleActionDto` list — including `enabled` and `disabledReason`, which is rendered as a
tooltip. Selecting a target queues the action; the moderator can change or delete it until
the night is ended. The Detective's investigation is the exception: it resolves immediately
through `useInvestigatePlayer` and shows the result in a modal.

### Role metadata

`src/hooks/useRoles.tsx` is the client-side catalogue of roles: display label, short and
long descriptions (all translated), artwork, role type (used for colour coding), whether
the role appears in the moderator's night call, and its call priority. The role
*descriptions* live in the locale files under `roles.*`. The role *behaviour* does not live
here at all — it is enforced by the server.

### i18n

`src/i18n.ts` loads `en`, `es` and `zh` from `src/locales/`. English is both the default and
the fallback. The language picker sits in the root layout, below the page content. Lint
rules flag literal strings in JSX, so user-facing text should go through `t()`.

## Project layout

```
src/
├── routes/          TanStack Router file routes (__root, index, room/$roomId)
├── components/
│   ├── MainMenu/    Create/join a room
│   ├── Lobby/       Pre-game: players, avatars, moderator, role settings
│   ├── GameRoom/
│   │   ├── ModeratorView/   NightView (role-call stepper, action modals),
│   │   │                    DayView (deaths banner, chopping block)
│   │   ├── PlayerView/      The player's own role card
│   │   └── GameSummaryTimeline/  End-of-game replay
│   ├── ui/          Chakra UI snippet components
│   └── ui-addons/   Local additions (toaster, skeleton, clipboard button, provider)
├── hooks/           One hook per backend endpoint, plus useRoles / useSocketConnection
├── context/         RoomContext (current room id), SocketContext + SocketProvider
├── dto/             TypeScript mirrors of the backend DTOs
├── enum/            TypeScript mirrors of the backend enums — keep numeric values in sync
├── locales/         en / es / zh translation files
├── util/            api fetch helper, cookie helper, responsive drawer placement
└── assets/          Role artwork, avatars, fonts, icons
```

`src/dto/` and `src/enum/` are hand-maintained copies of the server's contracts. When the
backend's enums or DTOs change, update them here too — or regenerate the typed API surface
with `npm run generate-types`.
