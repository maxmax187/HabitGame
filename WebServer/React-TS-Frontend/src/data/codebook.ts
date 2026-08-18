import { CONDITION_SLUGS, type Condition } from './conditions'

// Add one row per participant here. Each code is single-use in spirit (give
// each participant their own), and only ever resolves to their assigned
// condition - never both. Codes are matched case-insensitively.
//
// To later restrict which day a code can open, add a field here (e.g.
// `allowedDays: number[]`) and check it in ConditionOverview/DayPage.
interface ParticipantCode {
  code: string
  condition: Condition
}

const CODEBOOK: ParticipantCode[] = [
  { code: 'DEMO-L-001', condition: 'L' },
  { code: 'DEMO-R-001', condition: 'R' },
]

export function resolveCode(input: string): string | null {
  const normalized = input.trim().toUpperCase()
  const entry = CODEBOOK.find((c) => c.code.toUpperCase() === normalized)
  return entry ? CONDITION_SLUGS[entry.condition] : null
}
