import type { PropsWithChildren, ReactNode } from 'react'

interface CardProps {
  title?: string
  subtitle?: string
  actions?: ReactNode
  className?: string
}

const baseClasses =
  'flex flex-col gap-4 rounded-2xl border border-accent-muted/40 bg-surface/90 p-6 shadow-card backdrop-blur-lg'

const Card = ({ title, subtitle, actions, className, children }: PropsWithChildren<CardProps>) => (
  <section className={[baseClasses, className ?? ''].filter(Boolean).join(' ')}>
    {(title || subtitle || actions) && (
      <header className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          {title && <h2 className="text-base font-semibold text-text">{title}</h2>}
          {subtitle && <p className="mt-1 text-sm text-text-muted">{subtitle}</p>}
        </div>
        {actions && <div className="flex-shrink-0 sm:ml-auto">{actions}</div>}
      </header>
    )}
    <div className="flex flex-col gap-4">{children}</div>
  </section>
)

export default Card

