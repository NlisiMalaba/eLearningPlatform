# EduZim mobile

Expo (React Native) client for EduZim on Android and iOS. Offline module progress is stored in a local SQLite `OfflineSyncQueue` table and uploaded to `POST /api/v1/sync/upload` when connectivity returns.

## Scripts

- `npm start` — Expo dev server
- `npm run android` / `npm run ios` — open a platform
- `npm test` — unit tests for the offline queue and sync mapping

Set `EXPO_PUBLIC_API_URL` (see `.env.example`) to the EduZim.API base URL.

Learning screens render video (with captions), PDF, audio (with transcript), quizzes, and 3D scenes. ZimBot is available on module screens when a student session token is set. Optionally set `EXPO_PUBLIC_MODULE_ID` to open a module immediately.

Accessibility settings (high-contrast mode and text-to-speech in English, Shona, or Ndebele) are on every screen. Interactive controls are switch-accessible: 44pt targets, accessibility roles/labels, a visible focus ring, and hardware switch/keyboard next-prev-activate.

Pre-school (toddler) mode is opened from the home screen, or by setting `EXPO_PUBLIC_TIER=preschool`. It includes an animated character, tap-to-hear alphabet/numbers/shapes/colours/animals/body/words, a star celebration when a category is finished, a 20-minute rest prompt, a 60-second inactivity pause, and an English / Shona / Ndebele language selector.
