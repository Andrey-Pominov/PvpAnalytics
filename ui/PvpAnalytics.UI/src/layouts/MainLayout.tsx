import type {PropsWithChildren} from 'react'
import {useState, useEffect} from 'react'
import Navigation from '../components/Navigation/Navigation'
import RightSidebar from '../components/RightSidebar/RightSidebar'
import {useThemeStore} from '../store/themeStore'
import {applyThemeClass} from '../config/themeConfig'

const MainLayout = ({children}: PropsWithChildren) => {
    const {theme} = useThemeStore()
    const [isSidebarCollapsed, setSidebarCollapsed] = useState(() => {
        try {
            const saved = localStorage.getItem('sidebarCollapsed')
            return saved ? JSON.parse(saved) : false
        } catch {
            return false
        }
    })

    const [isRightSidebarOpen, setRightSidebarOpen] = useState(() => {
        try {
            const saved = localStorage.getItem('rightSidebarOpen')
            return saved ? JSON.parse(saved) : true
        } catch {
            return false
        }
    })

    // Initialize theme on mount (safety measure - themeStore also handles this)
    useEffect(() => {
        applyThemeClass(theme)
    }, [theme])

    useEffect(() => {
        const handleStorageChange = (e: StorageEvent) => {
            if (e.key === 'sidebarCollapsed') {
                setSidebarCollapsed(e.newValue ? JSON.parse(e.newValue) : false)
            }
        }

        const handleCustomEvent = () => {
            const saved = localStorage.getItem('sidebarCollapsed')
            setSidebarCollapsed(saved ? JSON.parse(saved) : false)
        }

        window.addEventListener('storage', handleStorageChange)
        window.addEventListener('sidebarToggle', handleCustomEvent)

        return () => {
            window.removeEventListener('storage', handleStorageChange)
            window.removeEventListener('sidebarToggle', handleCustomEvent)
        }
    }, [])

    const toggleRightSidebar = () => {
        const newValue = !isRightSidebarOpen
        setRightSidebarOpen(newValue)
        localStorage.setItem('rightSidebarOpen', JSON.stringify(newValue))
    }

    return (
        <div className="main-layout-bg relative min-h-screen overflow-hidden transition-colors duration-300">
            <div
                className="main-layout-glow pointer-events-none absolute inset-0 blur-[120px] transition-opacity duration-300"
                aria-hidden="true">
                <div className="absolute inset-0 -z-10 scale-105"/>
            </div>
            <Navigation/>
            <RightSidebar isOpen={isRightSidebarOpen} onToggle={toggleRightSidebar}/>
            <main className={`relative z-10 flex flex-col transition-all duration-300 ${
                isSidebarCollapsed ? 'lg:ml-20' : 'lg:ml-64'
            } ${isRightSidebarOpen ? 'xl:mr-80' : ''} lg:pt-[73px]`}>
                <div className="mx-auto w-full max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
                    <div className="mt-8 lg:mt-0 w-full">{children}</div>
                </div>
            </main>
        </div>
    )
}

export default MainLayout

