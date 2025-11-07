import styles from './ToggleGroup.module.css'

export interface ToggleOption {
  id: string
  label: string
}

interface ToggleGroupProps {
  options: ToggleOption[]
  activeId: string
  onChange?: (id: string) => void
}

const ToggleGroup = ({ options, activeId, onChange }: ToggleGroupProps) => {
  return (
    <div className={styles.group}>
      {options.map((option) => (
        <button
          key={option.id}
          className={`${styles.button} ${option.id === activeId ? styles.active : ''}`}
          type="button"
          onClick={() => onChange?.(option.id)}
        >
          {option.label}
        </button>
      ))}
    </div>
  )
}

export default ToggleGroup

