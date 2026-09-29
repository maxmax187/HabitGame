// Non-guessable path segments for each study condition's game builds.
// Participants only ever learn one of these, via a redirect after entering
// a registered email on the landing page - never both, and never any of
// the "moderate"/"extensive"/"removal"/"devaluation" wording itself.
//
// 2 x 2 between-subjects design: training length (MODERATE = 1 day,
// EXTENSIVE = 3 days) x outcome manipulation (REMOVAL vs DEVALUATION,
// handled entirely in-game, not by this site), balanced equally across
// all 4.
export const CONDITION_SLUGS = {
  MODERATE_REMOVAL: '583130b11053b121a6e1',
  MODERATE_DEVALUATION: 'd017714ca7706c3b9319',
  EXTENSIVE_REMOVAL: 'a131a02f2abd8c554cbf',
  EXTENSIVE_DEVALUATION: '49d9065b16b9ff9fe57f',
  // Fifth, separate condition: a single simplified session for
  // participants who registered but don't want the full study.
  // Deliberately excluded from auto-balancing and "reassign all" - only
  // reachable by being force-assigned in participant_admin.php.
  SHORT: 'bdb0b53f4ed37bc478c2',
} as const

export type Condition = keyof typeof CONDITION_SLUGS

// How many day-builds each condition actually has.
const CONDITION_DAY_COUNT: Record<Condition, number> = {
  MODERATE_REMOVAL: 1,
  MODERATE_DEVALUATION: 1,
  EXTENSIVE_REMOVAL: 3,
  EXTENSIVE_DEVALUATION: 3,
  SHORT: 1,
}

// Extensive days 1 and 2 are identical for removal and devaluation, so both
// conditions load them from one shared build folder; only day 3 differs.
// The participant-facing URL still uses the condition's own slug.
export const EXTENSIVE_SHARED_SLUG = '0f2d679ee812aeb0abfe'
const EXTENSIVE_SHARED_DAYS = [1, 2]
const EXTENSIVE_CONDITIONS: Condition[] = ['EXTENSIVE_REMOVAL', 'EXTENSIVE_DEVALUATION']

// Scratch testing slug - routes through the same Overview/DayPage flow as a
// real condition (so ?email=&day= get attached the same way), but points at
// the single flat public/builds/test/ folder instead of a day-numbered one.
// Not reachable via the Gateway's email check - only by navigating directly
// to /test, on purpose, so it stays clearly separate from the real
// participant flow. Defaults to showing all 3 day slots since it's a
// generic scratch build, not tied to a specific condition's day count.
export const TEST_SLUG = 'test'

// Demo branches - fixed, single-day preview builds for showing the game to
// prospective participants. Not gated by email/DB at all (Submit just
// won't find a participant row - that's fine, these are previews only) and
// not part of the balanced study conditions, same spirit as TEST_SLUG.
export const DEMO_SLUGS = ['demo1', 'demo2', 'demo3'] as const

const SLUG_TO_CONDITION = new Map<string, Condition>(
  (Object.entries(CONDITION_SLUGS) as [Condition, string][]).map(([condition, slug]) => [
    slug,
    condition,
  ]),
)

function isDemoSlug(slug: string): boolean {
  return (DEMO_SLUGS as readonly string[]).includes(slug)
}

export function isValidSlug(slug: string | undefined): slug is string {
  return !!slug && (SLUG_TO_CONDITION.has(slug) || slug === TEST_SLUG || isDemoSlug(slug))
}

export function getDayCount(slug: string): number {
  const condition = SLUG_TO_CONDITION.get(slug)
  if (condition) return CONDITION_DAY_COUNT[condition]
  if (isDemoSlug(slug)) return 1
  return 3
}

// Flat builds (public/builds/<slug>/index.html) rather than day-numbered
// subfolders - true for the test slug and the demo branches, which aren't
// real multi-day study conditions.
export function isFlatBuildSlug(slug: string): boolean {
  return slug === TEST_SLUG || isDemoSlug(slug)
}

// Path of the build's index.html, relative to the site's base URL.
export function getBuildPath(slug: string, day: number): string {
  if (isFlatBuildSlug(slug)) return `builds/${slug}/index.html`
  const condition = SLUG_TO_CONDITION.get(slug)
  const folder =
    condition && EXTENSIVE_CONDITIONS.includes(condition) && EXTENSIVE_SHARED_DAYS.includes(day)
      ? EXTENSIVE_SHARED_SLUG
      : slug
  return `builds/${folder}/day${day}/index.html`
}
