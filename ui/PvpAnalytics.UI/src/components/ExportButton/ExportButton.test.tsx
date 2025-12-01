import { describe, it, expect } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import ExportButton from './ExportButton'

describe('ExportButton', () => {
  const data = [{ id: 1 }]

  it('wires aria-controls to generated menu id and toggles menu visibility', () => {
    render(<ExportButton data={data} filename="test" />)

    const button = screen.getByRole('button', { name: /export data/i })
    const menuId = button.getAttribute('aria-controls')
    expect(menuId).toBeTruthy()

    // Initially menu is not rendered
    expect(screen.queryByRole('menu')).toBeNull()

    // Open menu
    fireEvent.click(button)
    const menu = screen.getByRole('menu')
    expect(menu.id).toBe(menuId)

    // Close menu
    fireEvent.click(button)
    expect(screen.queryByRole('menu')).toBeNull()
  })
})


