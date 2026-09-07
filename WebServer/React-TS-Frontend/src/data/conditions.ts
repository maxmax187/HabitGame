// Non-guessable path segments for each study condition's game builds.
// Participants only ever learn one of these, via a redirect after entering
// a valid access code on the landing page - never both, and never any of
// the "moderate"/"extensive"/"L"/"R" wording itself.
//
// ML/MR = moderate dosage (1 day), EL/ER = extensive dosage (3 days),
// crossed with L/R bias. Participants are balanced equally across all 4.
export const CONDITION_SLUGS = {
  ML: '26fb1a7345514a4ae729',
  MR: 'ca0c3a8454c653f57ab9',
  EL: 'e71408556147d1f4a022',
  ER: '4e868a6f2c029521d9e4',
} as const

export type Condition = keyof typeof CONDITION_SLUGS

// How many day-builds each condition actually has.
const CONDITION_DAY_COUNT: Record<Condition, number> = {
  ML: 1,
  MR: 1,
  EL: 3,
  ER: 3,
}

// Scratch testing slug - routes through the same Overview/DayPage flow as a
// real condition (so ?email=&day= get attached the same way), but points at
// the single flat public/builds/test/ folder instead of a day-numbered one.
// Not reachable via the Gateway's email check - only by navigating directly
// to /test, on purpose, so it stays clearly separate from the real
// participant flow. Defaults to showing all 3 day slots since it's a
// generic scratch build, not tied to a specific condition's day count.
export const TEST_SLUG = 'test'

const SLUG_TO_CONDITION = new Map<string, Condition>(
  (Object.entries(CONDITION_SLUGS) as [Condition, string][]).map(([condition, slug]) => [
    slug,
    condition,
  ]),
)

export function isValidSlug(slug: string | undefined): slug is string {
  return !!slug && (SLUG_TO_CONDITION.has(slug) || slug === TEST_SLUG)
}

export function getDayCount(slug: string): number {
  const condition = SLUG_TO_CONDITION.get(slug)
  return condition ? CONDITION_DAY_COUNT[condition] : 3
}
