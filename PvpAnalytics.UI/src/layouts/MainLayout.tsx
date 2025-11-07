import type { PropsWithChildren } from 'react'
import styles from './MainLayout.module.css'

const MainLayout = ({ children }: PropsWithChildren) => {
  return (
    <div className={styles.appShell}>
      <div className={styles.backdrop} />
      <main className={styles.content}>{children}</main>
    </div>
  )
}

export default MainLayout

