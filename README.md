# HabitGame

A Unity-based research tool that gamifies habit formation and exports behavioral
data for analysis. This README is a reference for how the whole study
deployment (game, website, server, database) fits together.

## Live deployment

- **Main URL**: https://htionline.tue.nl/f8622112 - this is the link shared
  with participants. They enter their email, it's checked against the
  database of registered email addresses (not publicly accessible), and they're redirected
  into their assigned condition.

- **Test build**: https://htionline.tue.nl/f8622112/builds/test/ 
- **Demo build 1**: https://htionline.tue.nl/f8622112/builds/demo1 (game build for playtesters, if needed)
- **Demo build 2**: https://htionline.tue.nl/f8622112/builds/demo2
- **Demo build 3**: https://htionline.tue.nl/f8622112/builds/demo3

- **Participant admin**: https://htionline.tue.nl/f8622112/api/participant_admin.php (participant registration dashboard)
- **Data admin**: https://htionline.tue.nl/f8622112/api/data_admin.php (participant data dashboard)
- **DB admin**: https://htionline.tue.nl/f8622112/api/DBadmin.php (raw SQL manipulation)

**Manual FTP access** (e.g. via [WinSCP](https://winscp.net/eng/download.php)):
File protocol `FTP`, Encryption `TLS/SSL Explicit encryption`, Home
`/httpdocs/f8622112`. Credentials are never written down here or committed
anywhere - get them from whoever has access already, or from `HabitGame/.env` if
you already have deploy access.

## Repository layout

```
HabitGame/                     Unity project (game source)
WebServer/
  React-TS-Frontend/           The participant-facing website + PHP/MariaDB API
Json-Explorer/                 Leftover from Amber's work - outdated data exploration script tool
WebBuild/                      Gitignored local output folder for Unity WebGL builds (not deployed from here directly - see Unity game section below)
```

## study participation, end to end

1. **Registration** - the researcher registers a participant's email in
   [the participant admin dashboard](https://htionline.tue.nl/f8622112/api/participant_admin.php), which assigns them a condition.
2. **Entry** - the participant opens the study's landing page and enters their
   email at the link they receive in the email. They are then redirected to a
   URL containing a non-guessable per-condition slug - their participation is 
   isolated to the game builds for their condition and people outside the study
   cannot access the website. If they are not registered they will not get past
   the landing page.
3. **Playing** - for the 4 main conditions, the participant sees an overview
   page with Day 1/2/3 buttons; each loads that day's Unity WebGL build in an
   iframe, with the participant's email and the day number passed in via the
   URL. For the `SHORT` condition, they're sent straight to the (single)
   game page instead.
4. **Data** - when the participant is done, the game automatically attempts to 
   upload their game data to the DataBase on the server. In the event that this fails, 
   the participant can download their data and send it via email as a backup.
5. **Review** - the researcher reviews/exports/deletes collected data via
   [the data admin dashboard](https://htionline.tue.nl/f8622112/api/data_admin.php).

## Study conditions

There are 5 condition values, plus 2 kinds of non-study preview builds.

| Condition   | Days | In-game testing schedule             | Balanced? |
|-------------|------|--------------------------------------|-----------|
| `BETWEEN_L` | 3    | Tested only at the end of day 3      | Yes (auto-assigned, part of "reassign all") |
| `BETWEEN_R` | 3    | Tested only at the end of day 3      | Yes |
| `WITHIN_L`  | 3    | Tested at day 1 *and* day 3          | Yes |
| `WITHIN_R`  | 3    | Tested at day 1 *and* day 3          | Yes |
| `SHORT`     | 1    | n/a - single simplified session      | **No** - only reachable by force-assigning it in the admin dashboard; never auto-assigned or touched by "reassign all" |

`BETWEEN` vs `WITHIN` is the in-game testing schedule (handled entirely by the
Unity build, not the website); `L`/`R` is the bias condition. All of this is
hidden from participants - they only ever see a random-looking URL slug.

Condition slugs, day counts, and which slugs are "flat" builds (test/demo, no
per-day subfolder) are all defined in
[`conditions.ts`](WebServer/React-TS-Frontend/src/data/conditions.ts) - this
is the single source of truth on the website side.

**Currently, every condition/day combination needs its own separate Unity
WebGL build** - there's no single build that adapts these parameters at runtime. 
That's 4 conditions x 3 days, plus `SHORT`, plus the 3 demo
branches and `test` - 17 separate build targets in total. This is 
what the auto-deploy tooling described under **Unity game** below exists for.

## Website (React + TypeScript + Vite)

Lives in `WebServer/React-TS-Frontend/`. Static site, routed with React
Router:

- `/` - **Gateway**: email entry, looks up the participant's condition.
- `/:slug` - **ConditionOverview**: Day 1/2/3 buttons (or a single "to the
  game" button for single-day conditions).
- `/:slug/day1`, `/day2`, `/day3` - **DayPage**: loads that day's Unity WebGL
  build in an iframe, at `public/builds/<slug>/day<N>/index.html` (or
  `public/builds/<slug>/index.html` for test/demo, which are flat).

If changes are needed on
the website frontend text or anywhere else on the server deployment, a new build is needed.
In React-TS-Frontend/src/pages exist the text for the webpages. After adjusting, rebuild with vite command `npm run build` (outputs to `dist/`, gitignored); the deployed
site's base path is `/f8622112/` (see `vite.config.ts`).
The output in the `dist/` directory should then be copied to the FTP server manually.
Ensure that the .env file that only exists on the server in `/api` is not deleted during 
this process.

### PHP / MariaDB API

Lives in `public/api/` and is deployed alongside the built site. Key files:

- `env.php` - loads `public/api/.env` (DB credentials, admin password). This
  file only exists on the live server - it is *not* committed and there's no
  local copy; `example.env` is the template.
- `db.php` - shared helpers: DB connection, the condition lists
  (`CONDITIONS`, `BALANCED_CONDITIONS`), balanced auto-assignment, and
  per-condition day counts. Changing conditions almost always starts here.
- `check-email.php` - looks up a participant's condition by email (used by
  the Gateway page).
- `submit-data.php` - validates and stores one day's submitted game data.
- `participant_admin.php` - password-gated dashboard: add/remove
  participants, force a specific condition, or "reassign all" (re-randomizes
  every `BETWEEN`/`WITHIN` participant into a fresh balanced split - never
  touches `SHORT` participants).
- `data_admin.php` - password-gated dashboard: summary counts and several
  views (by day, by condition, day x condition, per-participant completion,
  recent submissions), a "download all data as .zip" link, and a "delete all
  data" danger-zone action.
- `export.php` - builds the .zip for the above.
- `sql/schema.sql` - run once against a fresh database.
- `DBadmin.php` - a generic, password-gated low-level DB admin tool (raw
  `SHOW TABLES`/custom-query/SQL-file runner against the live database).
  Intentionally provided for fine-grained manual fixes, but rarely needed -
  prefer `participant_admin.php`/`data_admin.php` for anything routine.
  Contains some legacy code and functionality from previous use. The custom
  query box, and the SQL-file runner are still useful though.

All admin dashboards share one password (`ADMIN_PASSWORD` in `.env`) and one
PHP session.

## Unity game

Lives in `HabitGame/`. Unity 6000.0.60f1, WebGL Build Support module required.

- `Assets/Scripts/Settings/Config.cs` - the participant's save data.
  `Email` is read from the page URL (`?email=...`), always present in
  Download/Submit output (falls back to `"UNKNOWN"` if it can't be read -
  Download always works even then). `Day` and `ChestSide` are set in-game
  (`ConfigManager.cs`) from the site's own `?day=` URL param and the
  assigned condition - kept deliberately independent from the URL's `day` so
  the two can be cross-checked against each other as a sanity check.
- `Assets/Editor/FTPDeployWebGL.cs` - a build post-processor that uploads
  the WebGL build to the FTP server. See its own header comment for full
  setup instructions. In short:
  - `Tools > WebGL FTP Deploy > Target` picks which condition/day (or
    Short/Demo/Test) the *next* build uploads to.
  - `Tools > WebGL FTP Deploy > Enable Auto-Deploy` is **off by default** -
    turn it on deliberately before a build you actually want uploaded, so a
    routine build never silently overwrites something live, and remember to
    turn it back off afterwards if you're going back to local-only testing.
  - Configuration (FTP host/credentials/paths, per-condition slugs) comes
    from a `.env` file in the Unity project root (`HabitGame/.env`, based on
    `.env.example`) - gitignored, never committed.

## Environment / secrets files

None of these are committed; each has a matching `*.example` template.

| File | Where | Contains |
|------|-------|----------|
| `HabitGame/.env` | Unity project root | FTP host/port/credentials, optional TLS certificate pin, per-condition FTP slugs |
| `WebServer/React-TS-Frontend/public/api/.env` | Server only (no local copy) | DB host/user/password/name/port, admin dashboard password |

If the server's TLS certificate is ever renewed, `FTP_CERT_SHA256` in
`HabitGame/.env` needs updating to match (see the comment above it) - this
server's certificate chain is missing intermediates, so the deploy script
pins to the fingerprint instead of relying on normal validation.
Certificate typically automatically renews every ~90 days.

## Misc
- **Changing the condition scheme**: start in
  `WebServer/React-TS-Frontend/src/data/conditions.ts` (slugs/day counts) and
  `public/api/db.php` (balancing), then update `schema.sql` and add a
  `migrate_*.sql` to adjust the existing DB tables, then update 
  `FTPDeployWebGL.cs`'s targets/env keys and `HabitGame/.env`.

