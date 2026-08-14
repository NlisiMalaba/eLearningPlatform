# EduZim web PWA

Next.js App Router client for EduZim. Offline module content is cached by Serwist; failed progress mutations are queued in IndexedDB and uploaded to `POST /api/v1/sync/upload` on reconnect.

## Scripts

- `npm run dev` — development server (webpack; service worker disabled)
- `npm run build` / `npm start` — production PWA with service worker
- `npm test` — unit tests

Set `EDUZIM_API_URL` (see `.env.example`) so the same-origin `/api/v1/*` proxy can reach EduZim.API.
