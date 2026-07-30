# Game Flow and Roles

This describes what actually happens in a game of Werewolf Party, from creating a room to a
faction winning — and what the server does at each step.

## Cast of participants

Every person in a room is a **player row** (`player_room`). Exactly one of them is the
**moderator**. When roles are dealt, the moderator is excluded: they do not get a role card
and do not die. Everyone else gets a **player role row** (`player_role`) which is what the
rest of the game refers to.

Two different ids exist and are easy to confuse:

- `playerRoomId` — identifies a person in a room (used for kicking, moderator changes).
- `playerRoleId` — identifies a dealt role in a game (used for every game action).

## 1. Creating and joining a room

1. The client calls `POST /api/player/get-id` and gets a JWT containing a randomly
   generated player GUID. That token is the player's identity for everything afterwards.
2. `POST /api/room/create-room` generates a unique 5-character room id from the alphabet
   `ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789` (no `0`, no `O` — they look alike on a phone) and
   creates default role settings for it.
3. Players open the room link, and the client calls `JoinRoom` on the SignalR hub with a
   nickname and avatar index. The hub adds the connection to a SignalR group named after
   the room, and tells everyone else `PlayersInLobbyUpdated`.
4. **The first person to join becomes the moderator** — `AddPlayerToRoom` assigns the
   moderator when the room does not already have one.

Room ids are matched case-insensitively throughout (`ILIKE` in queries, `ToUpper()` on
SignalR group names).

## 2. Configuring the game

Default settings for a new room: 1 werewolf, plus Doctor, Detective and Witch, game
summary enabled, multiple self-heals allowed.

| Setting | Effect |
| --- | --- |
| `numberOfWerewolves` | How many Werewolf cards go into the deck. |
| `selectedRoles` | Which special role cards are in the deck (one each). |
| `showGameSummary` | Whether the end-of-game replay timeline is shown to players. |
| `allowMultipleSelfHeals` | When false, the Doctor may not heal themselves two nights running. |

Any change broadcasts `RoomRoleSettingsUpdated` so every lobby refreshes.

## 3. Dealing the cards

`POST /api/room/start-game`:

1. **Player count check.** `players - 1 (moderator) >= selectedRoles.Count + numberOfWerewolves`.
   If not, the request comes back with `success: false` and "More players are needed for
   current game settings".
2. **Reset.** All game actions for the room are deleted, `currentNight` → 0, `isDay` →
   false, win condition → none, and any previous role assignments are removed. Starting a
   game is therefore also how you *re*start one.
3. **Shuffle and deal.** Everyone except the moderator is shuffled. The deck is
   `selectedRoles` plus one `Werewolf` per configured werewolf; any player left over after
   the deck runs out is a plain **Villager**.
4. Game state becomes `CardsDealt`. Clients are told `GameState` (first game) or
   `GameRestart` (a game was already dealt), and each player fetches their own card from
   `GET /api/game/{roomId}/assigned-role`.

Only the moderator can read `GET /api/game/{roomId}/all-player-roles` — the endpoint
checks the caller is the room's moderator before returning who has what.

## 4. Night and day

The room tracks two fields: `currentNight` (an integer) and `isDay` (a bool). A game runs:

```
night 0 → day 0 → night 1 → day 1 → night 2 → …
```

Each phase transition is `ProgressToNextPoint`: if it is currently day, increment the night
counter and switch to night; otherwise just switch to day.

### Night — the moderator's role call

The moderator's screen is a stepper: the werewolves first, then each role that participates
in the night call, in a fixed priority order (Werewolf → Doctor → Detective → Witch →
Vigilante), then a "night complete" card.

For each role the client asks the server which actions that role may take right now
(`GET /api/game/{roomId}/{playerRoleId}/role-actions`). The server replies with, per action,
a label, the action type, whether it is enabled, why it is disabled if it isn't, and the
list of player role ids that are valid targets. **All of the rule logic lives server-side**
in the `Role/` classes — the client just renders what it is told.

The moderator picks a target and the action is *queued*
(`POST /api/game/queued-action`), not applied. Queued actions can be changed or removed
(`DELETE /api/game/queued-action/{actionId}`) right up until the night ends, so a
misheard whisper is easy to fix. All werewolves share a single queued kill for the room.

The Detective is the exception: investigation is answered immediately by
`POST /api/game/investigate` and stores no state, because the moderator needs to give the
Detective a thumbs up or thumbs down on the spot.

### Ending the night

`POST /api/game/end-night` resolves every queued action for the room together:

- **Revive** cancels a kill on that target — whether the kill was queued before or after
  it — and makes them immune to any further kill that night.
- **Kill / WerewolfKill / Suicide** mark the target for death unless they were revived.
  A `Suicide` is an ordinary death for this purpose: a Doctor or Witch who heals the
  Vigilante on the night his guilt comes due **saves him**.
