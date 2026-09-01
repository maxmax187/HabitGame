// Non-guessable path segments for each study condition's game builds.
// Participants only ever learn one of these, via a redirect after entering
// a valid access code on the landing page - never both.
export const CONDITION_SLUGS = {
  L: 'e71408556147d1f4a022',
  R: '4e868a6f2c029521d9e4',
} as const

export type Condition = keyof typeof CONDITION_SLUGS

// Scratch testing slug - routes through the same Overview/DayPage flow as a
// real condition (so ?email=&day= get attached the same way), but points at
// the single flat public/builds/test/ folder instead of a day-numbered one.
// Not reachable via the Gateway's email check - only by navigating directly
// to /test, on purpose, so it stays clearly separate from the real
// participant flow.
export const TEST_SLUG = 'test'

const VALID_SLUGS = new Set([...Object.values(CONDITION_SLUGS), TEST_SLUG] as string[])

export function isValidSlug(slug: string | undefined): slug is string {
  return !!slug && VALID_SLUGS.has(slug)
}
