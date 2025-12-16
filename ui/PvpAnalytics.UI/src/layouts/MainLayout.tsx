import type {PropsWithChildren} from 'react'
import {useState, useEffect} from 'react'
import Navigation from '../components/Navigation/Navigation'
import RightSidebar from '../components/RightSidebar/RightSidebar'
import {useThemeStore} from '../store/themeStore'
import {applyThemeClass} from '../config/themeConfig'

const MainLayout = ({children}: PropsWithChildren) => {
    const {theme} = useThemeStore()
    const [sidebarCollapsed, setSidebarCollapsed] = useState(() => {
        try {
            const saved = localStorage.getItem('sidebarCollapsed')
            return saved ? JSON.parse(saved) : false
        } catch {
            return false
        }
    })

    const [rightSidebarOpen, setRightSidebarOpen] = useState(() => {
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
                try {

                    setSidebarCollapsed(e.newValue ? JSON.parse(e.newValue) : false)
                } catch {
                    setSidebarCollapsed(false)
                }
            }

        }

        const handleCustomEvent = () => {
            try {
                const saved = localStorage.getItem('sidebarCollapsed')
                setSidebarCollapsed(saved ? JSON.parse(saved) : false)
            } catch {
                setSidebarCollapsed(false)
            }
        }

        globalThis.addEventListener('storage', handleStorageChange)
        globalThis.addEventListener('sidebarToggle', handleCustomEvent)

        return () => {
            globalThis.removeEventListener('storage', handleStorageChange)
            globalThis.removeEventListener('sidebarToggle', handleCustomEvent)
        }
    }, [])

    const toggleRightSidebar = () => {
        const newValue = !rightSidebarOpen
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
            <RightSidebar isOpen={rightSidebarOpen} onToggle={toggleRightSidebar}/>
            <main className={`relative z-10 flex flex-col transition-all duration-300 ${
                sidebarCollapsed ? 'lg:ml-20' : 'lg:ml-64'
            } ${rightSidebarOpen ? 'xl:mr-80' : ''} lg:pt-[73px]`}>
                <div className="mx-auto w-full max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
                    <div className="mt-8 lg:mt-0 w-full">{children}</div>
                </div>
            </main>
        </div>
    )
}

export default MainLayout

