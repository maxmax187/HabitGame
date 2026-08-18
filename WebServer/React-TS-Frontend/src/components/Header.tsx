import { Link } from 'react-router-dom'

interface HeaderProps {
  showBack?: boolean
}

function Header({ showBack = false }: HeaderProps) {
  return (
    <header className="site-header">
      <Link to="/" className="brand">
        <img
          src={`${import.meta.env.BASE_URL}tue_logo_icon.png`}
          alt="TU Eindhoven logo icon"
          className="brand-logo"
        />
      </Link>
      {showBack && (
        <Link to="/" className="back-link">
          &larr; Back to overview
        </Link>
      )}
    </header>
  )
}

export default Header
