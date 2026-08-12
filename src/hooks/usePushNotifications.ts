import { useCallback, useEffect, useState } from "react";
import { getApi } from "@/util/api";

const SERVER = import.meta.env.WEREWOLF_SERVER_URL;

/** Web Push transports keys as base64url; PushManager wants raw bytes. */
const base64UrlToUint8Array = (base64Url: string) => {
  const padding = "=".repeat((4 - (base64Url.length % 4)) % 4);
  const base64 = (base64Url + padding).replace(/-/g, "+").replace(/_/g, "/");
  const raw = window.atob(base64);
  return Uint8Array.from([...raw].map((char) => char.charCodeAt(0)));
};

/** The subscription's keys arrive as ArrayBuffers and have to go back out as base64url. */
const arrayBufferToBase64Url = (buffer: ArrayBuffer | null) => {
  if (!buffer) return "";
  const bytes = new Uint8Array(buffer);
  let binary = "";
  bytes.forEach((byte) => (binary += String.fromCharCode(byte)));
  return window
    .btoa(binary)
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/, "");
};

export type PushStatus =
  | "unsupported"
  | "unconfigured"
  | "needs-install"
  | "prompt"
  | "denied"
  | "subscribed";

/**
 * Registers this device for turn notifications.
 *
 * Push is strictly an enhancement — the in-app prompt already tells a player it is their turn
 * whenever the app is open. This exists for the much more common case of a phone lying
 * face-down on the table.
 *
 * The iOS caveat is the awkward one: Safari only delivers Web Push to a site that has been
 * added to the home screen, so an iPhone player who just opens the link gets nothing. We detect
 * that and ask them to install rather than showing a permission prompt that cannot work.
 */
export const usePushNotifications = () => {
  const [status, setStatus] = useState<PushStatus>("unsupported");
  const [isSubscribing, setIsSubscribing] = useState(false);

  const isSupported =
    typeof window !== "undefined" &&
    "serviceWorker" in navigator &&
    "PushManager" in window &&
    "Notification" in window;

  // Standalone means "launched from the home screen". `standalone` is the iOS-only signal.
  const isStandalone =
    typeof window !== "undefined" &&
    (window.matchMedia("(display-mode: standalone)").matches ||
      (window.navigator as { standalone?: boolean }).standalone === true);

  const isIos =
    typeof navigator !== "undefined" &&
    /iphone|ipad|ipod/i.test(navigator.userAgent);

  useEffect(() => {
    let cancelled = false;

    const resolveStatus = async () => {
      if (!isSupported) {
        // iOS before installation exposes no PushManager at all, so this is where an iPhone
        // player lands until they add the app to their home screen.
        if (!cancelled) setStatus(isIos && !isStandalone ? "needs-install" : "unsupported");
        return;
      }

      const vapidKey = await getApi<string | null>({
        url: `${SERVER}/api/push/vapid-key`,
        method: "GET",
      }).catch(() => null);

      if (cancelled) return;
      if (!vapidKey) {
        setStatus("unconfigured");
        return;
      }

      if (Notification.permission === "denied") {
        setStatus("denied");
        return;
      }

      const registration = await navigator.serviceWorker.getRegistration();
      const existing = await registration?.pushManager.getSubscription();
      if (cancelled) return;

      setStatus(existing ? "subscribed" : "prompt");
    };

    void resolveStatus();
    return () => {
      cancelled = true;
    };
  }, [isSupported, isIos, isStandalone]);

  const subscribe = useCallback(async () => {
    if (!isSupported) return;
    setIsSubscribing(true);

    try {
      const vapidKey = await getApi<string | null>({
        url: `${SERVER}/api/push/vapid-key`,
        method: "GET",
      });
      if (!vapidKey) {
        setStatus("unconfigured");
        return;
      }

      const permission = await Notification.requestPermission();
      if (permission !== "granted") {
        setStatus(permission === "denied" ? "denied" : "prompt");
        return;
      }

      const registration = await navigator.serviceWorker.register(
        "/service-worker.js"
      );
      await navigator.serviceWorker.ready;

      // Reuse an existing subscription rather than creating a second one for the same device.
      const subscription =
        (await registration.pushManager.getSubscription()) ??
        (await registration.pushManager.subscribe({
          // Required by every browser: a push must always be user-visible.
          userVisibleOnly: true,
          applicationServerKey: base64UrlToUint8Array(vapidKey),
        }));

      await getApi<void>({
        url: `${SERVER}/api/push/subscribe`,
        method: "POST",
        body: JSON.stringify({
          endpoint: subscription.endpoint,
          p256dh: arrayBufferToBase64Url(subscription.getKey("p256dh")),
          auth: arrayBufferToBase64Url(subscription.getKey("auth")),
        }),
      });

      setStatus("subscribed");
    } catch {
      // A failed subscription is not worth interrupting a game over; the in-app prompt still
      // works and the player can try again from the same button.
      setStatus("prompt");
    } finally {
      setIsSubscribing(false);
    }
  }, [isSupported]);

  return { status, subscribe, isSubscribing, isIos, isStandalone };
};
