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
 *   npm run playtest -- --watch       serves two dev pages: every phone, and every card
 *
 * The bots read the same endpoints the app does and never look at anything a player could
 * not see. They ask the server what they are allowed to do this step and pick from that, so
 * they need no knowledge of any particular role and will keep working as roles are added.
 */
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";
import http from "node:http";
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
const WATCH = flag("watch", false);
const WATCH_PORT = Number(flag("watch-port", 7777));

const NAMES = [
  "Bea", "Cal", "Dot", "Eli", "Fen", "Gus", "Hana", "Ivo",
  "Jo", "Kit", "Lex", "Mo", "Nel", "Otis", "Pim", "Quill",
];

/** room/{id}/game-state: 0 Lobby, 1 CardsDealt. */
const GAME_STATE_CARDS_DEALT = 1;

// Mirrors of the server enums. Only names for display — the values are what the API speaks.
const ROLE_NAMES = [
  "Moderator", "Werewolf", "Doctor", "Detective", "Witch",
  "Drunk", "Harlot", "Villager", "Vigilante", "Cursed",
];
const STEP_NAMES = [
  "Werewolves", "Doctor", "Detective", "Witch", "Vigilante", "Resolving",
];

/*
 * What the watch page shows.
 *
 * Assembled entirely out of what the bots are each told about themselves — every role here
 * came from that player's own `my-role`. Nothing reads another player's card, because nothing
 * can: the whole point of the opaque night is that no such endpoint exists. This is the view
 * you would get by holding everyone's phone at once, which is exactly what this process does.
 */
const table = {
  room: null,
  url: null,
  phase: "lobby",
  night: 0,
  step: null,
  winner: null,
  players: [],
  events: [],
};

