"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState, type ReactNode } from "react";
import { OfflineSyncManager } from "@/components/offline/OfflineSyncManager";
import { SilentTokenRefresh } from "@/components/auth/SilentTokenRefresh";
import { AccessibilityProvider } from "@/components/accessibility/AccessibilityProvider";

type AppProvidersProps = {
  children: ReactNode;
};

export function AppProviders({ children }: AppProvidersProps) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 60_000,
            retry: 1,
          },
          mutations: {
            retry: 0,
          },
        },
      }),
  );

  return (
    <QueryClientProvider client={queryClient}>
      <AccessibilityProvider />
      <SilentTokenRefresh />
      <OfflineSyncManager />
      {children}
    </QueryClientProvider>
  );
}
