export const getSecureRandomFloat = (): number => {
  if (globalThis.window?.crypto?.getRandomValues) {
    const array = new Uint32Array(1)
    globalThis.window.crypto.getRandomValues(array)
    // Convert to [0, 1)
    return array[0] / (0xffffffff + 1)
  }

  // Fallback for non-browser environments; not cryptographically secure
  return Math.random()
}

export const getSecureRandomInt = (minInclusive: number, maxExclusive: number): number => {
  const r = getSecureRandomFloat()
  return Math.floor(r * (maxExclusive - minInclusive)) + minInclusive
}

export const getSecureRandomId = (prefix: string): string => {
  if (globalThis.window?.crypto?.randomUUID) {
    return `${prefix}-${globalThis.window.crypto.randomUUID()}`
  }

  // Fallback: use secure float-based string
  const rand = getSecureRandomFloat().toString(36).substring(2)
  return `${prefix}-${rand}`
}
