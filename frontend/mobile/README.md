# INK Teacher & Student mobile app

This is the Expo Go iPhone client for Institude of New Khmer. Administrator remains in `frontend/web`; this package contains only protected Teacher and Student route trees.

## Demo sign-in

| Role | Email | Password |
| --- | --- | --- |
| Teacher | `teacher@gmail.com` | `1234` |
| Student | `studnet@gmail.com` | `1234` |

The login email links to the same email field in Administrator → Management → Teachers or Students. No Administrator route or navigation exists in this mobile package.

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
src/app/(teacher)/           Teacher-only Home, Schedule, Attendance, Gradebook, Account
src/app/(student)/           Student-only Home, Schedule, Attendance, Results, Account
src/features/auth/           Local demo session and protected-route identity
src/features/portal/         Existing ASP.NET API connection and role-scoped data
```

Expo Router protected routes enforce the in-app navigation boundary. The fixed demo password is intentionally prototype-only; production authorization must also be enforced by the ASP.NET API before public release.