- **VigilanteKill** kills the target unless they were revived, and if the target was *not*
  a werewolf, queues a `Suicide` action against the Vigilante for the **next** night.
  Guilt is decided by *who he shot*, not by whether they survived — healing the victim
  spares the victim but not the Vigilante.

Everyone marked for death is set `isAlive = false` with `nightKilled = currentNight`, all
processed actions are stamped `Processed`, and the phase advances to day. The server then
checks the win condition and broadcasts `WinConditionMet` if one was reached, followed by
`DayTimeUpdated`.

The morning banner comes from `GET /api/game/{roomId}/latest-deaths`, which returns the
players whose `nightKilled` equals the current night.

### Day — the chopping block

The village argues in person; the app only records the outcome. The moderator either
selects a player and lynches them, or abstains — both go through
`POST /api/game/vote-out-player` (`playerRoleId` omitted means abstain).

A lynch writes a `VotedOut` action that is stored **already processed** (it is applied
immediately, not queued), kills the player, then advances the phase to the next night.
Win conditions are checked here too.

## 5. Winning

Checked after every night resolution and every day vote:

| Result | Condition |
| --- | --- |
| **Villagers win** | No werewolves are alive. |
| **Werewolves win** | Alive werewolves ≥ all other alive players. |

The win condition is written onto the room, so it survives a refresh, and
`WinConditionMet` is broadcast. Every client then renders the win screen instead of the
game view.

If `showGameSummary` is on, the win screen shows a timeline built from
`GET /api/game/{roomId}/summary` — all processed actions grouped by night, split into
night actions and the day's vote, with nights that had no actions filled in as empty.

Ending a game (`POST /api/room/end-game`) simply puts the room back into `Lobby` state so
the group can re-configure and deal again.

## Roles

Role behaviour is implemented one class per role under `Role/`, resolved by `RoleFactory`.
Each class answers a single question: *given the current game state, what may this player
do right now?*

| Role | Night action | Rules enforced by the server |
| --- | --- | --- |
| **Werewolf** | Kill any other living player | All werewolves share one queued kill per night. The action stays enabled even for a dead werewolf, since the pack acts as a group. |
| **Doctor** | Heal (revive) any living player, including themselves | With `allowMultipleSelfHeals` off, cannot self-heal if their most recent heal was on themselves. Disabled once dead. |
| **Detective** | Investigate any other living player | Answered instantly: returns whether the target reads as a werewolf. **Cursed also reads as a werewolf.** Disabled once dead. |
| **Witch** | One heal *and* one kill, each usable once per game | Each ability is disabled after it has been used ("Ability was previously used"). May heal themselves; may not kill themselves. Disabled once dead. |
| **Vigilante** | Kill any other living player | If the victim was not a werewolf, the Vigilante is queued to die of guilt the following night, and their kill is disabled that night with an explanatory reason. Guilt is triggered by shooting an innocent, even if a heal saves that victim — but the Vigilante himself can be healed out of his own suicide. |
| **Villager** | — | No abilities; votes during the day like everyone else. |
| **Drunk** | — | No app-enforced behaviour. The rule (may not speak during the day) is played out at the table; the card exists so the moderator can deal it. |
| **Cursed** | — | Plays as a villager but is reported as a werewolf by the Detective. |

`RoleName.Harlot` exists in the enum and has no implementation — `RoleFactory` throws if it
is ever dealt, so it must not be added to `selectedRoles` until a `Harlot` role class
exists.

## Moderator housekeeping

- **Change moderator** — `POST /api/room/update-moderator`; broadcasts `ModeratorUpdated`
  and `PlayersInLobbyUpdated`.
- **Kick a player** — `POST /api/room/kick-player`; broadcasts `PlayerKicked` with the
  removed player's room id.
- **Leave a room** — `POST /api/room/leave-room`. If the person leaving was the moderator,
  another player is promoted and `ModeratorUpdated` is broadcast.

### Nobody leaves a game in progress

Pulling a player out mid-game ruins the round for everyone else, so **a player holding a
dealt role can neither leave nor be kicked while the game is running**. Both endpoints
check `RoomService.CanPlayerBeRemovedFromRoom` and refuse with `success: false` and an
explanatory message; the client turns that into a toast.

The exception is a player who joined *after* the cards were dealt. They hold no role, sit
in the waiting room until the next game, and may leave freely — removing them touches no
game state. This is also why the rule is expressed as "does this player have a
`player_role`?" rather than purely as a game-state check: a player row with a role is
exactly the one whose deletion would cascade into the night history.

## Action types

`ActionType` values, as stored in `room_game_action`:

| Value | Meaning |
| --- | --- |
| `Kill` | Witch's poison |
| `WerewolfKill` | The pack's nightly kill |
| `VigilanteKill` | Vigilante's shot |
| `Revive` | Doctor heal or Witch potion |
| `Investigate` | Detective check (resolved immediately, never queued) |
| `VotedOut` | Day lynch (written pre-processed) |
| `Suicide` | Vigilante's guilt, queued for the night after a mistaken kill |
