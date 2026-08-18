import { BrowserRouter, Routes, Route } from 'react-router-dom'
import Overview from './pages/Overview'
import DayPage from './pages/DayPage'
import './App.css'

function App() {
  return (
    <BrowserRouter basename={import.meta.env.BASE_URL.replace(/\/$/, '')}>
      <Routes>
        <Route path="/" element={<Overview />} />
        <Route path="/day1" element={<DayPage day={1} />} />
        <Route path="/day2" element={<DayPage day={2} />} />
        <Route path="/day3" element={<DayPage day={3} />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
