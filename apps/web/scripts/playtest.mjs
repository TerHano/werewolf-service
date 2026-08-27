/*
 * A table of bots that plays the game by itself.
 *
 * The point is to watch the app, not to play: a self-moderated game needs five people before
 * it will deal, which makes "does the new card animation look right" a five-device question.
 * This fills the room with players who take their own turns, so you can sit in the game on
 * your phone — or just watch a browser tab — and see every screen the app can show.
 *
 *   npm run playtest                 five bots, new room, plays until somebody wins
 *   npm run playtest -- --loop       ... and deals again, forever
 *   npm run playtest -- --watch-me   waits for you to join before it starts
 *   npm run playtest -- --join ABC12 adds bots to a room you already made
 *
 * The bots read the same endpoints the app does and never look at anything a player could
 * not see. They ask the server what they are allowed to do this step and pick from that, so
 * they need no knowledge of any particular role and will keep working as roles are added.
 */
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";
import path from "node:path";

// Resolved from the app's own dependencies — this script deliberately adds none of its own.
const require = createRequire(
  path.join(path.dirname(fileURLToPath(import.meta.url)), "..") + path.sep
);
const signalR = require("@microsoft/signalr");

const args = process.argv.slice(2);
const flag = (name, fallback) => {
  const i = args.indexOf(`--${name}`);
  if (i === -1) return fallback;
  const next = args[i + 1];
  return next && !next.startsWith("--") ? next : true;
};

const BASE = String(flag("url", "http://localhost:8080")).replace(/\/$/, "");
const BOT_COUNT = Number(flag("players", 5));
const JOIN_ROOM = flag("join", null);
const LOOP = flag("loop", false);
const WATCH_ME = flag("watch-me", false);
// The server holds this to 10-300; bots lock in the moment they act, so the floor only
// matters for a step whose role is dead and has to run out the clock.
const STEP_SECONDS = Math.min(300, Math.max(10, Number(flag("step-seconds", 10))));
const DAY_SECONDS = Number(flag("day-seconds", 6));
const DEAL_SECONDS = Number(flag("deal-seconds", 9));
const QUIET = flag("quiet", false);

const NAMES = [
  "Bea", "Cal", "Dot", "Eli", "Fen", "Gus", "Hana", "Ivo",
  "Jo", "Kit", "Lex", "Mo", "Nel", "Otis", "Pim", "Quill",
];

/** room/{id}/game-state: 0 Lobby, 1 CardsDealt. */
const GAME_STATE_CARDS_DEALT = 1;

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const pick = (list) => list[Math.floor(Math.random() * list.length)];
const log = (...parts) => !QUIET && console.log(...parts);

/** Every response comes back in the same envelope; unwrap it or throw the server's reason. */
async function api(token, method, endpoint, body) {
  const response = await fetch(`${BASE}/api/${endpoint}`, {
    method,
    headers: {
      Authorization: `Bearer ${token}`,
      ...(body ? { "Content-Type": "application/json" } : {}),
    },
    ...(body ? { body: JSON.stringify(body) } : {}),
  });
  const text = await response.text();
  let payload;
  try {
    payload = JSON.parse(text);
  } catch {
    throw new Error(`${method} ${endpoint} → ${response.status} ${text.slice(0, 120)}`);
  }
  if (!payload.success) {
    throw new Error(payload.errorMessages?.[0] ?? `${method} ${endpoint} failed`);
  }
  return payload.data;
}

