# Architecture

## System shape

```
┌──────────────────────────┐         REST (Bearer JWT)        ┌───────────────────────────┐
│  werewolf-party-vite     │ ───────────────────────────────► │  WerewolfParty-Server     │
│  React + TanStack Query  │                                  │  ASP.NET Core 8           │
│                          │ ◄─────────────────────────────── │                           │
│  SignalR client          │      /Events hub (WebSocket,     │  SignalR EventsHub        │
│                          │      token in query string)      │                           │
└──────────────────────────┘                                  └─────────────┬─────────────┘
                                                                            │ EF Core 9
                                                                    ┌───────▼────────┐
                                                                    │   PostgreSQL   │
                                                                    └────────────────┘
```

Every player's phone is a client. State lives entirely in Postgres — there is no in-memory
game state — so a player who closes the tab, reloads, or loses signal simply refetches and
carries on.

## Layering

```
API/           Minimal API endpoints — parse the request, call a service, wrap in APIResponse,
               broadcast a SignalR event when other clients need to know
  ↓
Service/       GameService, RoomService — all orchestration and game rules
  ↓
Role/          Per-role policy: what actions is this role allowed right now?
  ↓
Repository/    EF Core queries; the only place DbContext is touched
  ↓
DbContext/     WerewolfDbContext + entity relationships
```

Everything is registered as `Scoped` in `Program.cs` except the SignalR user-id provider
(singleton). There is no interface indirection over most services — endpoints take the
concrete `RoomService` / `GameService` from DI.

### Where the rules live

Game rules are deliberately kept on the server. A client never decides whether the Witch
has used her potion or whether the Doctor may self-heal; it asks
`GET /api/game/{roomId}/{playerRoleId}/role-actions` and renders the returned
`RoleActionDto` list — label, enabled flag, disabled reason, and valid target ids.

Each `Role` subclass implements one method:

```csharp
public abstract List<RoleActionDto> GetActions(ActionCheckDto actionCheckDto);
```

`ActionCheckDto` hands the role everything it needs to decide: the current player's role
row, all processed actions, all queued actions, all players in the game, and the room's
settings. Adding a role means adding a class and a `RoleFactory` case — no endpoint or
client change.

## Request/response conventions

Every endpoint returns an `APIResponse` envelope:

```json
{ "success": true, "data": { }, "errorMessages": [] }
```

Two failure paths exist:

- **Handled failures** (validation errors, not enough players) return HTTP 200 with
  `success: false` and populated `errorMessages`.
- **Unhandled exceptions** hit `GlobalExceptionHandler`, which logs and returns HTTP 500
  with the same envelope shape and the exception message as the single error message.

The frontend's `getApi` helper treats both the same way: it throws an `Error` carrying
`errorMessages[0]`.

## Authentication

There are no accounts. Identity is a GUID carried in a JWT.

1. `POST /api/player/get-id` — if the caller already presents a valid token, a fresh token
   is issued for the **same** player GUID; otherwise a new GUID is generated. The GUID goes
   in the `ClaimTypes.NameIdentifier` claim.
2. The client stores the token in a long-lived `session` cookie and sends it as
   `Authorization: Bearer <token>` on every request.
3. `ClaimsPrincipalExtension.GetPlayerId()` pulls the GUID back out inside endpoints.

Tokens are signed HMAC-SHA256 with the symmetric `Auth:PrivateKey`. Issuer, audience and
signature are validated; **lifetime is not** (`ValidateLifetime = false`) — tokens are
issued with a one-day expiry but keep working past it, which is what lets a player rejoin a
long-running game night without re-identifying.

### Authorization

A valid token only proves the caller is *someone*. Room-scoped access is enforced on top of
it by `RoomAccessFilter` (`Filters/`), applied through two extension methods:

| Extension | Requires |
| --- | --- |
| `RequireRoomMembership()` | valid token + caller is a player in the room |
| `RequireRoomModerator()` | valid token + caller is that room's moderator |

The filter resolves the room id from either the `{roomId}` route value or a request body
implementing `IRoomScopedRequest`, then looks the caller up by their token GUID. Failures
return the standard `APIResponse` envelope with 401, 403 or 400 as appropriate — a caller
who is not in the room gets 403 whether or not the room exists.

Everything that changes the game or reveals hidden information (queueing and resolving
actions, investigating, reading other players' roles, starting/ending the game, kicking,
moderator changes, role settings) is moderator-only. Reads that a player legitimately needs
are membership-only.

