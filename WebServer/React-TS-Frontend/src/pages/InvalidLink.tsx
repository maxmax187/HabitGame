import { Link } from 'react-router-dom'
import Header from '../components/Header'

function InvalidLink() {
  return (
    <div className="page">
      <Header />

      <main className="overview-main">
        <h1>Link not recognized</h1>

        <section className="intro-card">
          <p>
            This link is no longer valid or was typed incorrectly. Please
            return to the homepage and enter the access code you were
            given.
          </p>
        </section>

        <Link to="/" className="day-button" style={{ display: 'inline-flex' }}>
          <span className="day-button-label">Back to homepage</span>
        </Link>
      </main>

      <footer className="site-footer">
        <p>Eindhoven University of Technology</p>
      </footer>
    </div>
  )
}

export default InvalidLink
