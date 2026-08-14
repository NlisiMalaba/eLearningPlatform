/// <reference lib="webworker" />
import { defaultCache } from "@serwist/next/worker";
import type { PrecacheEntry, RuntimeCaching, SerwistGlobalConfig } from "serwist";
import {
  CacheFirst,
  ExpirationPlugin,
  NetworkOnly,
  RangeRequestsPlugin,
  Serwist,
  StaleWhileRevalidate,
} from "serwist";

declare global {
  interface WorkerGlobalScope extends SerwistGlobalConfig {
    __SW_MANIFEST: (PrecacheEntry | string)[] | undefined;
  }
}

declare const self: ServiceWorkerGlobalScope;

const THIRTY_DAYS_SECONDS = 30 * 24 * 60 * 60;

const moduleContentCache: RuntimeCaching[] = [
  {
    matcher: ({ url }) =>
      url.pathname.startsWith("/api/v1/sync/") || url.pathname.startsWith("/api/auth/"),
    handler: new NetworkOnly(),
  },
  {
    matcher: ({ request, url }) => {
      const isModuleApi =
        url.pathname.startsWith("/api/v1/content/") ||
        url.pathname.startsWith("/api/v1/modules/");
      return request.method === "GET" && isModuleApi;
    },
    handler: new StaleWhileRevalidate({
      cacheName: "eduzim-module-api",
      plugins: [
        new ExpirationPlugin({
          maxEntries: 256,
          maxAgeSeconds: THIRTY_DAYS_SECONDS,
          maxAgeFrom: "last-used",
        }),
      ],
    }),
  },
  {
    matcher: ({ request }) =>
      request.destination === "video" || request.destination === "audio",
    handler: new CacheFirst({
      cacheName: "eduzim-module-media",
      plugins: [
        new RangeRequestsPlugin(),
        new ExpirationPlugin({
          maxEntries: 64,
          maxAgeSeconds: THIRTY_DAYS_SECONDS,
          maxAgeFrom: "last-used",
        }),
      ],
    }),
  },
  {
    matcher: /\.(?:pdf)$/i,
    handler: new CacheFirst({
      cacheName: "eduzim-module-documents",
      plugins: [
        new ExpirationPlugin({
          maxEntries: 64,
          maxAgeSeconds: THIRTY_DAYS_SECONDS,
          maxAgeFrom: "last-used",
        }),
      ],
    }),
  },
];

const serwist = new Serwist({
  precacheEntries: self.__SW_MANIFEST,
  skipWaiting: true,
  clientsClaim: true,
  navigationPreload: true,
  runtimeCaching: [...moduleContentCache, ...defaultCache],
  fallbacks: {
    entries: [
      {
        url: "/~offline",
        matcher({ request }) {
          return request.destination === "document";
        },
      },
    ],
  },
});

serwist.addEventListeners();
