import { APIResponse } from "@/dto/APIResponse";
import { getSessionCookie } from "./cookie";

interface apiOptions {
  url: string;
  method: "GET" | "PUT" | "POST" | "DELETE";
  body?: string;
}

export async function getApi<T>({ url, method, body }: apiOptions): Promise<T> {
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
