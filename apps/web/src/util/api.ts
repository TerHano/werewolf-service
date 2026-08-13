import { APIResponse } from "@/dto/APIResponse";
import { clearSessionCookie, getSessionCookie, setSessionCookie } from "./cookie";

const PLAYER_ID_URL = `${import.meta.env.WEREWOLF_SERVER_URL}/api/player/get-id`;

/**
 * Exchanges whatever token we currently hold for a fresh one.
 *
 * The server returns a token for the same player when the old one is valid, and mints a new
 * player when it is not — so this both keeps an identity alive and recovers from a token the
 * server will no longer accept.
 */
export async function refreshSessionToken(): Promise<string> {
  const token = await getApi<string>({ url: PLAYER_ID_URL, method: "POST", isRetry: true });
  setSessionCookie(token);
  return token;
}

interface apiOptions {
  url: string;
  method: "GET" | "PUT" | "POST" | "DELETE";
  body?: string;
  /** Set internally to stop a refreshed request from refreshing again. */
  isRetry?: boolean;
}

export async function getApi<T>({
  url,
  method,
  body,
  isRetry,
}: apiOptions): Promise<T> {
  const token = getSessionCookie();

  // Create AbortController for timeout
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), 10000); // 10 second timeout

  try {
    const response = await fetch(url, {
      method: method,
      headers: new Headers({
        Authorization: "Bearer " + token,
        ...(body && { "Content-Type": "application/json" }),
      }),
      body,
      signal: controller.signal,
    });

    clearTimeout(timeoutId);

    // The server sends its APIResponse envelope on failures too — a 500 from the global
    // exception handler and 401/403 from the room access filter all carry errorMessages.
    // Parse the body before looking at the status so the real reason reaches the user
    // instead of a bare "HTTP 500: Internal Server Error".
    const contentType = response.headers.get("content-type");
    const isJson = contentType?.includes("application/json") ?? false;

    let data: APIResponse<T> | null = null;
    if (isJson) {
      try {
        data = (await response.json()) as APIResponse<T>;
      } catch {
        data = null;
      }
    }

    // A 401 means the stored token is no longer accepted — it was signed with a different key,
    // or issued for a different issuer/audience, both of which happen whenever the server's auth
    // configuration changes. Nothing else clears it, so without this the browser would keep
    // sending the dead token forever and every request would fail until cookies were cleared by
    // hand. Swap it for a fresh one and retry once.
    if (response.status === 401 && !isRetry) {
      clearSessionCookie();
      await refreshSessionToken();
      return getApi<T>({ url, method, body, isRetry: true });
    }

    const serverMessage = data?.errorMessages?.[0];

    if (!response.ok) {
      throw new Error(
        serverMessage ?? `HTTP ${response.status}: ${response.statusText}`
      );
    }

    if (!isJson) {
      throw new Error("Server returned non-JSON response");
    }

    if (!data) {
      throw new Error("Server returned a malformed response");
    }

    if (!data.success) {
      throw new Error(serverMessage ?? "Server Error");
    }

    return data.data!;
  } catch (error) {
    clearTimeout(timeoutId);

    if (error instanceof Error) {
      if (error.name === "AbortError") {
        throw new Error("Request timed out after 10 seconds");
      }
      throw error;
    }

    throw new Error("Unknown error occurred");
  }
}
