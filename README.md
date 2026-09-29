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
3. **Playing** - for the 3-day (`EXTENSIVE_*`) conditions, the participant
   sees an overview page with Day 1/2/3 buttons; each loads that day's Unity
   WebGL build in an iframe, with the participant's email and the day number
   passed in via the URL. For the 1-day conditions (`MODERATE_*` and
   `SHORT`), they're sent straight to the (single) game page instead.
4. **Data** - when the participant is done, the game automatically attempts to 
   upload their game data to the DataBase on the server. In the event that this fails, 
   the participant can download their data and send it via email as a backup.
5. **Review** - the researcher reviews/exports/deletes collected data via
   [the data admin dashboard](https://htionline.tue.nl/f8622112/api/data_admin.php).

## Study conditions

There are 5 condition values, plus 2 kinds of non-study preview builds.

| Condition               | Days | Outcome manipulation                | Balanced? |
|-------------------------|------|-------------------------------------|-----------|
| `MODERATE_REMOVAL`      | 1    | Removal                             | Yes (auto-assigned, part of "reassign all") |
| `MODERATE_DEVALUATION`  | 1    | Devaluation                         | Yes |
| `EXTENSIVE_REMOVAL`     | 3    | Removal (day 3)                     | Yes |
| `EXTENSIVE_DEVALUATION` | 3    | Devaluation (day 3)                 | Yes |
| `SHORT`                 | 1    | n/a - single simplified session     | **No** - only reachable by force-assigning it in the admin dashboard; never auto-assigned or touched by "reassign all" |

This is a 2 x 2 between-subjects design: training length (`MODERATE` = 1 day
vs `EXTENSIVE` = 3 days) x outcome manipulation (`REMOVAL` vs `DEVALUATION`,
handled entirely by the Unity build, not the website). There is no chest-side
(L/R) condition any more. All of this is hidden from participants - they
only ever see a random-looking URL slug.

Condition slugs, day counts, and which slugs are "flat" builds (test/demo, no
per-day subfolder) are all defined in
[`conditions.ts`](WebServer/React-TS-Frontend/src/data/conditions.ts) - this
is the single source of truth on the website side.

### Direct links (manual fallback)

The landing page, participant registration and automatic data upload are
optional. In the manual approach, none of them are used: each condition group
gets one shared link per day, and participants send in their data file
themselves. Nobody has to be registered, and nothing needs to be set up on
the server apart from the game builds.

**How it works:**
1. Split participants into the four groups yourself (e.g. a condition column
   in the form export).
2. Send each group the link(s) for its condition from the table below: one
   link for Moderate, three for Extensive (one per day).
3. At the end of each session the game tries to upload the data. Because the
   link has no email, this fails **by design**, and the game shows the red
   "ERROR SUBMITTING DATA - DO NOT CLOSE THE GAME" message. The participant
   then clicks **Download** and emails the JSON file to the researcher. Tell
   participants in the invitation that this message is expected and what to
   do, so it doesn't worry them.
4. Combine the received files with a script.

| Condition | Link (website) | Link (build only) |
|---|---|---|
| Moderate Removal | https://htionline.tue.nl/f8622112/583130b11053b121a6e1/day1 | https://htionline.tue.nl/f8622112/builds/583130b11053b121a6e1/day1/ |
| Moderate Devaluation | https://htionline.tue.nl/f8622112/d017714ca7706c3b9319/day1 | https://htionline.tue.nl/f8622112/builds/d017714ca7706c3b9319/day1/ |
| Extensive Removal, day 1 | https://htionline.tue.nl/f8622112/a131a02f2abd8c554cbf/day1 | https://htionline.tue.nl/f8622112/builds/0f2d679ee812aeb0abfe/day1/ |
| Extensive Removal, day 2 | https://htionline.tue.nl/f8622112/a131a02f2abd8c554cbf/day2 | https://htionline.tue.nl/f8622112/builds/0f2d679ee812aeb0abfe/day2/ |
| Extensive Removal, day 3 | https://htionline.tue.nl/f8622112/a131a02f2abd8c554cbf/day3 | https://htionline.tue.nl/f8622112/builds/a131a02f2abd8c554cbf/day3/ |
| Extensive Devaluation, day 1 | https://htionline.tue.nl/f8622112/49d9065b16b9ff9fe57f/day1 | https://htionline.tue.nl/f8622112/builds/0f2d679ee812aeb0abfe/day1/ |
| Extensive Devaluation, day 2 | https://htionline.tue.nl/f8622112/49d9065b16b9ff9fe57f/day2 | https://htionline.tue.nl/f8622112/builds/0f2d679ee812aeb0abfe/day2/ |
| Extensive Devaluation, day 3 | https://htionline.tue.nl/f8622112/49d9065b16b9ff9fe57f/day3 | https://htionline.tue.nl/f8622112/builds/49d9065b16b9ff9fe57f/day3/ |
| Short | https://htionline.tue.nl/f8622112/bdb0b53f4ed37bc478c2/day1 | https://htionline.tue.nl/f8622112/builds/bdb0b53f4ed37bc478c2/day1/ |

Both columns open the same game. The website link shows the page header and a
"Day N" / "Game" heading above the game, which helps participants notice they
opened the right day. The build-only link shows just the game. Extensive days
1 and 2 are one shared build, so those links are identical for Removal and
Devaluation.

**What the received files do and don't contain:**
- `Email` is `"UNKNOWN"`: the game only knows the email when it's in the
  link. Identify the participant by the address the file was sent from.
- `Day` is the day the build was made for (set in the Unity Inspector), so it
  shows which day's build was actually played.
- The condition is **not** in the file. Derive it from the group the sender
  was assigned to. For Extensive days 1 and 2 that's the only way, because
  both groups play the same build.
- Every download has the same file name, so rename files as they come in
  (e.g. `<condition>_day<N>_<email>.json`) to avoid overwriting.
- A participant who clicks the wrong day's link sends in a file whose `Day`
  repeats a day or is out of order. Check the dates of the emails.

This also covers people who should only play without taking part in the
study: send them a link and don't collect their file.

**Every condition/day combination needs its own Unity WebGL build** -
there's no single build that adapts these parameters at runtime. Extensive
days 1 and 2 are identical for Removal and Devaluation, so they're built once
and stored in a shared folder that both Extensive conditions load:

| Build                        | Folder under `builds/`         |
|------------------------------|--------------------------------|
| Moderate Removal             | `583130b11053b121a6e1/day1/`   |
| Moderate Devaluation         | `d017714ca7706c3b9319/day1/`   |
| Extensive day 1 (shared)     | `0f2d679ee812aeb0abfe/day1/`   |
| Extensive day 2 (shared)     | `0f2d679ee812aeb0abfe/day2/`   |
| Extensive day 3 Removal      | `a131a02f2abd8c554cbf/day3/`   |
| Extensive day 3 Devaluation  | `49d9065b16b9ff9fe57f/day3/`   |
| Short                        | `bdb0b53f4ed37bc478c2/day1/`   |

That's 7 study builds, plus the 3 demo branches and `test` - 11 build
targets in total. This is what the auto-deploy tooling described under
**Unity game** below exists for.

## Website (React + TypeScript + Vite)

Lives in `WebServer/React-TS-Frontend/`. Static site, routed with React
Router:

- `/` - **Gateway**: email entry, looks up the participant's condition.
- `/:slug` - **ConditionOverview**: Day 1/2/3 buttons (or a single "to the
  game" button for single-day conditions).
- `/:slug/day1`, `/day2`, `/day3` - **DayPage**: loads that day's Unity WebGL
  build in an iframe, at `public/builds/<slug>/day<N>/index.html` (or
  `public/builds/<slug>/index.html` for test/demo, which are flat). Extensive
  days 1 and 2 load from the shared folder instead (`getBuildPath` in
  `conditions.ts`).

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
  every `MODERATE`/`EXTENSIVE` participant into a fresh balanced split - never
  touches `SHORT` participants), plus a danger-zone "delete all registered
  participants" action (empties `participants`, leaves `game_data` alone).
  Participants can also be imported in bulk
  from a Microsoft Forms export (.xlsx) or a .csv/.txt file: the email
  column is detected by content (not by name), and a preview shows which
  addresses are new, already registered, duplicated or invalid before
  anything is added.
- `import_parser.php` - file-reading helpers for the bulk import (blocked
  from direct HTTP access in `api/.htaccess`).
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
  Download always works even then). `Day` is **not** read from the URL -
  it's set manually per build in the Unity Inspector (`ConfigManager`
  component, "Session Settings") before building each target: Day 1 for the
  Moderate and Short builds, Day 1/2/3 for the Extensive ones. The server's `game_data.day` column comes from the
  URL's `?day=` instead - the two are deliberately independent so a mismatch
  (e.g. a Day 2 build uploaded to the Day 3 slot) can be spotted in the data.
- `Assets/Editor/FTPDeployWebGL.cs` - a build post-processor that uploads
  the WebGL build to the FTP server. See its own header comment for full
  setup instructions. In short:
  - `Tools > WebGL FTP Deploy > Target` picks which of the 7 study builds
    (or Demo/Test) the *next* build uploads to.
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
  `public/api/db.php` (balancing, day counts), then update `schema.sql` and
  add a `migrate_*.sql` to adjust the existing DB tables, then update
  `CONDITION_LABELS` in `participant_admin.php`, the `Targets` table and header
  in `FTPDeployWebGL.cs`, the `FTP_SLUG_*` keys in `HabitGame/.env` and
  `.env.example`, and the placeholder folders in `public/builds/`.

