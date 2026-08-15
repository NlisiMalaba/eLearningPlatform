# EduZim mobile

Expo (React Native) client for EduZim on Android and iOS. Offline module progress is stored in a local SQLite `OfflineSyncQueue` table and uploaded to `POST /api/v1/sync/upload` when connectivity returns.

## Scripts

- `npm start` — Expo dev server
- `npm run android` / `npm run ios` — open a platform
- `npm test` — unit tests for the offline queue and sync mapping

Set `EXPO_PUBLIC_API_URL` (see `.env.example`) to the EduZim.API base URL.

Learning screens render video (with captions), PDF, audio (with transcript), quizzes, and 3D scenes. ZimBot is available on module screens when a student session token is set. Optionally set `EXPO_PUBLIC_MODULE_ID` to open a module immediately.
