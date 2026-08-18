import { useParams, useSearchParams } from 'react-router-dom'
import Header from '../components/Header'
import InvalidLink from './InvalidLink'
import { isValidSlug } from '../data/conditions'

interface DayPageProps {
  day: 1 | 2 | 3
}

function DayPage({ day }: DayPageProps) {
  const { slug } = useParams()
  const [searchParams] = useSearchParams()
  const email = searchParams.get('email')

  if (!isValidSlug(slug)) {
    return <InvalidLink />
  }

  const buildQuery = email
    ? `?email=${encodeURIComponent(email)}&day=${day}`
    : `?day=${day}`
  const buildSrc = `${import.meta.env.BASE_URL}builds/${slug}/day${day}/index.html${buildQuery}`
  const query = searchParams.toString()
  const backTo = query ? `/${slug}?${query}` : `/${slug}`

  return (
    <div className="page day-page">
      <Header backTo={backTo} />

      <main className="day-main">
        <h1>Day {day}</h1>
        <div className="game-frame-wrapper">
          <iframe
            className="game-frame"
            src={buildSrc}
            title={`Day ${day} game`}
            allow="fullscreen; autoplay"
          />
        </div>
      </main>
    </div>
  )
}

export default DayPage
