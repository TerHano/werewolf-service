# Self-Moderated Night — Design

**Status:** implemented, steps 1-7. Remaining follow-ups are listed at the end.
**Goal:** everyone at the table plays, including the person who currently runs the game. The
server becomes the moderator: it walks the night role-by-role, pushes a notification to the
player whose turn it is, takes their action on their own phone, and resolves the night itself.

Decisions taken up front:

| Question | Decision |
| --- | --- |
| Who runs the night | The server. Fully automated. No human moderator during play. |
| Alerts | Web Push (works with the phone locked / tab closed). |
| Night order | Sequential by role, in the existing priority order. The night is opaque — only the acting players learn which step is running — so a step can end as soon as they lock in. |
| First to die | Takes a **social** moderator badge — narrates the table, runs the lynch. No role information, no power over the night's timing. |

See [Dead players and the moderator badge](#7-dead-players-and-the-moderator-badge) for what
the badge does and, more importantly, what it deliberately does not do.

---

## 1. What has to change conceptually

Today the moderator is load-bearing in three separate ways, and they need to be pulled apart:

1. **Lobby admin** — creates/configures the room, starts the game, kicks people. *Still needed.*
2. **Excluded from the deal** — `ShuffleAndAssignRoles` deals to everyone *except* the
   moderator, and the player-count check is `players - 1`. *Must go.*
3. **The only caller allowed to submit game actions** — every action endpoint in
   `API/GameEndpoint.cs` is `.RequireRoomModerator()`. *Must be replaced by a per-player check.*

So: rename the concept in your head from **moderator** to **host**. The host is a lobby role
only. They get dealt a card like everyone else and have no special powers once the game starts.
The DB column and the `ModeratorUpdated` event can keep their names — this is a behaviour
change, not a rename exercise.

### Back-compat

Add a room setting `selfModerated`. When `false`, the existing human-moderator flow runs
unchanged. This keeps the current `ModeratorView` working for groups who prefer a human caller,
and lets you ship without a flag day. Everything below describes the `selfModerated = true`
path.

**Implemented, defaulting to `true`** now that the engine runs. It was deliberately held at
`false` through steps 2-4: until the engine existed, a self-moderated room would have dealt the
moderator a card *and* still had them run the night call — a moderator holding a role while
seeing every card, worse than either flow on its own. The settings drawer has a toggle, so a
group who prefers a human caller can switch back.

---

## 2. Data model changes

### `room`

| Column | Type | Purpose |
| --- | --- | --- |
| `night_step` | int (nullable) | Which step of the night is live. Null when it is day or the game is in the lobby. |
| `night_step_deadline` | timestamptz (nullable) | When the current step auto-advances. |

### `room_settings`

| Column | Type | Purpose |
| --- | --- | --- |
| `self_moderated` | bool, default true | Server-run night vs. human moderator. See [Back-compat](#back-compat). |
| `night_step_seconds` | int, default 45 | Per-step timeout. |

### New table `push_subscription`

One row per device, keyed on the player GUID from the JWT — **not** on room, so a subscription
survives leaving and rejoining rooms.

| Column | Notes |
| --- | --- |
| `id` | PK |
| `player_id` | uuid, the JWT `NameIdentifier` |
| `endpoint` | text, unique |
| `p256dh` | text |
| `auth` | text |
| `created_date` | timestamptz |
| `last_seen_date` | timestamptz — bump on use; prune rows older than ~30 days |

Delete the row on a `404`/`410` from the push service; that is how browsers tell you a
subscription is dead.

### New enum `NightStep`

```
WerewolfKill = 0
DoctorHeal   = 1
DetectiveInvestigate = 2
WitchAct     = 3
VigilanteShoot = 4
Resolving    = 5
```

This mirrors the order the moderator stepper already uses (Werewolf → Doctor → Detective →
Witch → Vigilante), so the night plays out exactly as it does today.

### Investigate becomes persistent — **still to do**

Today `POST /api/game/investigate` answers on the spot and stores nothing, because a human
moderator was standing there to give a thumbs up. With the Detective reading the result off
their own phone, a refresh must not lose it. Write the `Investigate` action row (it is already
in `ActionType` and already skipped by `ProcessQueuedActions`) and add an endpoint that returns
*that Detective's own* past investigation results.

Not done yet: the Detective's verdict currently lives in component state, so a refresh during
their step loses it. Worth closing before push lands, since a push notification is precisely
an invitation to reopen the app.

`InvestigatedPlayerDTO` already correctly withholds the target's role — keep it that way.

---

## 3. The night engine

### State machine

```
CardsDealt
   ↓  StartNight
NightStep.WerewolfKill ──► DoctorHeal ──► DetectiveInvestigate ──► WitchAct ──► VigilanteShoot
   ↓ (all steps done)
NightStep.Resolving  →  ProcessQueuedActions()  →  isDay = true  →  CheckWinCondition()
   ↓
Day  →  lynch  →  CurrentNight++  →  StartNight
```

`ProcessQueuedActions` and `ProgressToNextPoint` in `Service/GameService.cs` are unchanged —
all the resolution rules (revive beats kill, Vigilante guilt, Suicide) already work and stay
exactly as documented in [game-flow.md](game-flow.md). The engine only changes *who queues the
actions and when*.

### The night is opaque, and that is what makes early exits safe

**Nobody outside a step learns anything about it.** The step name, its deadline and its position
in the order go only to the players acting in it, over the user-targeted
`YourTurn`. Everyone else gets a bare `NightAdvanced` signal and a screen that says the night is
in progress.

That opacity is load-bearing. It is the only reason a step may end early. An earlier version
broadcast the step and a countdown to the whole room, and therefore had to run every step for
its full length — because once the room can watch a specific step, a short one means somebody
acted and a full-length one means nobody could, which is to say that role is dead. Hiding the
step removes the observation, and with it the constraint.

So:

- **Every role in the deck still gets a step every night**, whether or not anyone is alive to
  act. A missing step would announce a death just as loudly.
- **A step ends as soon as every living actor has locked in** (`POST /api/game/lock-in`), or
  when its deadline passes. Locking in is allowed whether or not an action was queued — a Witch
  saving both potions still needs a way to say she is finished.
- **Empty steps take a random slice of the configured length**, between 40% and 100%, rather
  than always running to the deadline. If steps nobody could act in were reliably the longest,
  the length of the night would still count the dead.
- **Nothing else is broadcast.** `StepExtended` goes to the acting players only, for the same
  reason — they are the only ones with a countdown to correct.

The cost is atmosphere: there is no shared "the werewolves are choosing" screen any more. That
is the badge holder's job to say out loud, which is the job the badge exists to give them.

Werewolves are the one group step: all living wolves are prompted together and share the single
queued `WerewolfKill`, and the step ends once **all** of them have locked in. First submission
wins on the kill itself; any wolf may change it until the step ends.

### Who drives the clock

The deadline advances the night when nobody acts, and a lock-in advances it when everyone has.
Both funnel through the same guarded transition, so the two cannot both fire.

**Implemented** as `NightClockService`, a `BackgroundService` that wakes every second, selects
rooms where `night_step_deadline <= now()`, and advances them. Correct as long as you run
**one** server instance; if you scale out, every transition already goes through a conditional
`UPDATE ... WHERE night_step = @expected` (`RoomRepository.TryMoveToNightStep`) so only one
instance can win a race. Keep that guard regardless — a moderator extending a step also writes
`night_step_deadline` and must not collide with the clock firing on the old value.

**Two things load-bearing enough to be worth stating outright**, because both caused a runaway
loop during implementation and the code is not obviously wrong without them:

- `TryMoveToNightStep` uses `ExecuteUpdateAsync`, which writes straight past EF's change
  tracker. `GameService.ProgressToNextPoint` reads the room and then writes *every* column
  back, so a tracked copy holding the pre-transition step will resurrect it — leaving an
  expired step that the clock resolves again on its next tick, and again after that. The
  repository reloads any tracked copy after a successful write to keep the scope consistent.
- The `Resolving` step carries a **null deadline**. The work queue only picks up rooms that
  have one, so a room cannot be claimed for resolution twice while the first resolve is still
  in flight.

Effective step length is at most `night_step_seconds` plus up to one second of clock
granularity, and less whenever the actors lock in early. Since nobody outside the step can see
its length, that variation reveals nothing.

### Night 0

The first night currently runs like any other. Keep that. If you later want a "no kill on the
first night" rule, it belongs in the `Role` classes, not the engine.

---

## 4. Authorization — the part most likely to leak the game

This is the highest-risk piece of the change. Today, safety comes from *only the moderator can
call these endpoints*. That guarantee disappears, and the replacement has to be exact.

**Implemented** as `Service/GameAuthorizationService.cs` rather than the endpoint filter this
section originally proposed. The six endpoints need six genuinely different checks — read your
own role, submit as yourself during your step, withdraw your own action, investigate as the
Detective — and contorting one filter to cover them all would have hidden the differences.
Every endpoint keeps `.RequireRoomMembership()` as a blanket gate and then calls the service
explicitly, so each rule is visible at the point it applies.

Each method branches on `selfModerated` first: **moderator-run rooms keep the old rule
untouched** — the moderator may do everything, nobody else may do anything.

The checks, for a self-moderated room:

1. the caller is a member of the room (already covered by `RoomAccessFilter`);
2. the `playerRoleId` in the request belongs to **the caller's own** `player_room` row —
   a player may only ever submit as themselves;
3. the caller is alive;
4. `room.night_step` matches the caller's role's step (werewolves may act during
   `WerewolfKill`, and so on);
5. the requested `ActionType` and target are in the list that
   `Role.GetActions()` currently returns as enabled and valid for that player.

Point 5 matters: without it a client can post a `Revive` as a Villager, or a Witch can heal
twice. The server already computes exactly this list — call it and validate against it rather
than trusting the request.

Endpoint-by-endpoint:

| Endpoint | Today | Self-moderated |
| --- | --- | --- |
| `GET .../{playerRoleId}/role-actions` | moderator | **caller's own role id only** |
| `GET .../{playerRoleId}/queued-action` | moderator | **caller's own role id only** |
| `GET .../all-queued-actions` | moderator | **removed** — hands out the whole night |
| `GET .../all-player-roles` | moderator | **removed during play**; allowed only once `WinCondition != None` |
| `POST /queued-action` | moderator | `RequireActingPlayer` |
| `DELETE /queued-action/{id}` | moderator | caller must own the action *and* its step must still be live |
| `POST /investigate` | moderator | Detective only, during `DetectiveInvestigate`, result returned to caller only |
| `POST /end-night` | moderator | **removed** — the engine ends the night |
| `POST /vote-out-player` | moderator | badge holder only (see [Day phase](#6-day-phase)) |

The badge holder's own controls (`vote-out-player`, `extend-step`, `pause`) keep a
moderator-style check — but note it is now a check on a *dead* player, so
`RoomAccessFilter`'s implicit assumptions about the moderator being an ordinary participant
need re-reading. None of the three returns any role information.

Werewolves are a partial exception to rule 2: any living wolf may write or overwrite the shared
`WerewolfKill`. That is scoped explicitly to `ActionType.WerewolfKill` so it does not become a
general hole — and because the ownership check is relaxed there, the endpoint **stamps the
stored action with the caller's own role id** rather than trusting the one in the request.
Without that, a wolf could write a fellow player's id into the row and misattribute the kill in
the end-of-game summary.

`GET /api/game/{roomId}/pack` returns the werewolves to a werewolf, so the pack can recognise
each other. `PlayerRoleDTO` carries only id, nickname and role, and every row is a wolf, so
nothing leaks through it.

**Rule validation is applied to self-moderated rooms only.** A moderator-run room still trusts
its single moderator, exactly as before — that was always its security model, and adding
validation there would change a working flow for no security gain. If the moderator-run flow is
ever opened up, this is the first thing to revisit.

---

## 5. Push notifications

### Prerequisite: the frontend is not a PWA yet

`werewolf-party-vite` has no `manifest.webmanifest` and no service worker. Both are required:

- **Service worker** — mandatory for the Push API on every browser.
- **Manifest + Add to Home Screen** — mandatory on iOS. Safari only delivers Web Push to sites
  installed to the home screen (iOS 16.4+). An iPhone player who just opens the link in Safari
  **will not get a push.** Plan for an in-app "install this to get turn alerts" prompt, and keep
  the in-app SignalR prompt as the always-present fallback.

Add `vite-plugin-pwa` or hand-roll a small service worker — a hand-rolled one is maybe 30 lines
here, since the only job is `push` and `notificationclick`.

### Server side

- Generate a VAPID keypair once; store the private key in config (`appsettings` / env, next to
  the existing JWT secret), expose the public key via a small `GET /api/push/vapid-key`. A dev
  keypair is in `appsettings.Development.json`; **production must set its own** via
  `Push__PublicKey` / `Push__PrivateKey` / `Push__Subject`.
- Use **`Lib.Net.Http.WebPush`**, not the `WebPush` package this plan originally named. `WebPush`
  1.0.13 only implements the superseded `aesgcm` content encoding — verified by inspecting the
  assembly and by the `Content-Encoding: aesgcm` header it actually sent. RFC 8291 requires
  `aes128gcm`, and Safari follows the RFC, so that package cannot reach the one platform this
  feature exists for. Push is optional: with no keys configured the whole game still runs on the
  in-app prompt.
- `POST /api/push/subscribe` and `DELETE /api/push/subscribe` to manage rows.
- Send is fire-and-forget relative to the game loop: never let a slow push service delay a step
  transition. Queue it, or at minimum don't await it inside the transaction.

### Payload — say as little as possible

A push notification renders on a lock screen that the person sitting next to you can read.

```json
{ "roomId": "AB12C", "title": "Werewolf Party", "body": "It's your turn." }
```

**Never** put the role, the target, the investigation result, or who died into the push body.
The player opens the app to see any of that.

### When to send

At the start of each step, to every living player required to act at that step. Do **not** send
a second reminder before the deadline — a phone buzzing twice while everyone's eyes are closed
tells the table something.

### Fix required first: SignalR user targeting is broken — **done**

`Service/NameUserIdProvider.cs` returns `connection.User?.Identity?.Name`, but `JwtService`
only ever adds a `ClaimTypes.NameIdentifier` claim — no `Name` claim. So `Identity.Name` is
null and `Clients.User(...)` could not target anyone. Nothing depended on it, which is why it
went unnoticed, but every per-player prompt below needs it.

Fixed: the provider now reads `NameIdentifier`, matching `ClaimsPrincipalExtension.GetPlayerId()`,
and normalises to canonical lowercase. Verified against a running server with two connected
clients — the targeted send reaches exactly one of them, and reaches neither on the old code.

**Constraint for every caller from here on:** SignalR's user lookup is an ordinal string
compare, so send with `Clients.User(playerGuid.ToString())`. A hand-built or uppercased id
matches nobody and fails *silently* — no exception, no log. This is the most likely cause of a
future "the push never arrived" bug.

---

## 6. Day phase

The day is the one piece automation does not obviously improve — the village argues out loud,
and the app only records the outcome.

`POST /vote-out-player` stays as it is, gated on whoever currently holds the moderator badge:
the **host** until the first death, the **first player eliminated** thereafter (see below).
This is the badge's one recurring mechanical job.

**Follow-up:** real in-app voting — every living player submits a vote, server tallies, majority
lynches, ties abstain. This is a bigger change (new action type, new endpoints, tie rules) and
is better done once the night engine is proven.

---

## 7. Dead players and the moderator badge

Dead players stop receiving prompts and pushes. Their view becomes a spectator view showing the
same public state as everyone else (deaths, phase, day history) plus their own past actions,
and **no** live role information. Full reveal stays where it is today — the end-of-game
summary, gated on `showGameSummary`. Showing a dead player everything is a real leak if they
talk, and Werewolf tables are chatty.

### The moderator badge (first to die)

Being first out is boring, so the first player eliminated takes the moderator badge for the
rest of the game. **The badge is social, not mechanical.** Its real content is being the voice
that runs the table — "everyone close your eyes, werewolves wake up" — which keeps a dozen
people synchronised and needs no server permissions whatsoever.

Mechanically it carries exactly three controls:

| Control | Endpoint | Notes |
| --- | --- | --- |
| Run the lynch | `POST /vote-out-player` | The one recurring job, once per day phase. Now refuses outside the day. |
| Begin the night | `POST /start-night` | The table has looked at their cards and stopped arguing. |
| Extend the current night step | `POST /extend-step` | Adds `night_step_seconds` to `night_step_deadline`. For a player visibly fumbling with their phone. |

**Pause was specified and then dropped as redundant.** Nothing advances on its own outside a
night step: the day has no timer, and the night only begins when the badge holder presses
"begin". A pause flag would therefore have been a no-op with a button attached. Pausing *is*
"don't press begin yet". If a future change ever makes the day advance on a timer, this needs
revisiting.

Plus the room housekeeping the host already has: kick a player whose phone died, restart after
a win.

**The badge holder gets no role information.** Not `all-player-roles`, and not the count of who
is still to act in the current step. That last one is the subtle part: a "waiting on 0 players"
readout would tell the badge holder the step is a dummy, which tells them the role is dead.

**There is deliberately no "skip step" control**, for the same reason steps never end early
([section 3](#every-step-in-the-deck-runs-every-night--including-empty-ones)). A human able to
shorten a step makes the length of the step a tell, whether or not they intend it. Extending is
safe because it is visibly a response to a fumbling player; shortening is not.

### Handover rules

- **Night 0 has no badge holder** — nobody has died yet. The host holds it until the first
  death, so the automated engine and the lynch button must both work with no dead players.
  This feature is strictly additive; it never lets you skip any of the engine work.
- **Ties.** `ProcessQueuedActions` kills a *set* — a werewolf kill and a Witch poison resolve
  together with the same `NightKilled`. Pick the lowest `player_role.id` among that night's
  deaths. Arbitrary, deterministic, and untraceable to anything in the game state.
- **A day-0 lynch counts** as a death for this purpose, on the same footing as a night kill.
- **The badge never moves again.** Second and third deaths change nothing — otherwise the
  badge migrates all game and nobody settles into the job.
- **If the badge holder leaves or is kicked**, fall back to the next death in order, and to the
  host if there is none. Reuse the promotion path that `leave-room` already has.

Reassignment writes `room.current_moderator` and broadcasts the existing `ModeratorUpdated`
event, so most of this plumbing exists — `update-moderator`, the FK, and the auto-promotion on
leave all work already.

---

## 8. SignalR events

Additions to `Hubs/IClientEventsHub.cs`:

| Event | Target | Payload |
| --- | --- | --- |
| `NightStarted` | group | night number |
| `NightAdvanced` | group | nothing at all — just "refetch"; naming the step here is the leak |
| `YourTurn` | user | `NightStep`, deadline — the only place a step is ever named |
| `ActionAcknowledged` | user | confirms the queue write, so the UI can show "locked in" |
| `NightResolved` | group | — clients refetch `latest-deaths` |
| `StepExtended` | group | new deadline — everyone's countdown must agree |
| `GamePaused` | group | paused/resumed |

`DayTimeUpdated`, `WinConditionMet`, `ModeratorUpdated` and the rest stay as they are —
`ModeratorUpdated` is what announces the badge changing hands after the first death.

`NightAdvanced` deliberately carries no payload. An earlier version broadcast the step name and
deadline on the grounds that "everyone can see the werewolves are acting anyway" — true at a
table with a human caller, but the app made it precise and permanent, and a step that visibly
ran its full length then announced that its role was dead. Only `YourTurn` names the step, and
only to the players acting in it.

---

## 9. New and changed files

Server (`WerewolfParty-Server`):

- `Enum/NightStep.cs`
- `Service/NightEngineService.cs` — step selection, skip rules, advance, resolve
- `Service/NightClockService.cs` — `IHostedService` timeout driver
- `Service/PushService.cs` + `Repository/PushSubscriptionRepository.cs`
- `Filters/ActingPlayerFilter.cs`
- badge assignment inside night resolution + `extend-step` / `pause` endpoints
- `Entities/PushSubscriptionEntity.cs` + migration
- `API/PushEndpoint.cs`; rework `API/GameEndpoint.cs` authorization
- fix `Service/NameUserIdProvider.cs`

Frontend (`werewolf-party-vite`):

- service worker + manifest; install prompt
- `hooks/usePushSubscription.tsx`, `hooks/useNightStep.tsx`, `hooks/useSubmitAction.tsx`
- `components/GameRoom/PlayerView/NightActionPrompt.tsx` — the target picker, built from
  `role-actions`, which already returns labels, enabled flags and valid target ids
- `components/GameRoom/PlayerView/WaitingForNight.tsx` — "the werewolves are choosing…"
- `components/GameRoom/PlayerView/SpectatorView.tsx` — plus the badge holder's extend/pause/lynch
  controls, shown only to the badge holder
- `ModeratorView` stays, used only when `selfModerated` is false
- regenerate `src/types/api.d.ts` (`npm run generate-types`)

---

## 10. Suggested build order

1. ~~Fix `NameUserIdProvider`; verify `Clients.User(...)` reaches one device.~~ **Done.**
2. ~~Deal the host into the game; fix the player-count check (`players`, not `players - 1`).~~
   **Done** — behind `selfModerated`, which currently defaults to false.
3. ~~`NightStep` on the room + the engine, driven by a temporary debug endpoint, no push.~~
   **Done** — driven by real `start-night` / `night-state` endpoints rather than a debug one,
   since step 5 needs both anyway.
4. ~~`ActingPlayerFilter` and the endpoint authorization rework — **before** any client can
   submit as itself.~~ **Done**, as `GameAuthorizationService` rather than a filter.
5. ~~Player-side night prompt UI over SignalR only. At this point the game is playable end to
   end with phones face-up.~~ **Done.** `selfModerated` now defaults to true, and the settings
   drawer has a toggle to turn it off. Two endpoints the plan had not anticipated were needed:
   `my-role` (a player had no way to learn their own `player_role` id, which every action is
   addressed by) and `players` (names for target ids, now that `all-player-roles` is closed
   during play — it carries no roles).
6. ~~PWA shell + Web Push on top.~~ **Done** — manifest, service worker, VAPID, subscription
   storage, and delivery on every step change.
7. ~~Spectator view, then the moderator badge on top of it.~~ **Done.**
8. In-app day voting, if you want it.

Steps 1–5 are the actual feature. Step 6 is the buzz. Step 7 is the reward for dying first.

### Still open

- **The Detective's verdict is not persisted** (see [section 2](#investigate-becomes-persistent--still-to-do)).
  A refresh during their step loses it, which matters more now that a push notification is an
  invitation to reopen the app.
- **In-app day voting** — step 8, never started.
- Playtest `night_step_seconds`. The default 45s makes a four-role deck about three minutes of
  eyes-closed per night.

---

## 11. Open risks

- **iOS push requires home-screen install.** Some percentage of your table will never do this.
  The in-app fallback is not optional.
- **Phones face-up during the night is a new failure mode.** A player can shoulder-surf their
  neighbour. Consider a large tap-to-reveal cover over the action screen.
- **Timeouts are unforgiving.** 45s with a visible countdown is a starting point; expect to
  tune it. A player who misses their window simply takes no action that night — make sure the
  UI says so plainly rather than failing silently. The badge holder's extend control is the
  pressure valve, but it only helps if they can see that someone is struggling.
- **Night length is now bounded, not fixed.** At most `steps × night_step_seconds`, less when
  people lock in. The default is 20s per step. If it still drags, cut it further in room
  settings rather than reintroducing any shared view of the step — that view is what made early
  exits unsafe in the first place.
- **The badge is thin on purpose.** Its mechanical content is one button per day phase plus
  two rarely-used controls; its real content is narration. If playtesting shows the first-out
  player still feels sidelined, the fix is a richer spectator view, not more powers — every
  power that touches the night's timing leaks something.
- **The engine must be idempotent.** Reconnects, double-taps and a timeout racing a submission
  are all normal. Every advance should be a conditional update on the expected current step.