Four endpoints stay token-only by necessity, because they are all reachable *before* the
caller has joined anything: `player/get-id`, `room/create-room`, `room/check-room`,
`room/{roomId}/is-player-in-room`, and `room/{roomId}/game-state` (the join screen polls
it to decide whether a game is already in progress).

`DELETE /api/game/queued-action/{actionId}` carries no room id, so the filter cannot scope
it; that endpoint resolves the action's room itself and then applies the same moderator
check via `RoomService.IsPlayerModeratorOfRoom`.

### Authentication over SignalR

Browsers cannot set headers on a WebSocket handshake, so the JWT bearer handler is
configured with an `OnMessageReceived` hook that reads the token from the `access_token`
query-string parameter — but only for paths starting with `/Events`. The client supplies it
via SignalR's `accessTokenFactory`.

## Real-time events

The hub is `EventsHub`, mapped at `/Events`, typed against `IClientEventsHub`. Clients are
grouped by uppercased room id, so every broadcast is scoped to one room.

**Client → server**

| Method | Purpose |
| --- | --- |
| `JoinRoom(AddEditPlayerDetailsDTO)` | Validates the room exists, adds/updates the player, joins the SignalR group, tells others `PlayersInLobbyUpdated`. Returns a `SocketResponse`. |

**Server → client**

| Event | Sent when |
| --- | --- |
| `PlayersInLobbyUpdated` | Someone joins, leaves, or edits their details |
| `ModeratorUpdated(PlayerDTO)` | Moderator changes (explicitly, or because the old one left) |
| `PlayerKicked(int playerRoomId)` | A player is removed by the moderator |
| `RoomRoleSettingsUpdated` | Role settings change |
| `GameState(GameState)` | Cards are dealt, or the game is ended back to lobby |
| `GameRestart` | Start-game is called on a room that was already in play |
| `DayTimeUpdated` | Night resolved or day vote completed |
| `WinConditionMet` | A faction has won |

Notifications are triggered from the REST endpoints via `IHubContext<EventsHub,
IClientEventsHub>`, not from the hub itself. The pattern is: the acting client makes a REST
call and refetches on success; everyone else is nudged by the broadcast and refetches too.
Events carry no game data (aside from `ModeratorUpdated` and `PlayerKicked`) — they are
cache-invalidation signals.

## Database schema

Postgres, EF Core code-first, snake_case table and column names via data annotations.

```
room ──────────────┬─── role_settings          (1:1)
  id (5 chars, PK) │
  current_moderator ──► player_room.id
  game_state       ├─── player_room  (1:many)  ──► player_role (1:1)
  current_night    │
  is_day           └─── room_game_action (1:many)
  win_condition
  last_modified_date
```

| Table | Entity | Notes |
| --- | --- | --- |
| `room` | `RoomEntity` | The game itself: phase counters, win condition, current moderator. |
| `player_room` | `PlayerRoomEntity` | A person in a room: their GUID, nickname (≤10 chars), avatar index, connection status. |
| `player_role` | `PlayerRoleEntity` | A dealt card: role, alive flag, and which night they were killed. Cleared and re-created on every deal. |
| `role_settings` | `RoomSettingsEntity` | Per-room configuration; `selected_roles` is stored as an array of role enum values. |
| `room_game_action` | `RoomGameActionEntity` | Every action taken: actor role id (nullable — a lynch has no actor), action type, affected role id, night number, and `Queued` vs `Processed`. |

`room_game_action` is the game's event log. Queued rows are the pending night; processed
rows are history, and they are what the end-of-game summary is built from. The whole table
for a room is wiped when a new game starts.

There is a single migration (`20250204230616_ini`); `script.sql` in the repo root is the
generated idempotent equivalent.

## Notable behaviours worth knowing

- **Room ids are case-insensitive.** Queries use `EF.Functions.ILike` and SignalR group
  names are uppercased, so `abc12` and `ABC12` are the same room.
- **Restart is destructive.** `StartGame` clears all actions and role assignments for the
  room before dealing.
- **The moderator is not a player.** They are excluded from the deal and from every
  target list.
- **`OnDisconnectedAsync` is not implemented** (it is commented out in `EventsHub`), so
  `PlayerStatus.Disconnected` is never set — a player who drops off is still listed as
  active until they explicitly leave or are kicked.
- **`GetAllRooms` returns every room in the database** and requires only a valid token.
  It is useful in development; it is not something you would want exposed publicly.
