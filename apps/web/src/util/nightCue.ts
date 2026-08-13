/**
 * The "night is over, open your eyes" cue.
 *
 * Safe to make loud and shared, because daybreak is public and reaches everyone at the same
 * instant — unlike anything about an individual step, which is why nothing else in the night
 * makes a sound. Without this the only way to know the night has ended is to look at your
 * screen, which is the one thing players are trying not to do.
 *
 * Everything here is best-effort: a browser that refuses audio or has no vibration motor still
 * plays a perfectly good game, it just relies on someone saying "wake up".
 */

let audioContext: AudioContext | null = null;

type WebkitWindow = Window & { webkitAudioContext?: typeof AudioContext };

/**
 * Browsers only allow audio to start from a user gesture, and Safari will not even let a
 * context be created outside one. Build it on the first interaction — joining a room, tapping a
 * card — so it is ready long before the first night ends.
 */
export const primeNightCue = () => {
  if (audioContext) return;

  const AudioContextClass =
    window.AudioContext ?? (window as WebkitWindow).webkitAudioContext;
  if (!AudioContextClass) return;

  try {
    audioContext = new AudioContextClass();
  } catch {
    audioContext = null;
  }
};

const playChime = () => {
  if (!audioContext) return;

  // A context created before the page was interacted with can still be suspended.
  if (audioContext.state === "suspended") void audioContext.resume();

  const now = audioContext.currentTime;

  // Two soft rising notes — meant to read as "morning", not as an alarm.
  [
    { frequency: 587.33, at: 0 },
    { frequency: 880, at: 0.18 },
  ].forEach(({ frequency, at }) => {
    const oscillator = audioContext!.createOscillator();
    const gain = audioContext!.createGain();

    oscillator.type = "sine";
    oscillator.frequency.value = frequency;

    // Shaped rather than switched: an abrupt start and stop on a sine wave clicks audibly.
    gain.gain.setValueAtTime(0, now + at);
    gain.gain.linearRampToValueAtTime(0.18, now + at + 0.03);
    gain.gain.exponentialRampToValueAtTime(0.0001, now + at + 0.45);

    oscillator.connect(gain).connect(audioContext!.destination);
    oscillator.start(now + at);
    oscillator.stop(now + at + 0.5);
  });
};

/**
 * Fire when the night resolves — and only then. A cue on any individual step would announce
 * that step's timing to the whole table, which is what the opaque night exists to prevent.
 */
export const playNightOverCue = () => {
  try {
    playChime();
  } catch {
    // An unusable audio context is not worth interrupting a game over.
  }

  // Not supported on iOS Safari at all; harmless where it is missing.
  if (typeof navigator !== "undefined" && "vibrate" in navigator) {
    try {
      navigator.vibrate([120, 80, 120]);
    } catch {
      // Some browsers throw when vibration is blocked by a permissions policy.
    }
  }
};
