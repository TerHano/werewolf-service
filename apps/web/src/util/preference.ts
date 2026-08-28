/**
 * Small per-device preferences — the kind a player sets once and expects to stay set.
 *
 * Deliberately not on the server: these are about how someone likes to read their own screen,
 * not about the game, and they should not follow them onto somebody else's phone. Storage can
 * refuse outright in a private window, so every path falls back to the default rather than
 * failing.
 */
export function readFlag(key: string, fallback: boolean) {
  try {
    const stored = localStorage.getItem(key);
    return stored === null ? fallback : stored === "1";
  } catch {
    return fallback;
  }
}

export function writeFlag(key: string, value: boolean) {
  try {
    localStorage.setItem(key, value ? "1" : "0");
  } catch {
    // A preference that cannot be saved is not worth failing a game over.
  }
}
