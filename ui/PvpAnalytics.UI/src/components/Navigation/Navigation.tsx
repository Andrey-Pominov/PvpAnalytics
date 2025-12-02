import {Link, useLocation, useNavigate} from 'react-router-dom'
import {useState, useRef, useEffect} from 'react'
import SearchBar from '../SearchBar/SearchBar'
import axios from 'axios'

const Navigation = () => {
    const location = useLocation()
    const navigate = useNavigate()
    const [searchLoading, setSearchLoading] = useState(false)
    const abortControllerRef = useRef<AbortController | null>(null)
    const searchTokenRef = useRef(0)

    // Cleanup on unmount
    useEffect(() => {
        return () => {
            if (abortControllerRef.current) {
                abortControllerRef.current.abort()
                abortControllerRef.current = null
            }
        }
    }, [])

    const navItems = [
        {path: '/', label: 'Stats', icon: '📊'},
        {path: '/players', label: 'Players', icon: '👥'},
        {path: '/matches', label: 'Matches', icon: '⚔️'},
        {path: '/teams', label: 'Teams', icon: '👫'},
        {path: '/leaderboards', label: 'Leaderboards', icon: '🏆'},
        {path: '/highlights', label: 'Highlights', icon: '⭐'},
        {path: '/discover', label: 'Discover', icon: '🔍'},
        {path: '/favorites', label: 'Favorites', icon: '⭐'},
        {path: '/rivals', label: 'Rivals', icon: '⚡'},
        {path: '/profile', label: 'Profile', icon: '👤'},
        {path: '/upload', label: 'Upload', icon: '📤'},
    ]

    // Handle search: distinguish between player ID and match ID
    // Numeric IDs are checked as match first, then player; non-numeric searches go to players page
    const handleSearch = async (term: string) => {
        const trimmedTerm = term.trim()

        if (!trimmedTerm) {
            handleEmptySearch()
            return
        }

        const abortController = initializeSearch()
        const currentToken = searchTokenRef.current

        if (isNumericSearch(trimmedTerm)) {
            await handleNumericSearch(trimmedTerm, abortController, currentToken)
        } else {
            navigateToPlayersSearch(trimmedTerm)
        }
    }

    const handleEmptySearch = () => {
        if (location.pathname === '/players') {
            navigate('/players')
        }
    }

    const initializeSearch = (): AbortController => {
        if (abortControllerRef.current) {
            abortControllerRef.current.abort()
        }

        const abortController = new AbortController()
        abortControllerRef.current = abortController
        searchTokenRef.current++
        return abortController
    }

    const isNumericSearch = (term: string): boolean => {
        return /^\d+$/.test(term)
    }

    const isSearchAborted = (abortController: AbortController, token: number): boolean => {
        return abortController.signal.aborted || token !== searchTokenRef.current
    }

    const navigateToPlayersSearch = (term: string) => {
        navigate(`/players?search=${encodeURIComponent(term)}`)
    }

    const handleNumericSearch = async (
        term: string,
        abortController: AbortController,
        token: number
    ) => {
        setSearchLoading(true)

        try {
            const matchFound = await tryFindMatch(term, abortController, token)
            if (matchFound) return

            const playerFound = await tryFindPlayer(term, abortController, token)
            if (playerFound) return

            if (!isSearchAborted(abortController, token)) {
                navigateToPlayersSearch(term)
            }
        } catch (error) {
            if (!isSearchAborted(abortController, token)) {
                console.error('Search error:', error)
                navigateToPlayersSearch(term)
            }
        } finally {
            if (!isSearchAborted(abortController, token)) {
                setSearchLoading(false)
            }
        }
    }

    const tryFindMatch = async (
        term: string,
        abortController: AbortController,
        token: number
    ): Promise<boolean> => {
        try {
            const baseUrl = getBaseUrl()
            const {data: match} = await axios.get<{ id: number }>(`${baseUrl}/matches/${term}`, {
                validateStatus: (status) => status === 200 || status === 404,
                signal: abortController.signal,
            })

            if (isSearchAborted(abortController, token)) {
                return false
            }

            if (match?.id) {
                navigate(`/matches/${term}`)
                return true
            }
        } catch (error) {
            console.error('Search error:', error)
        }

        return false
    }

    const tryFindPlayer = async (
        term: string,
        abortController: AbortController,
        token: number
    ): Promise<boolean> => {
        try {
            const baseUrl = getBaseUrl()
            const {data: player} = await axios.get<{ id: number }>(`${baseUrl}/players/${term}`, {
                validateStatus: (status) => status === 200 || status === 404,
                signal: abortController.signal,
            })

            if (isSearchAborted(abortController, token)) {
                return false
            }

            if (player?.id) {
                navigate(`/players/${term}`)
                return true
            }
        } catch (error) {
            if (!isSearchAborted(abortController, token)) {
                console.debug('Player lookup failed:', error)
            }
        }

        return false
    }

    const getBaseUrl = (): string => {
        return import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
    }

    return (
        <nav
            className="sticky top-0 z-50 mb-8 flex flex-col gap-4 border-b border-accent-muted/30 bg-background/95 backdrop-blur-sm pb-4 pt-4">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                <div className="flex flex-wrap items-center gap-2">
                    {navItems.map((item) => {
                        const isActive = item.path === '/' ?
                            location.pathname === '/' :
                        location.pathname.startsWith('/teams') ||
                        location.pathname.startsWith('/leaderboards') ||
                        location.pathname.startsWith('/favorites') ||
                        location.pathname.startsWith('/rivals') ||
                        location.pathname.startsWith('/profile')
                        return (
                            <Link
                                key={item.path}
                                to={item.path}
                                className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-semibold transition-colors ${
                                    isActive
                                        ? 'bg-gradient-to-r from-accent to-sky-400 text-white shadow-lg'
                                        : 'text-text-muted hover:text-text hover:bg-surface/50'
                                }`}
                            >
                                <span>{item.icon}</span>
                                <span>{item.label}</span>
                            </Link>
                        )
                    })}
                </div>
                <div className="flex-1 w-full sm:max-w-md">
                    <SearchBar
                        placeholder={searchLoading ? 'Searching...' : 'Search player ID, match ID, or name...'}
                        onSearch={handleSearch}
                    />
                </div>
            </div>
        </nav>
    )
}

export default Navigation
