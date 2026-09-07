import { useParams, useSearchParams } from 'react-router-dom'
import Header from '../components/Header'
import InvalidLink from './InvalidLink'
import { isValidSlug, getDayCount, TEST_SLUG } from '../data/conditions'

interface DayPageProps {
  day: 1 | 2 | 3
}

function DayPage({ day }: DayPageProps) {
  const { slug } = useParams()
  const [searchParams] = useSearchParams()
  const email = searchParams.get('email')

  if (!isValidSlug(slug) || day > getDayCount(slug)) {
    return <InvalidLink />
  }

  const dayCount = getDayCount(slug)

  const buildQuery = email
    ? `?email=${encodeURIComponent(email)}&day=${day}`
    : `?day=${day}`
  // The test slug is a single flat build (no per-day subfolder) - Day
  // 1/2/3 all load the same build, just with a different ?day= value, so
  // Submit's URL-reading logic can be exercised for any day on one build.
  const buildPath =
    slug === TEST_SLUG ? 'builds/test/index.html' : `builds/${slug}/day${day}/index.html`
  const buildSrc = `${import.meta.env.BASE_URL}${buildPath}${buildQuery}`
  const query = searchParams.toString()
  const backTo = query ? `/${slug}?${query}` : `/${slug}`

  // Single-day (moderate) conditions don't reveal day-numbering to the
  // participant, matching the overview button reading "to the game"
  // instead of "Day 1".
  const heading = dayCount === 1 ? 'Game' : `Day ${day}`

  return (
    <div className="page day-page">
      <Header backTo={backTo} />

      <main className="day-main">
        <h1>{heading}</h1>
        <div className="game-frame-wrapper">
          <iframe
            className="game-frame"
            src={buildSrc}
            title={`${heading} - game`}
            allow="fullscreen; autoplay"
          />
        </div>
      </main>
    </div>
  )
}

export default DayPage
