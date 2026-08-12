import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";
import { TanStackRouterVite } from "@tanstack/router-plugin/vite";
import tsconfigPaths from "vite-tsconfig-paths";

// https://vite.dev/config/
export default defineConfig({
  envPrefix: ["WEREWOLF"],
  server: {
    // Mirrors the nginx config used in the container: the app talks to a single origin and the
    // dev server forwards /api and the SignalR hub to the backend. WEREWOLF_SERVER_URL can then
    // stay empty everywhere, so requests are relative and nothing has a backend URL baked in.
    //
    // WEREWOLF_API_TARGET points at the backend: the api service in Docker, localhost otherwise.
    proxy: {
      "/api": {
        target: process.env.WEREWOLF_API_TARGET ?? "http://localhost:5049",
        changeOrigin: true,
      },
      "/Events": {
        target: process.env.WEREWOLF_API_TARGET ?? "http://localhost:5049",
        changeOrigin: true,
        ws: true,
      },
    },
  },
  plugins: [TanStackRouterVite(), react(), tsconfigPaths()],
  resolve: {
    alias: {
      // /esm/icons/index.mjs only exports the icons statically, so no separate chunks are created
      "@tabler/icons-react": "@tabler/icons-react/dist/esm/icons/index.mjs",
    },
  },
  build: {
    chunkSizeWarningLimit: 1000, // Set to 1000 KB (1MB)
  },
});
