# Institute of New Khmer mobile app

This is the Expo Go phone client for Institute of New Khmer. Administrator remains in `frontend/web`; this package contains only protected Teacher and Student route trees.

## Mobile sign-in

Use the exact Public ID shown on a Teacher or Student profile in Web Management. The initial mobile password for every profile is `1234`; the API validates the credentials, resolves the role, and links the matching profile automatically. No Administrator route or navigation exists in this mobile package.

## Run on an iPhone with Expo Go

1. Start the existing API from the repository root:

   ```powershell
   docker compose up -d
   ```

2. Keep the Windows computer and iPhone on the same Wi-Fi network.
3. Start Expo in LAN mode:

   ```powershell
   cd frontend/mobile
   npm start
   ```

4. Open Expo Go on the iPhone and scan the QR code.

For a browser preview, press `w` in the Expo terminal. The local API allows the Expo web development origins on ports `8081` and `8082`.

The app normally derives the computer address from Expo's LAN development host and connects to port `5080`. If the network needs an explicit address, copy `.env.example` to `.env`, replace `192.168.1.100` with the computer's IPv4 address from `ipconfig`, and fully reload Expo Go.

`EXPO_PUBLIC_API_URL` is a public client setting; never place a password, API key, or other secret in it.

## Structure

```text
src/app/sign-in.tsx          Shared Teacher/Student login
src/app/(teacher)/           Teacher-only Home, Classes, Assessment, Notifications, Profile
src/app/(student)/           Student-only Home, Classes, Results, Notifications, Profile
src/features/auth/           Stored mobile session and protected-route identity
src/features/portal/         Existing ASP.NET API connection, role-scoped data, and saved assessment drafts
```

Expo Router protected routes enforce the in-app navigation boundary. The shared initial password is intentionally prototype-only; use per-account password hashes and API authorization before public release.

## Teacher assessment drafts

Assessment keeps Attendance read-only and calculates it on the API from completed class sessions. Teachers enter Assignment, Midterm, and Final Term scores for each student. Every change is saved locally per teacher, course, and student, so unfinished work returns after the app is reopened. A teacher can submit one completed student or submit the full completed roster; local drafts are removed only after a successful submission.
