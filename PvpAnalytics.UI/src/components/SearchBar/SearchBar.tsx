import { useState } from 'react'
import type { FormEvent } from 'react'
import styles from './SearchBar.module.css'

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
    <form className={styles.form} onSubmit={handleSubmit}>
      <input
        className={styles.input}
        placeholder={placeholder}
        value={value}
        onChange={(event) => setValue(event.target.value)}
      />
      <button className={styles.button} type="submit">
        Search
      </button>
    </form>
  )
}

export default SearchBar