async function newPlayer(nickname, index) {
  const response = await fetch(`${BASE}/api/player/get-id`, { method: "POST" });
  const token = (await response.json()).data;

  // Joining is a hub call, not a REST one, and the connection has to stay open: leaving the
  // room is what a dropped connection means to everyone else.
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${BASE}/Events`, { accessTokenFactory: () => token })
    .configureLogging(signalR.LogLevel.None)
    .withAutomaticReconnect()
    .build();

  return { nickname, token, connection, avatarIndex: index % 12 };
}

async function joinRoom(bot, roomId) {
  await bot.connection.start();
  const result = await bot.connection.invoke("JoinRoom", {
    roomId,
    nickName: bot.nickname,
    avatarIndex: bot.avatarIndex,
  });
  if (!result.success) throw new Error(`${bot.nickname}: ${result.errorMessage}`);
  bot.playerRoomId = (await api(bot.token, "GET", `player/${roomId}/player`)).id;
}

/**
 * One bot's turn.
 *
 * It asks for its own available actions rather than deciding from its role, so a role that
 * gains, loses or disables an ability needs no change here — a disabled action simply is not
 * offered, and a bot with nothing to do locks in and lets the night move on.
 */
async function takeTurn(bot, roomId) {
  const me = await api(bot.token, "GET", `game/${roomId}/my-role`);
  const actions = await api(
    bot.token,
    "GET",
    `game/${roomId}/${me.playerRoleId}/role-actions`
  );

  const usable = actions.filter(
    (action) => action.enabled && action.validPlayerIds?.length
  );
  if (usable.length > 0) {
    const action = pick(usable);
    const target = pick(action.validPlayerIds);
    await api(bot.token, "POST", "game/queued-action", {
      roomId,
      playerRoleId: me.playerRoleId,
      action: action.type,
      affectedPlayerRoleId: target,
    });
    log(`    ${bot.nickname}: ${action.label.toLowerCase()}`);
  } else {
    log(`    ${bot.nickname}: nothing to do`);
  }

  await api(bot.token, "POST", "game/lock-in", { roomId });
}

/** Watches for its own turn and takes it. Nobody is told whose turn it is but the actor. */
function playNights(bot, roomId) {
  let acting = false;
  let actedIn = null;
  const check = async () => {
    if (acting) return;
    try {
      const night = await api(bot.token, "GET", `game/${roomId}/night-state`);
      // currentStep is non-null only for the players acting in the running step.
      if (night.currentStep === null || night.hasLockedIn) return;

      // The push and the poll can both land before the lock-in is visible, and queueing twice
      // silently replaces the first choice. One turn per step, decided once.
      const step = `${night.currentNight}:${night.currentStep}`;
      if (actedIn === step) return;
      actedIn = step;

      acting = true;
      await takeTurn(bot, roomId);
    } catch (error) {
      if (!/not your turn/i.test(error.message)) log(`    ! ${bot.nickname}: ${error.message}`);
    } finally {
      acting = false;
    }
  };

  // The push and the poll do the same job. The poll is the one that has to be right: a bot
  // that misses its cue would stall the step until it times out.
  bot.connection.on("YourTurn", check);
  const poller = setInterval(check, 700);

  // Both have to be undone between games. An interval that is cleared but a hub handler that
  // is left attached means the next game runs with two watchers per bot — each with its own
  // idea of whether this step has been played — and the bot takes its turn twice.
  return () => {
    clearInterval(poller);
    bot.connection.off("YourTurn", check);
  };
}

async function badgeHolder(bots, roomId) {
  const moderator = await api(bots[0].token, "GET", `room/${roomId}/get-moderator`);
  return bots.find((bot) => bot.playerRoomId === moderator?.id) ?? null;
}

async function runGame(bots, roomId) {
  const host = bots[0];
  const stopWatching = bots.map((bot) => playNights(bot, roomId));

  try {
    while (true) {
      const winner = await api(host.token, "GET", `game/${roomId}/check-win-condition`);
      if (winner !== 0) {
        log(`\n  ${winner === 1 ? "Werewolves" : "Villagers"} win.\n`);
        return winner;
      }

      // Nothing to run until the cards are out — which, with --join, is somebody else's call.
      const state = await api(host.token, "GET", `room/${roomId}/game-state`);
      if (state !== GAME_STATE_CARDS_DEALT) {
        await sleep(1500);
        continue;
      }

      const runner = await badgeHolder(bots, roomId);
      const night = await api(host.token, "GET", `game/${roomId}/night-state`);

      // The badge can be held by a human — the first player to die takes it, and that may be
      // you. Then the bots simply wait: it is your day to run.
      if (!runner) {
        await sleep(1500);
        continue;
      }

      if (night.isDay) {
        await sleep(DAY_SECONDS * 1000);
        const players = await api(runner.token, "GET", `game/${roomId}/players`);
        const alive = players.filter((player) => player.isAlive);
        const lynched = Math.random() < 0.2 ? null : pick(alive);
        await api(runner.token, "POST", "game/vote-out-player", {
          roomId,
          ...(lynched ? { playerRoleId: lynched.id } : {}),
        });
        log(`  Day ${night.currentNight + 1}: ${lynched ? `lynched ${lynched.nickname}` : "abstained"}`);
      } else if (!night.isNightCallRunning) {
        // Long enough to actually watch the deal: the card takes about a second and a half to
        // arrive and turn over, and the point of looking is what happens after that.
        await sleep(DEAL_SECONDS * 1000);
        await api(runner.token, "POST", "game/start-night", { roomId });
        log(`  Night ${night.currentNight + 1}`);
      } else {
        await sleep(700);
      }
    }
  } finally {
    stopWatching.forEach((stop) => stop());
  }
}

async function main() {
  const count = Math.max(1, Math.min(BOT_COUNT, NAMES.length));
  const bots = [];
  for (let i = 0; i < count; i++) {
    bots.push(await newPlayer(NAMES[i], i));
  }
  const host = bots[0];

  const roomId = JOIN_ROOM
    ? String(JOIN_ROOM).toUpperCase()
    : await api(host.token, "POST", "room/create-room");

  for (const bot of bots) await joinRoom(bot, roomId);
  log(`\n  Room ${roomId} — ${BASE}/room/${roomId}`);
  log(`  ${bots.map((bot) => bot.nickname).join(", ")} are in.\n`);

  // Only the room's own host may change settings or deal, so a room you made yourself stays
  // yours to start: the bots just fill the seats.
  if (!JOIN_ROOM) {
    const settings = await api(host.token, "GET", `room/${roomId}/role-settings`);
    await api(host.token, "POST", "room/role-settings", {
      ...settings,
      roomId,
      selfModerated: true,
      nightStepSeconds: STEP_SECONDS,
    });

    if (WATCH_ME) {
      log(`  Join at ${BASE}/room/${roomId} — dealing in 30s.\n`);
      await sleep(30000);
    }

    do {
      // Dealing belongs to whoever holds the badge, and after a game that is the first player
      // who died rather than the bot that made the room. If it has landed on you, the bots
      // cannot deal and should not try — they wait for you to press it.
      const dealer = await badgeHolder(bots, roomId);
      if (dealer) {
        await api(dealer.token, "POST", "room/start-game", { roomId });
        log("  Cards dealt.");
      } else {
        log("  You hold the badge — press Start Game when you are ready.");
        while (
          (await api(host.token, "GET", `room/${roomId}/game-state`)) !==
          GAME_STATE_CARDS_DEALT
        ) {
          await sleep(1500);
        }
      }
      await runGame(bots, roomId);
      if (LOOP) await sleep(8000);
    } while (LOOP);
  } else {
    // Someone else deals; the bots still play their nights.
    await runGame(bots, roomId);
  }

  // Holding the connections open keeps the bots in the room, so the end-of-game screen stays
  // up to be looked at. Dropping them would empty the room out from under you.
  log("  Done. Ctrl-C to release the bots.\n");
  await new Promise(() => {});
}

const shutdown = () => process.exit(0);
process.on("SIGINT", shutdown);
process.on("SIGTERM", shutdown);

main().catch((error) => {
  console.error(`\n  ${error.message}\n`);
  process.exit(1);
});
