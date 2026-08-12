// Service worker for turn notifications.
//
// Deliberately tiny: it does not cache anything and does not try to make the app work offline.
// A push subscription is only possible from a service worker, so its whole job is to receive a
// notification and to focus the app when it is tapped.

self.addEventListener("install", () => {
  // Take over immediately rather than waiting for every tab to close; a player who reloads
  // mid-game should not end up on a stale worker.
  self.skipWaiting();
});

self.addEventListener("activate", (event) => {
  event.waitUntil(self.clients.claim());
});

self.addEventListener("push", (event) => {
  let payload = {};
  try {
    payload = event.data ? event.data.json() : {};
  } catch {
    payload = {};
  }

  // The server sends nothing sensitive — a lock screen is readable by the person next to you,
  // so the notification says it is your turn and nothing about your role or the game state.
  const title = payload.title || "Werewolf Party";
  const body = payload.body || "It's your turn.";
  const roomId = payload.roomId;

  event.waitUntil(
    self.registration.showNotification(title, {
      body,
      icon: "/site-icon-dark.svg",
      badge: "/site-icon-dark.svg",
      // Collapse repeat turns onto one notification rather than stacking them up.
      tag: roomId ? `turn-${roomId}` : "turn",
      renotify: true,
      vibrate: [200, 100, 200],
      data: { roomId },
    })
  );
});

self.addEventListener("notificationclick", (event) => {
  event.notification.close();
  const roomId = event.notification.data && event.notification.data.roomId;
  const target = roomId ? `/room/${roomId}` : "/";

  event.waitUntil(
    self.clients
      .matchAll({ type: "window", includeUncontrolled: true })
      .then((clientList) => {
        // Prefer focusing a tab that is already open on this room over opening another one.
        for (const client of clientList) {
          if (client.url.includes(target) && "focus" in client) {
            return client.focus();
          }
        }
        if (clientList.length > 0 && "navigate" in clientList[0]) {
          return clientList[0].navigate(target).then((c) => c && c.focus());
        }
        return self.clients.openWindow(target);
      })
  );
});
