import { Link, useParams, useSearchParams } from 'react-router-dom'
import Header from '../components/Header'
import InvalidLink from './InvalidLink'
import { isValidSlug, getDayCount } from '../data/conditions'

function ConditionOverview() {
  const { slug } = useParams()
  const [searchParams] = useSearchParams()

  if (!isValidSlug(slug)) {
    return <InvalidLink />
  }

  const dayCount = getDayCount(slug)
  const query = searchParams.toString()
  const withQuery = (path: string) => (query ? `${path}?${query}` : path)

  return (
    <div className="page">
      <Header />

      <main className="overview-main">
        <h1>Your Games</h1>

        <section className="intro-card">
          {dayCount === 1 ? (
            <p>
              Please use the button below to play the game. You can return
              to this page at any time using the same link.
            </p>
          ) : (
            <p>
              Please use the buttons below on the corresponding day to play
              that day&apos;s game. You can return to this page at any time
              using the same link.
            </p>
          )}
        </section>

        <section className="day-buttons">
          {dayCount === 1 ? (
            <Link to={withQuery(`/${slug}/day1`)} className="day-button">
              <span className="day-button-label">to the game</span>
            </Link>
          ) : (
            Array.from({ length: dayCount }, (_, i) => i + 1).map((day) => (
              <Link key={day} to={withQuery(`/${slug}/day${day}`)} className="day-button">
                <span className="day-button-label">Day {day}</span>
              </Link>
            ))
          )}
        </section>
      </main>

      <footer className="site-footer">
        <p>Eindhoven University of Technology</p>
      </footer>
    </div>
  )
}

export default ConditionOverview
