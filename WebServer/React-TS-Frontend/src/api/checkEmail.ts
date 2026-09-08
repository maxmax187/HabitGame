import { API_BASE } from '../config'
import { CONDITION_SLUGS, type Condition } from '../data/conditions'

export interface ResolvedParticipant {
  slug: string
  email: string
}

interface CheckEmailResponse {
  found: boolean
  condition?: Condition
}

export async function checkEmail(
  input: string,
): Promise<ResolvedParticipant | null> {
  const email = input.trim().toLowerCase()
  if (!email) return null

  const response = await fetch(`${API_BASE}/check-email.php`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email }),
  })

  if (!response.ok) {
    throw new Error('check-email request failed')
  }

  const result = (await response.json()) as CheckEmailResponse
  if (!result.found || !result.condition) return null

  return { slug: CONDITION_SLUGS[result.condition], email }
}
