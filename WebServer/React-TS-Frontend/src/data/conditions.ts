// Non-guessable path segments for each study condition's game builds.
// Participants only ever learn one of these, via a redirect after entering
// a valid access code on the landing page - never both.
export const CONDITION_SLUGS = {
  L: 'e71408556147d1f4a022',
  R: '4e868a6f2c029521d9e4',
} as const

export type Condition = keyof typeof CONDITION_SLUGS

const VALID_SLUGS = new Set(Object.values(CONDITION_SLUGS) as string[])

export function isValidSlug(slug: string | undefined): slug is string {
  return !!slug && VALID_SLUGS.has(slug)
}
