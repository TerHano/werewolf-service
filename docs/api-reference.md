# API Reference

Base URL in development: `http://localhost:5049`. Interactive Swagger UI is available at
`/swagger` when running in the Development environment.

All endpoints except `POST /api/player/get-id` require
`Authorization: Bearer <token>`. Most additionally require that the caller belongs to the
room they are acting on, and many require that they are its moderator — each endpoint below
is tagged **member** or **moderator** accordingly, and untagged endpoints need only a valid
token. See [architecture.md](architecture.md#authorization) for how this is enforced.

All responses use the envelope:

```json
{ "success": true, "errorMessages": [], "data": … }
```

`data` is described below as "Returns". Handled failures come back as HTTP 200 with
`success: false`; unhandled ones as HTTP 500 with the same shape.

Two ids appear throughout and are not interchangeable:

- **`playerRoomId`** — a person in a room (`player_room.id`).
- **`playerRoleId`** — a dealt role in a game (`player_role.id`). Every game action uses this.

---

## Player

### `POST /api/player/get-id`
*No auth required.* Issues a JWT identifying the caller. If a valid token is already
presented, the same player GUID is reissued; otherwise a new GUID is minted.

**Returns** `string` — the JWT.

### `GET /api/player/{roomId}/player` — **member**
The caller's own player record in the room.

**Returns** `PlayerDTO` `{ id, nickname, avatarIndex }`

### `POST /api/player/update-player` — **member**
Update the caller's nickname/avatar in a room. Broadcasts `PlayersInLobbyUpdated`.

**Body** `{ roomId, nickName, avatarIndex }`

---

## Room

### `POST /api/room/create-room`
Creates a room with a unique 5-character id and default role settings (1 werewolf; Doctor,
Detective, Witch; summary on; multiple self-heals allowed).

**Returns** `string` — the new room id.

### `POST /api/room/check-room`
**Body** `{ roomId }` → **Returns** `bool` — whether the room exists.

### `GET /api/room/{roomId}` — **member**
**Returns** the full `RoomEntity` (game state, current night, is-day, win condition,
moderator id).

### `GET /api/room/{roomId}/game-state`
**Returns** `GameState` — `0` Lobby, `1` CardsDealt.

### `GET /api/room/{roomId}/is-player-in-room`
**Returns** `bool` — whether the caller has already joined.

### `GET /api/room/{roomId}/players` — **member**
All players in the room **excluding the moderator**, with the caller moved to the front of
the list.

**Returns** `PlayerDTO[]`

### `GET /api/room/{roomId}/get-moderator` — **member**
**Returns** `PlayerDTO | null`

### `POST /api/room/update-moderator` — **moderator**
Assign a new moderator. Broadcasts `ModeratorUpdated` and `PlayersInLobbyUpdated`.

**Body** `{ roomId, newModeratorPlayerRoomId }`

### `POST /api/room/kick-player` — **moderator**
Remove a player. Broadcasts `PlayerKicked` with the removed id.

**Body** `{ roomId, playerRoomIdToKick }`

**Failure** `success: false` with "Players cannot be kicked while a game is in progress."
when the target holds a dealt role. Players who joined after the deal hold no role and can
still be removed.

### `POST /api/room/leave-room` — **member**
Remove the caller from the room. Broadcasts `PlayersInLobbyUpdated`, and
`ModeratorUpdated` if the departure caused a moderator change.

**Body** `{ roomId, connectionId }` — the SignalR connection id, so the leaver can be
removed from the room's group.

**Failure** `success: false` with "You cannot leave while a game is in progress." when the
caller holds a dealt role. Spectators who joined after the deal may leave at any time.

### `GET /api/room/{roomId}/role-settings` — **member**
**Returns** `RoomSettingsDto` `{ id, numberOfWerewolves, selectedRoles[], showGameSummary, allowMultipleSelfHeals }`

### `POST /api/room/role-settings` — **moderator**
Update role settings. Validated: at least one werewolf, and every selected role must be a
valid `RoleName`. Broadcasts `RoomRoleSettingsUpdated`.

**Body** `{ id, roomId, numberOfWerewolves, selectedRoles[], showGameSummary, allowMultipleSelfHeals }`

### `POST /api/room/start-game` — **moderator**
Deals the cards. Wipes any previous game for the room first, so this doubles as "play
again". Broadcasts `GameState(CardsDealt)`, or `GameRestart` if a game was already in play.

**Body** `{ roomId }`

**Failure** `success: false` with "More players are needed for current game settings" when
`players − 1 < selectedRoles.length + numberOfWerewolves`.

### `POST /api/room/end-game` — **moderator**
Returns the room to `Lobby`. Broadcasts `GameState(Lobby)`.

**Body** `{ roomId }`

### `GET /api/room/all-rooms`
Every room in the database. Development convenience — see the note in
[architecture.md](architecture.md).

---

## Game

### `GET /api/game/{roomId}/assigned-role` — **member**
The caller's own secret role.

**Returns** `RoleName | null` — null before cards are dealt.

### `GET /api/game/{roomId}/all-player-roles` — **moderator**
Every player's role plus the actions currently available to them.

**Returns** `PlayerRoleActionDto[]`
`{ id, nickname, avatarIndex, role, isAlive, actions: RoleActionDto[] }`

### `GET /api/game/{roomId}/{playerRoleId}/role-actions` — **moderator**
What this role may do right now, given everything that has already happened.

**Returns** `RoleActionDto[]`

```jsonc
{
  "label": "Heal Player",
  "type": 4,                  // ActionType
  "enabled": false,
  "disabledReason": "Ability was previously used",
  "validPlayerIds": [12, 13]  // player role ids that may be targeted
}
```

### `POST /api/game/queued-action` — **moderator**
Queue (or replace) an action for the coming night. A player has at most one queued action;
posting again overwrites it. `WerewolfKill` is keyed to the room rather than the actor, so
the whole pack shares one queued kill.

**Body** `{ roomId, playerRoleId, action, affectedPlayerRoleId }`
(`playerRoleId` may be omitted only for `WerewolfKill`.)

### `GET /api/game/{roomId}/{playerRoleId}/queued-action` — **moderator**
**Returns** `PlayerQueuedActionDTO | null`
`{ id, playerRoleId, action, affectedPlayerRoleId }`

### `GET /api/game/{roomId}/all-queued-actions` — **moderator**
All pending actions for the room. `Suicide` actions are filtered out so the moderator's
screen does not spoil the Vigilante's fate.

**Returns** `PlayerQueuedActionDTO[]`

### `DELETE /api/game/queued-action/{actionId}` — **moderator**
Remove a queued action.

### `POST /api/game/investigate` — **moderator**
The Detective's check. Resolved immediately and stored nowhere. Returns true for
**Werewolf and Cursed** alike.

**Body** `{ roomId, playerRoleId, investigationType }` (`investigationType: 1` = Werewolf)

**Returns** `{ playerRole: { id, nickname }, isInvestigationSuccessful }`

The subject's actual role is deliberately **not** returned — only the yes/no answer to the
question asked. Returning it would let any client read the whole game.

### `POST /api/game/end-night` — **moderator**
Resolves every queued action, marks the dead, advances to day, and evaluates the win
condition. Broadcasts `WinConditionMet` (if won) then `DayTimeUpdated`.

**Body** `{ roomId }`

### `POST /api/game/vote-out-player` — **moderator**
Records the day's lynch, or an abstention when `playerRoleId` is omitted. Advances to the
next night and evaluates the win condition. Broadcasts `WinConditionMet` (if won) then
`DayTimeUpdated`.

**Body** `{ roomId, playerRoleId? }`

### `GET /api/game/{roomId}/day-time` — **member**
**Returns** `{ currentNight, isDay }`

### `GET /api/game/{roomId}/latest-deaths` — **member**
Players who died in the current night — the morning announcement.

**Returns** `PlayerDTO[]`

### `GET /api/game/{roomId}/check-win-condition` — **member**
**Returns** `WinCondition` — `0` None, `1` Werewolves, `2` Villagers.

### `GET /api/game/{roomId}/summary` — **member**
The end-of-game replay: all processed actions grouped by night, split into night actions
and the day's vote. Nights with no actions are included as empty entries.

**Returns** `GameNightHistoryDTO[]`
`{ night, nightActions: PlayerGameActionDTO[], dayActions: PlayerGameActionDTO[] }`
where each action is `{ id, player, action, affectedPlayer }`.

---

## SignalR hub — `/Events`

Connect with the JWT supplied as the `access_token` query parameter (the client's
`accessTokenFactory` does this). Clients are grouped by uppercased room id.

**Invokable**

| Method | Body | Returns |
| --- | --- | --- |
| `JoinRoom` | `{ roomId, nickName, avatarIndex }` | `SocketResponse { success, errorMessage }` |

**Listenable**

| Event | Payload |
| --- | --- |
| `PlayersInLobbyUpdated` | — |
| `ModeratorUpdated` | `PlayerDTO` |
| `PlayerKicked` | `int` (player room id) |
| `RoomRoleSettingsUpdated` | — |
| `GameState` | `GameState` |
| `GameRestart` | — |
| `DayTimeUpdated` | — |
| `WinConditionMet` | — |

---

## Enums

**`RoleName`** — `0` Moderator, `1` WereWolf, `2` Doctor, `3` Detective, `4` Witch,
`5` Drunk, `6` Harlot *(no implementation — do not deal)*, `7` Villager, `8` Vigilante,
`9` Cursed

**`ActionType`** — `1` Kill, `2` WerewolfKill, `3` VigilanteKill, `4` Revive,
`5` Investigate, `6` VotedOut, `7` Suicide

**`GameState`** — `0` Lobby, `1` CardsDealt

**`WinCondition`** — `0` None, `1` Werewolves, `2` Villagers

**`ActionState`** — `0` Queued, `1` Processed

**`PlayerStatus`** — `0` Active, `1` Disconnected *(never set — see architecture.md)*

**`InvestigationType`** — `1` Werewolf