const remember = (line) => {
  table.events.unshift({ at: Date.now(), line });
  table.events.length = Math.min(table.events.length, 120);
};

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const pick = (list) => list[Math.floor(Math.random() * list.length)];
const log = (...parts) => {
  remember(parts.join(" ").trim());
  if (!QUIET) console.log(...parts);
};

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
  bot.playerRoleId = me.playerRoleId;
  bot.role = me.role;
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
    bot.did = `${action.label.toLowerCase()} → ${nameOfRole(target)}`;
    log(`    ${bot.nickname}: ${action.label.toLowerCase()}`);
  } else {
    bot.did = "nothing to do";
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
      // currentStep is non-null only for the players acting in the running step — which makes
      // it both the cue to act and, for the watch page, the answer to "whose step is this".
      bot.actingStep = night.currentStep;
      if (night.currentStep === null || night.hasLockedIn) return;

      // The push and the poll can both land before the lock-in is visible, and queueing twice
      // silently replaces the first choice. One turn per step, decided once.
      const step = `${night.currentNight}:${night.currentStep}`;
      table.step = night.currentStep;
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

/** Player role ids are what actions address; the watch page wants the name on the card. */
function nameOfRole(playerRoleId) {
  const seat = table.players.find((player) => player.playerRoleId === playerRoleId);
  return seat?.nickname ?? `#${playerRoleId}`;
}

/**
 * Refresh the table from the room, plus whatever each bot knows about itself.
 *
 * A human in the room shows up in the player list like anyone else, with no role attached —
 * nothing here can see their card, and it should stay that way.
 */
async function refreshTable(bots, roomId) {
  const host = bots[0];
  const [players, moderator, winner, night] = await Promise.all([
    api(host.token, "GET", `game/${roomId}/players`),
    api(host.token, "GET", `room/${roomId}/get-moderator`),
    api(host.token, "GET", `game/${roomId}/check-win-condition`),
    api(host.token, "GET", `game/${roomId}/night-state`),
  ]);

  for (const bot of bots) {
    if (bot.role === undefined) {
      try {
        const me = await api(bot.token, "GET", `game/${roomId}/my-role`);
        bot.playerRoleId = me.playerRoleId;
        bot.role = me.role;
      } catch {
        // Not dealt in yet.
      }
    }
  }

  table.players = players.map((player) => {
    const bot = bots.find((candidate) => candidate.playerRoleId === player.id);
    return {
      playerRoleId: player.id,
      nickname: player.nickname,
      role: bot && bot.role !== undefined ? ROLE_NAMES[bot.role] : null,
      isAlive: player.isAlive,
      isBot: Boolean(bot),
      hasBadge: moderator?.id != null && bot?.playerRoomId === moderator.id,
      acting: bot?.actingStep != null && bot.actingStep === table.step,
      phoneUrl: bot?.phoneUrl ?? null,
      did: bot?.did ?? null,
    };
  });

  table.night = night.currentNight + 1;
  table.winner = winner === 0 ? null : winner === 1 ? "Werewolves" : "Villagers";
  table.phase = table.winner
    ? "over"
    : night.isDay
      ? "day"
      : night.isNightCallRunning
        ? "night"
        : "dealt";
  if (!night.isNightCallRunning) table.step = null;
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

      if (WATCH) await refreshTable(bots, roomId).catch(() => {});

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

/*
 * One proxy, one hostname per player.
 *
 * The app knows who you are from a cookie, and a cookie belongs to a *host* — famously not to
 * a port, which is the trap here: localhost:7778 and localhost:7779 share one jar, so a proxy
 * per port hands every frame the same player. Subdomains of localhost do not share, and
 * browsers resolve them to loopback without touching /etc/hosts, so each player gets
 * bea.localhost, cal.localhost, and their own session with it.
 *
 * Pinning that host's cookie to one bot's token makes the app served through it *be* that
 * player. Nothing in the app changes: same bundle, same server, believing its cookie. That is
 * why this view needs no impersonation back door in the app itself.
 */
function startPhoneProxy(bots, port) {
  const target = new URL(BASE);
  const hostFor = (bot) => `${bot.nickname.toLowerCase()}.localhost:${port}`;
  const botFor = (request) => {
    // "bea.localhost:7778" → "bea": the label in front is the whole address book.
    const label = String(request.headers.host ?? "")
      .split(":")[0]
      .split(".")[0]
      .toLowerCase();
    return bots.find((bot) => bot.nickname.toLowerCase() === label) ?? null;
  };

  const upstreamOptions = (request, bot) => ({
    host: target.hostname,
    port: target.port || 80,
    path: request.url,
    method: request.method,
    headers: {
      ...request.headers,
      host: target.host,
      cookie: `session=${bot.token}`,
      // The document gets rewritten on the way through, so it must not arrive compressed.
      "accept-encoding": "identity",
    },
  });

  const server = http.createServer((request, response) => {
    const bot = botFor(request);
    if (!bot) {
      response.writeHead(404, { "content-type": "text/plain" });
      response.end("playtest: no such player. Use <name>.localhost");
      return;
    }

    const upstream = http.request(upstreamOptions(request, bot), (upstreamResponse) => {
      const headers = { ...upstreamResponse.headers };
      // Whatever the real deployment says about framing does not apply to a local mirror.
      delete headers["x-frame-options"];
      delete headers["content-security-policy"];

      const isDocument = String(headers["content-type"] ?? "").includes("text/html");
      if (!isDocument) {
        response.writeHead(upstreamResponse.statusCode, headers);
        upstreamResponse.pipe(response);
        return;
      }

      // Identity is answered from script rather than stored, because these pages are viewed
      // inside a frame on another origin and a cross-site frame may have no cookie access at
      // all — the app would then boot with no session, mint itself a brand new player, and
      // fail to keep even that. So document.cookie is redefined for this one page: it answers
      // with this bot's token and swallows writes, which both hands the app an identity and
      // stops it replacing one. Nothing is persisted; the next load is pinned the same way.
      const chunks = [];
      upstreamResponse.on("data", (chunk) => chunks.push(chunk));
      upstreamResponse.on("end", () => {
        const pin =
          "<script>(function(){var s=" +
          JSON.stringify(`session=${bot.token}`) +
          ";try{Object.defineProperty(document,'cookie',{configurable:true," +
          "get:function(){return s},set:function(){}})}catch(e){document.cookie=s}})();</script>";
        const body = Buffer.from(
          Buffer.concat(chunks)
            .toString("utf8")
            .replace(/<head([^>]*)>/i, `<head$1>${pin}`),
          "utf8"
        );
        headers["content-length"] = String(body.byteLength);
        response.writeHead(upstreamResponse.statusCode, headers);
        response.end(body);
      });
    });
    upstream.on("error", () => {
      response.writeHead(502, { "content-type": "text/plain" });
      response.end("playtest: app not reachable");
    });
    request.pipe(upstream);
  });

  // The hub and Vite's own reload channel are both websockets, and a proxy that drops them
  // gives you a screen that never updates — the one thing this view exists to show.
  server.on("upgrade", (request, socket, head) => {
    const bot = botFor(request);
    if (!bot) return socket.destroy();

    const upstream = http.request(upstreamOptions(request, bot));
    upstream.on("upgrade", (upstreamResponse, upstreamSocket, upstreamHead) => {
      socket.write(
        "HTTP/1.1 101 Switching Protocols\r\n" +
          Object.entries(upstreamResponse.headers)
            .map(([key, value]) => `${key}: ${value}`)
            .join("\r\n") +
          "\r\n\r\n"
      );
      // Each side's leftover bytes belong to that side's stream, not the other's.
      if (upstreamHead?.length) upstreamSocket.unshift(upstreamHead);
      if (head?.length) socket.unshift(head);
      upstreamSocket.pipe(socket);
      socket.pipe(upstreamSocket);
      upstreamSocket.on("error", () => socket.destroy());
      socket.on("error", () => upstreamSocket.destroy());
    });
    upstream.on("error", () => socket.destroy());
    upstream.end();
  });

  server.on("error", (error) => log(`  Phones: ${busy(error)}`));
  server.listen(port);
  for (const bot of bots) bot.phoneUrl = `http://${hostFor(bot)}`;
}

/**
 * A port in use almost always means another playtest is still running, and the pages it is
 * already serving belong to that game — which looks like this one until you try to act.
 */
const busy = (error) =>
  error.code === "EADDRINUSE"
    ? `port ${WATCH_PORT} is taken, most likely by a playtest that is still running. Stop it, or pass --watch-port.`
    : error.message;

const WATCH_PAGE = `<!doctype html>
<html lang="en"><head><meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>Playtest — the whole table</title>
<style>
  :root { color-scheme: dark; --bg:#0b0d12; --panel:#151923; --line:#252c3a; --fg:#e7ecf5;
          --dim:#8b95a8; --live:#5b8cff; --dead:#4a5265; }
  * { box-sizing: border-box; }
  body { margin:0; padding:1.5rem; background:var(--bg); color:var(--fg);
         font:15px/1.45 ui-sans-serif, system-ui, -apple-system, sans-serif; }
  header { display:flex; align-items:baseline; gap:1rem; flex-wrap:wrap; margin-bottom:.35rem; }
  h1 { font-size:1.05rem; margin:0; font-weight:600; }
  .room { color:var(--dim); font-size:.85rem; }
  .note { color:var(--dim); font-size:.8rem; margin:0 0 1.25rem; }
  .phase { display:inline-flex; align-items:center; gap:.5rem; padding:.3rem .7rem;
           border:1px solid var(--line); border-radius:999px; font-size:.85rem; }
  .dot { width:.5rem; height:.5rem; border-radius:50%; background:var(--live); }
  .layout { display:grid; grid-template-columns:1fr 20rem; gap:1.25rem; align-items:start; }
  @media (max-width: 800px) { .layout { grid-template-columns:1fr; } }
  .seats { display:grid; grid-template-columns:repeat(auto-fill,minmax(9.5rem,1fr)); gap:.75rem; }
  .seat { border:1px solid var(--line); border-radius:.75rem; background:var(--panel);
          padding:.7rem .8rem; }
  .seat.acting { border-color:var(--live); box-shadow:0 0 0 1px var(--live); }
  .seat.dead { opacity:.45; }
  .who { display:flex; align-items:center; gap:.4rem; font-weight:600; }
  .role { font-size:.9rem; color:var(--fg); margin-top:.15rem; }
  .role.unknown { color:var(--dim); font-style:italic; }
  .did { font-size:.78rem; color:var(--dim); margin-top:.35rem; min-height:1.1em; }
  .badge { font-size:.65rem; border:1px solid var(--line); border-radius:.3rem;
           padding:.05rem .3rem; color:var(--dim); }
  .log { border:1px solid var(--line); border-radius:.75rem; background:var(--panel);
         padding:.6rem .8rem; max-height:75vh; overflow:auto; }
  .log div { font-size:.82rem; color:var(--dim); padding:.16rem 0;
             font-family:ui-monospace,SFMono-Regular,Menlo,monospace; white-space:pre-wrap; }
  .log div.head { color:var(--fg); }
</style></head>
<body>
  <header>
    <h1>The whole table</h1>
    <span class="room" id="room"></span>
    <span class="phase"><span class="dot"></span><span id="phase">…</span></span>
  </header>
  <p class="note">Every card here came from that player's own phone. A dev view — the app
     itself still cannot see any of it.</p>
  <div class="layout">
    <div class="seats" id="seats"></div>
    <div class="log" id="log"></div>
  </div>
<script>
  const seatsEl = document.getElementById("seats");
  const logEl = document.getElementById("log");
  const esc = (value) => String(value ?? "").replace(/[&<>]/g, (c) =>
    ({ "&": "&amp;", "<": "&lt;", ">": "&gt;" })[c]);

  async function tick() {
    try {
      const state = await (await fetch("/state")).json();
      document.getElementById("room").textContent = state.room ? state.room + " — " + state.url : "";
      document.getElementById("phase").textContent =
        state.winner ? state.winner + " win"
        : state.phase === "night" ? "Night " + state.night + (state.step != null ? " — " + state.stepName : "")
        : state.phase === "day" ? "Day " + state.night
        : state.phase === "dealt" ? "Cards dealt" : "Lobby";

      seatsEl.innerHTML = state.players.map((player) => \`
        <div class="seat \${player.isAlive ? "" : "dead"} \${player.acting ? "acting" : ""}">
          <div class="who">\${esc(player.nickname)}
            \${player.hasBadge ? '<span class="badge">badge</span>' : ""}
            \${player.isAlive ? "" : '<span class="badge">out</span>'}</div>
          <div class="role \${player.role ? "" : "unknown"}">\${esc(player.role ?? "not a bot")}</div>
          <div class="did">\${esc(player.did ?? "")}</div>
        </div>\`).join("");

      logEl.innerHTML = state.events.map((event) =>
        '<div class="' + (/Night|Day|dealt|win/.test(event.line) ? "head" : "") + '">' +
        esc(event.line) + "</div>").join("");
    } catch {
      document.getElementById("phase").textContent = "playtest stopped";
    }
  }
  tick();
  setInterval(tick, 600);
</script>
</body></html>`;

const PHONES_PAGE = `<!doctype html>
<html lang="en"><head><meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>Playtest — every phone</title>
<style>
  :root { color-scheme: dark; --bg:#0b0d12; --panel:#151923; --line:#252c3a; --fg:#e7ecf5;
          --dim:#8b95a8; --live:#5b8cff; }
  * { box-sizing: border-box; }
  body { margin:0; padding:1.25rem; background:var(--bg); color:var(--fg);
         font:15px/1.45 ui-sans-serif, system-ui, -apple-system, sans-serif; }
  header { display:flex; align-items:baseline; gap:1rem; flex-wrap:wrap; margin-bottom:.35rem; }
  h1 { font-size:1.05rem; margin:0; font-weight:600; }
  a { color:var(--live); font-size:.85rem; }
  .phase { display:inline-flex; align-items:center; gap:.5rem; padding:.3rem .7rem;
           border:1px solid var(--line); border-radius:999px; font-size:.85rem; }
  .dot { width:.5rem; height:.5rem; border-radius:50%; background:var(--live); }
  .note { color:var(--dim); font-size:.8rem; margin:0 0 1rem; }
  .phones { display:flex; flex-wrap:wrap; gap:1rem; }
  .phone { border:1px solid var(--line); border-radius:1rem; background:var(--panel);
           padding:.55rem; width:calc(var(--w) * 1px + 1.1rem); }
  .phone.acting { border-color:var(--live); box-shadow:0 0 0 1px var(--live); }
  .cap { display:flex; align-items:baseline; gap:.4rem; padding:.1rem .2rem .5rem; }
  .cap b { font-size:.9rem; }
  .cap span { font-size:.78rem; color:var(--dim); }
  .screen { width:calc(var(--w) * 1px); height:calc(var(--h) * 1px); border-radius:.6rem;
            overflow:hidden; background:#000; }
  iframe { width:calc(var(--w) * 1px); height:calc(var(--h) * 1px); border:0; display:block; }
</style></head>
<body style="--w:300; --h:620">
  <header>
    <h1>Every phone</h1>
    <span class="phase"><span class="dot"></span><span id="phase">…</span></span>
    <a href="/">the whole table →</a>
  </header>
  <p class="note">Each screen is the real app, signed in as that player. A dev view: the
     separate sessions come from serving each one on its own port, not from anything in the
     app.</p>
  <div class="phones" id="phones"></div>
<script>
  const phonesEl = document.getElementById("phones");
  const esc = (value) => String(value ?? "").replace(/[&<>"]/g, (c) =>
    ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" })[c]);
  let built = "";

  async function tick() {
    try {
      const state = await (await fetch("/state")).json();
      document.getElementById("phase").textContent =
        state.winner ? state.winner + " win"
        : state.phase === "night" ? "Night " + state.night + (state.step != null ? " — " + state.stepName : "")
        : state.phase === "day" ? "Day " + state.night
        : state.phase === "dealt" ? "Cards dealt" : "Lobby";

      const withPhones = state.players.filter((player) => player.phoneUrl);
      // Rebuilding the markup would reload every iframe, throwing away the very screens this
      // page exists to show. The frames are written once; only the labels are kept current.
      const signature = withPhones.map((player) => player.phoneUrl).join();
      if (signature !== built) {
        built = signature;
        phonesEl.innerHTML = withPhones.map((player) => \`
          <div class="phone" data-for="\${esc(player.phoneUrl)}">
            <div class="cap"><b>\${esc(player.nickname)}</b><span class="role"></span></div>
            <div class="screen"><iframe src="\${esc(player.phoneUrl)}/room/\${esc(state.room)}"
              title="\${esc(player.nickname)}"></iframe></div>
          </div>\`).join("");
      }

      for (const player of withPhones) {
        const card = phonesEl.querySelector('[data-for="' + player.phoneUrl + '"]');
        if (!card) continue;
        card.classList.toggle("acting", Boolean(player.acting));
        card.querySelector(".role").textContent =
          (player.role ?? "") + (player.isAlive ? "" : " · out");
      }
    } catch {
      document.getElementById("phase").textContent = "playtest stopped";
    }
  }
  tick();
  setInterval(tick, 800);
</script>
</body></html>`;

/**
 * Serves the watch pages. Deliberately part of this script and not of the app: between them
 * they show every card on the table, which is the one thing the game is built never to be
 * able to do.
 */
function startWatchServer() {
  const server = http.createServer((request, response) => {
    if (request.url === "/phones") {
      response.writeHead(200, { "content-type": "text/html; charset=utf-8" });
      response.end(PHONES_PAGE);
      return;
    }
    if (request.url === "/state") {
      const state = {
        ...table,
        stepName: table.step === null ? null : STEP_NAMES[table.step] ?? String(table.step),
        players: table.players,
      };
      response.writeHead(200, { "content-type": "application/json" });
      response.end(JSON.stringify(state));
      return;
    }
    response.writeHead(200, { "content-type": "text/html; charset=utf-8" });
    response.end(WATCH_PAGE);
  });

  server.on("error", (error) => {
    log(`  Watch page could not start: ${busy(error)}`);
  });
  server.listen(WATCH_PORT, () => {
    log(`  The whole table: http://localhost:${WATCH_PORT}`);
    log(`  Every phone:     http://localhost:${WATCH_PORT}/phones\n`);
  });
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
  table.room = roomId;
  table.url = `${BASE}/room/${roomId}`;
  if (WATCH) {
    startPhoneProxy(bots, WATCH_PORT + 1);
    startWatchServer();
  }
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
        // Last game's cards are not this game's cards.
        for (const bot of bots) {
          bot.role = undefined;
          bot.did = null;
          bot.actingStep = null;
        }
        table.winner = null;
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
