import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

import "./main.css";
// Import the generated route tree
import { routeTree } from "./routeTree.gen";

import { createRouter, RouterProvider } from "@tanstack/react-router";
import { Provider } from "./components/ui-addons/provider.tsx";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

// Create a new router instance
const router = createRouter({ routeTree });

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: true,
    },
  },
});

// Register the router instance for type safety
declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <Provider>
        <RouterProvider router={router} />
      </Provider>
    </QueryClientProvider>
  </StrictMode>
);
