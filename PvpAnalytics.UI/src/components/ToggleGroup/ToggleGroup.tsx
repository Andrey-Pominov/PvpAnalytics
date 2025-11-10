export interface ToggleOption {
  id: string
  label: string
}

interface ToggleGroupProps {
  options: ToggleOption[]
  activeId: string
  onChange?: (id: string) => void
}

const baseButtonClasses =
  'rounded-full border border-accent-muted/60 bg-background/70 px-4 py-2 text-sm font-medium text-text-muted/80 transition duration-150 hover:border-accent hover:text-text focus:outline-none focus:ring-2 focus:ring-accent/60'

const activeButtonClasses = 'bg-gradient-to-r from-accent to-sky-400 text-white border-transparent shadow-lg'

const ToggleGroup = ({ options, activeId, onChange }: ToggleGroupProps) => (
  <div className="flex flex-wrap gap-3">
    {options.map((option) => (
      <button
        key={option.id}
        className={[baseButtonClasses, option.id === activeId ? activeButtonClasses : ''].filter(Boolean).join(' ')}
        type="button"
        onClick={() => onChange?.(option.id)}
      >
        {option.label}
      </button>
    ))}
  </div>
)

export default ToggleGroup

