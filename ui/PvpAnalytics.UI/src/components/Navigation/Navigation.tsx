import { Link, useLocation } from 'react-router-dom'

const Navigation = () => {
  const location = useLocation()

  const navItems = [
    { path: '/', label: 'Stats' },
    { path: '/players', label: 'Players' },
    { path: '/matches', label: 'Matches' },
    { path: '/upload', label: 'Upload Logs' },
  ]

  return (
    <nav className="mb-8 flex gap-2 border-b border-accent-muted/30 pb-4">
      {navItems.map((item) => {
        const isActive = location.pathname === item.path
        return (
          <Link
            key={item.path}
            to={item.path}
            className={`px-4 py-2 rounded-lg text-sm font-semibold transition-colors ${
              isActive
                ? 'bg-gradient-to-r from-accent to-sky-400 text-white shadow-lg'
                : 'text-text-muted hover:text-text hover:bg-surface/50'
            }`}
          >
            {item.label}
          </Link>
        )
      })}
    </nav>
  )
}

export default Navigation

