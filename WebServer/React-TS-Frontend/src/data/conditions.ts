// Non-guessable path segments for each study condition's game builds.
// Participants only ever learn one of these, via a redirect after entering
// a registered email on the landing page - never both, and never any of
// the "between"/"within"/"L"/"R" wording itself.
//
// All 4 main conditions now run the full 3 days. The axis that used to be
// dosage (moderate/extensive) is now the in-game testing schedule:
// BETWEEN_* participants are only tested at the end of day 3, WITHIN_*
// participants are tested on both day 1 and day 3 (both handled entirely
// in-game, not by this site) - each crossed with L/R bias, balanced
// equally across all 4.
export const CONDITION_SLUGS = {
  BETWEEN_L: 'ad27dc55c8b9bae0fea0',
  BETWEEN_R: '60daee13203a118c2dd0',
  WITHIN_L: '8b2d195f6f4428a73784',
  WITHIN_R: '346d6a2016c334eed14f',
  // Fifth, separate condition: a single simplified session for
  // participants who registered but don't want the full 3-day study.
  // Deliberately excluded from auto-balancing and "reassign all" - only
  // reachable by being force-assigned in participant_admin.php.
  SHORT: 'bdb0b53f4ed37bc478c2',
} as const

export type Condition = keyof typeof CONDITION_SLUGS

// How many day-builds each condition actually has.
const CONDITION_DAY_COUNT: Record<Condition, number> = {
  BETWEEN_L: 3,
  BETWEEN_R: 3,
  WITHIN_L: 3,
  WITHIN_R: 3,
  SHORT: 1,
}

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
