import { Link } from 'react-router-dom'
import Header from '../components/Header'

function Overview() {
  return (
    <div className="page">
      <Header />

      <main className="overview-main">
        <h1>Welcome to the Study</h1>

        <section className="intro-card">
          <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do
            eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut
            enim ad minim veniam, quis nostrud exercitation ullamco laboris
            nisi ut aliquip ex ea commodo consequat.
          </p>
          <p>
            Duis aute irure dolor in reprehenderit in voluptate velit esse
            cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat
            cupidatat non proident, sunt in culpa qui officia deserunt
            mollit anim id est laborum.
          </p>
          <p>
            Over the next three days, you will be asked to play a short
            game once per day. Please use the buttons below on the
            corresponding day to access that day&apos;s game.
          </p>
        </section>

        <section className="day-buttons">
          <Link to="/day1" className="day-button">
            <span className="day-button-label">Day 1</span>
          </Link>
          <Link to="/day2" className="day-button">
            <span className="day-button-label">Day 2</span>
          </Link>
          <Link to="/day3" className="day-button">
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

export default Overview
