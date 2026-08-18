import { BrowserRouter, Routes, Route } from 'react-router-dom'
import Gateway from './pages/Gateway'
import ConditionOverview from './pages/ConditionOverview'
import DayPage from './pages/DayPage'
import InvalidLink from './pages/InvalidLink'
import './App.css'

function App() {
  return (
    <BrowserRouter basename={import.meta.env.BASE_URL.replace(/\/$/, '')}>
      <Routes>
        <Route path="/" element={<Gateway />} />
        <Route path="/:slug" element={<ConditionOverview />} />
        <Route path="/:slug/day1" element={<DayPage day={1} />} />
        <Route path="/:slug/day2" element={<DayPage day={2} />} />
        <Route path="/:slug/day3" element={<DayPage day={3} />} />
        <Route path="*" element={<InvalidLink />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
