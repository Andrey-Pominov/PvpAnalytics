import type { PropsWithChildren, ReactNode } from 'react'
import styles from './Card.module.css'

interface CardProps {
  title?: string
  subtitle?: string
  actions?: ReactNode
  className?: string
}

const Card = ({ title, subtitle, actions, className, children }: PropsWithChildren<CardProps>) => {
  return (
    <section className={`${styles.card} ${className ?? ''}`.trim()}>
      {(title || actions) && (
        <header className={styles.header}>
          <div>
            {title && <h2 className={styles.title}>{title}</h2>}
            {subtitle && <p className={styles.subtitle}>{subtitle}</p>}
          </div>
          {actions && <div className={styles.actions}>{actions}</div>}
        </header>
      )}
      <div className={styles.body}>{children}</div>
    </section>
  )
}

export default Card

