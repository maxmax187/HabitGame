import { Link, useParams } from 'react-router-dom'
import Header from '../components/Header'
import InvalidLink from './InvalidLink'
import { isValidSlug } from '../data/conditions'

function ConditionOverview() {
  const { slug } = useParams()

  if (!isValidSlug(slug)) {
    return <InvalidLink />
  }

  return (
    <div className="page">
      <Header />

      <main className="overview-main">
        <h1>Your Games</h1>

        <section className="intro-card">
          <p>
            Please use the buttons below on the corresponding day to play
            that day&apos;s game. You can return to this page at any time
            using the same link.
          </p>
        </section>

        <section className="day-buttons">
          <Link to={`/${slug}/day1`} className="day-button">
            <span className="day-button-label">Day 1</span>
          </Link>
          <Link to={`/${slug}/day2`} className="day-button">
            <span className="day-button-label">Day 2</span>
          </Link>
          <Link to={`/${slug}/day3`} className="day-button">
            <span className="day-button-label">Day 3</span>
          </Link>
        </section>
      </main>

      <footer className="site-footer">
        <p>Eindhoven University of Technology</p>
      </footer>
    </div>
  )
}

export default ConditionOverview
