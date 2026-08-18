import Header from '../components/Header'

interface DayPageProps {
  day: 1 | 2 | 3
}

function DayPage({ day }: DayPageProps) {
  const buildSrc = `${import.meta.env.BASE_URL}builds/day${day}/index.html`

  return (
    <div className="page day-page">
      <Header showBack />

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
