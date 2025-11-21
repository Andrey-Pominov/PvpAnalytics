import type { PropsWithChildren } from 'react'
import Navigation from '../components/Navigation/Navigation'

const MainLayout = ({ children }: PropsWithChildren) => (
  <div className="relative min-h-screen overflow-hidden bg-gradient-to-br from-[#0e1f3d]/85 to-[#080e1d]/92">
    <div className="pointer-events-none absolute inset-0 opacity-85 blur-[120px]" aria-hidden="true">
      <div className="absolute inset-0 -z-10 scale-105 bg-[radial-gradient(circle_at_20%_20%,rgba(84,117,255,0.2),transparent_55%),radial-gradient(circle_at_80%_0%,rgba(99,201,255,0.2),transparent_40%),radial-gradient(circle_at_50%_75%,rgba(40,63,122,0.3),transparent_50%)]" />
    </div>
    <main className="relative z-10 mx-auto flex max-w-6xl flex-col px-4 py-8 sm:px-6 lg:px-8">
      <Navigation />
      <div className="mt-8">{children}</div>
    </main>
  </div>
)

export default MainLayout

