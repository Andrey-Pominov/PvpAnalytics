import { evaluateMetric, parseMetric } from './metricParser'

describe('metricParser', () => {
  it('parses variables from a valid expression', () => {
    const result = parseMetric('DamageDone / Math.max(TotalMatches, 1)')
    expect(result.isValid).toBe(true)
    expect(result.variables.sort()).toEqual(['DamageDone', 'TotalMatches'].sort())
  })

  it('rejects expressions with unsupported functions', () => {
    const result = parseMetric('DamageDone / customFunc(TotalMatches)')
    expect(result.isValid).toBe(false)
    expect(result.error).toBeDefined()
  })

  it('evaluates a simple expression safely', () => {
    const { result, error } = evaluateMetric('DamageDone / Math.max(TotalMatches, 1)', {
      DamageDone: 1000,
      TotalMatches: 4,
    })

    expect(error).toBeUndefined()
    expect(result).toBeCloseTo(250)
  })

  it('fails evaluation when variables are missing', () => {
    const { result, error } = evaluateMetric('DamageDone / Math.max(TotalMatches, 1)', {
      DamageDone: 1000,
    })

    expect(result).toBe(0)
    expect(error).toMatch(/Missing variables/i)
  })
})


