/**
 * Theme-aware color utilities
 * 
 * This module provides centralized color mappings that adapt to the current theme.
 * All colors use CSS variables defined in index.css for theme consistency.
 */

/**
 * Get WoW class color classes (text color)
 * @param className - WoW class name (case-insensitive)
 * @returns Tailwind-compatible class string using CSS variables
 */
export function getWoWClassColor(className?: string): string {
  if (!className) return 'text-text'
  
  const normalized = className.toLowerCase().trim()
  
  const classMap: Record<string, string> = {
    warrior: 'text-[var(--class-warrior)]',
    paladin: 'text-[var(--class-paladin)]',
    hunter: 'text-[var(--class-hunter)]',
    rogue: 'text-[var(--class-rogue)]',
    priest: 'text-[var(--class-priest)]',
    shaman: 'text-[var(--class-shaman)]',
    mage: 'text-[var(--class-mage)]',
    warlock: 'text-[var(--class-warlock)]',
    monk: 'text-[var(--class-monk)]',
    druid: 'text-[var(--class-druid)]',
    'death knight': 'text-[var(--class-death-knight)]',
    'demon hunter': 'text-[var(--class-demon-hunter)]',
    evoker: 'text-[var(--class-evoker)]',
  }
  
  return classMap[normalized] || 'text-text'
}

/**
 * Get WoW class background and text color classes
 * @param className - WoW class name (case-insensitive)
 * @returns Object with bg and text classes
 */
export function getWoWClassColors(className?: string): { bg: string; text: string } {
  if (!className) return { bg: 'bg-accent/20', text: 'text-accent' }
  
  const normalized = className.toLowerCase().trim()
  
  const classMap: Record<string, { bg: string; text: string }> = {
    warrior: { bg: 'bg-[var(--class-warrior-bg)]', text: 'text-[var(--class-warrior)]' },
    paladin: { bg: 'bg-[var(--class-paladin-bg)]', text: 'text-[var(--class-paladin)]' },
    hunter: { bg: 'bg-[var(--class-hunter-bg)]', text: 'text-[var(--class-hunter)]' },
    rogue: { bg: 'bg-[var(--class-rogue-bg)]', text: 'text-[var(--class-rogue)]' },
    priest: { bg: 'bg-[var(--class-priest-bg)]', text: 'text-[var(--class-priest)]' },
    shaman: { bg: 'bg-[var(--class-shaman-bg)]', text: 'text-[var(--class-shaman)]' },
    mage: { bg: 'bg-[var(--class-mage-bg)]', text: 'text-[var(--class-mage)]' },
    warlock: { bg: 'bg-[var(--class-warlock-bg)]', text: 'text-[var(--class-warlock)]' },
    monk: { bg: 'bg-[var(--class-monk-bg)]', text: 'text-[var(--class-monk)]' },
    druid: { bg: 'bg-[var(--class-druid-bg)]', text: 'text-[var(--class-druid)]' },
    'death knight': { bg: 'bg-[var(--class-death-knight-bg)]', text: 'text-[var(--class-death-knight)]' },
    'demon hunter': { bg: 'bg-[var(--class-demon-hunter-bg)]', text: 'text-[var(--class-demon-hunter)]' },
    evoker: { bg: 'bg-[var(--class-evoker-bg)]', text: 'text-[var(--class-evoker)]' },
  }
  
  return classMap[normalized] || { bg: 'bg-accent/20', text: 'text-accent' }
}

/**
 * Get faction color classes
 * @param faction - Faction name (Alliance or Horde)
 * @returns Tailwind-compatible class string
 */
export function getFactionColor(faction?: string): string {
  if (!faction) return 'text-text-muted'
  
  const normalized = faction.toLowerCase()
  if (normalized.includes('alliance')) {
    return 'text-[var(--faction-alliance-text)]'
  }
  if (normalized.includes('horde')) {
    return 'text-[var(--faction-horde-text)]'
  }
  return 'text-text-muted'
}

/**
 * Get faction background and text color classes
 * @param faction - Faction name (Alliance or Horde)
 * @returns Object with bg and text classes
 */
export function getFactionColors(faction?: string): { bg: string; text: string } {
  if (!faction) return { bg: 'bg-surface/50', text: 'text-text-muted' }
  
  const normalized = faction.toLowerCase()
  if (normalized.includes('alliance')) {
    return { bg: 'bg-[var(--faction-alliance-bg)]', text: 'text-[var(--faction-alliance-text)]' }
  }
  if (normalized.includes('horde')) {
    return { bg: 'bg-[var(--faction-horde-bg)]', text: 'text-[var(--faction-horde-text)]' }
  }
  return { bg: 'bg-surface/50', text: 'text-text-muted' }
}

/**
 * Get success color classes (for positive values, victories, gains)
 */
export function getSuccessColors(): { text: string; bg: string; border: string } {
  return {
    text: 'text-[var(--color-success-text)]',
    bg: 'bg-[var(--color-success-bg)]',
    border: 'border-[var(--color-success-border)]',
  }
}

/**
 * Get error color classes (for negative values, defeats, losses)
 */
export function getErrorColors(): { text: string; bg: string; border: string } {
  return {
    text: 'text-[var(--color-error-text)]',
    bg: 'bg-[var(--color-error-bg)]',
    border: 'border-[var(--color-error-border)]',
  }
}

/**
 * Get warning color classes (for warnings, anomalies)
 */
export function getWarningColors(): { text: string; bg: string; border: string } {
  return {
    text: 'text-[var(--color-warning-text)]',
    bg: 'bg-[var(--color-warning-bg)]',
    border: 'border-[var(--color-warning-border)]',
  }
}

/**
 * Get info color classes (for informational content)
 */
export function getInfoColors(): { text: string; bg: string; border: string } {
  return {
    text: 'text-[var(--color-info-text)]',
    bg: 'bg-[var(--color-info-bg)]',
    border: 'border-[var(--color-info-border)]',
  }
}

/**
 * Get victory color classes
 */
export function getVictoryColors(): { text: string; bg: string; border: string } {
  return {
    text: 'text-[var(--color-victory-text)]',
    bg: 'bg-[var(--color-victory-bg)]',
    border: 'border-[var(--color-victory-border)]',
  }
}

/**
 * Get defeat color classes
 */
export function getDefeatColors(): { text: string; bg: string; border: string } {
  return {
    text: 'text-[var(--color-defeat-text)]',
    bg: 'bg-[var(--color-defeat-bg)]',
    border: 'border-[var(--color-defeat-border)]',
  }
}

/**
 * Get win rate color based on percentage
 * @param winRate - Win rate percentage (0-100)
 * @returns Success color if > 50%, error color otherwise
 */
export function getWinRateColor(winRate: number): string {
  return winRate > 50 
    ? 'text-[var(--color-success-text)]' 
    : 'text-[var(--color-error-text)]'
}

/**
 * Get rating change color based on value
 * @param change - Rating change (positive or negative)
 * @returns Success color if positive, error color if negative
 */
export function getRatingChangeColor(change: number): string {
  if (change > 0) return 'text-[var(--color-success-text)]'
  if (change < 0) return 'text-[var(--color-error-text)]'
  return 'text-text-muted'
}

/**
 * Get error message styling classes
 */
export function getErrorStyles(): string {
  return 'border-[var(--color-error-border)] bg-[var(--color-error-bg)] text-[var(--color-error-text)]'
}

/**
 * Get player card background class
 */
export function getPlayerCardBg(): string {
  return 'bg-[var(--player-card-bg)]'
}

