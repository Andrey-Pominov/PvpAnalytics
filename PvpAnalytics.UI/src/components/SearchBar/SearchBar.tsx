import { useState } from 'react'
import type { FormEvent } from 'react'

interface SearchBarProps {
  placeholder?: string
  onSearch?: (term: string) => void
}

const SearchBar = ({ placeholder = 'Search player / team', onSearch }: SearchBarProps) => {
  const [value, setValue] = useState('')

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    onSearch?.(value.trim())
  }

  return (
    <form className="grid w-full gap-3 sm:grid-cols-[1fr_auto]" onSubmit={handleSubmit}>
      <input
        className="w-full rounded-2xl border border-accent-muted/50 bg-background/80 px-4 py-3 text-sm text-text placeholder:text-text-muted/70 focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/60"
        placeholder={placeholder}
        value={value}
        onChange={(event) => setValue(event.target.value)}
      />
      <button
        className="inline-flex items-center justify-center rounded-xl bg-gradient-to-r from-accent to-sky-400 px-5 py-3 text-sm font-semibold text-white transition duration-150 ease-out hover:-translate-y-0.5 hover:shadow-lg focus:outline-none focus:ring-2 focus:ring-accent focus:ring-offset-2 focus:ring-offset-background sm:h-full sm:px-6 sm:py-0"
        type="submit"
      >
        Search
      </button>
    </form>
  )
}

export default SearchBar

