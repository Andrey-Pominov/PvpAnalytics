/**
 * Generates a cryptographically secure random float in the range [0, 1).
 * Uses crypto.getRandomValues() when available.
 * 
 * Security considerations:
 * - Uses CSPRNG (cryptographically secure pseudorandom number generator)
 * - Each call generates a new random value (not reused)
 * - Values are not exposed or stored unless explicitly handled by caller
 * - Does not fall back to Math.random() to maintain security guarantees
 * 
 * @returns A random float in [0, 1)
 * @throws Error if no secure random number generator is available
 */
export const getSecureRandomFloat = (): number => {
  // Check for browser crypto API (Web Crypto API)
  // This is available in all modern browsers and is the standard secure RNG
  if (globalThis.window.crypto?.getRandomValues) {
    const array = new Uint32Array(1)
    globalThis.window.crypto.getRandomValues(array)
    // Convert Uint32 (0 to 2^32-1) to float [0, 1)
    // Using 0x100000000 (2^32) for clarity and precision
    return array[0] / 0x100000000
  }

  throw new Error(
    'Secure random number generator not available. ' +
    'This function requires crypto.getRandomValues() which is available in all modern browsers. ' +
    'Falling back to Math.random() would compromise security.'
  )
}

export const getSecureRandomInt = (minInclusive: number, maxExclusive: number): number => {
  const r = getSecureRandomFloat()
  return Math.floor(r * (maxExclusive - minInclusive)) + minInclusive
}

export const getSecureRandomId = (prefix: string): string => {
  if (globalThis.window?.crypto?.randomUUID) {
    return `${prefix}-${globalThis.window.crypto.randomUUID()}`
  }

  const rand = getSecureRandomFloat().toString(36).substring(2)
  return `${prefix}-${rand}`
}
