import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import { getSecureRandomFloat, getSecureRandomInt, getSecureRandomId } from './secureRandom'

describe('secureRandom helpers', () => {
  const originalCrypto = globalThis.crypto

  beforeEach(() => {
    const values = [0, 0xffffffff]
    let index = 0

    // Minimal crypto mock for getRandomValues / randomUUID
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const mockCrypto: any = {
      getRandomValues: (array: Uint32Array) => {
        array[0] = values[index % values.length]!
        index++
        return array
      },
      randomUUID: () => '00000000-0000-0000-0000-000000000000',
    }

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    ;(globalThis as any).crypto = mockCrypto
  })

  afterEach(() => {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    ;(globalThis as any).crypto = originalCrypto
  })

  it('getSecureRandomFloat returns value in [0,1)', () => {
    const value = getSecureRandomFloat()
    expect(value).toBeGreaterThanOrEqual(0)
    expect(value).toBeLessThan(1)
  })

  it('getSecureRandomInt returns value in range', () => {
    const value = getSecureRandomInt(10, 20)
    expect(value).toBeGreaterThanOrEqual(10)
    expect(value).toBeLessThan(20)
  })

  it('getSecureRandomId prefixes id correctly', () => {
    const id = getSecureRandomId('export-menu')
    expect(id.startsWith('export-menu-')).toBe(true)
  })
})


