import { Link } from 'react-router-dom'

interface HeaderProps {
  backTo?: string
}

function Header({ backTo }: HeaderProps) {
  return (
    <header className="site-header">
      <span className="brand">
        <img
          src={`${import.meta.env.BASE_URL}tue_logo_icon.png`}
          alt="TU Eindhoven logo icon"
          className="brand-logo"
        />
      </span>
      {backTo && (
        <Link to={backTo} className="back-link">
          &larr; Back to overview
        </Link>
      )}
    </header>
  )
}

export default Header
