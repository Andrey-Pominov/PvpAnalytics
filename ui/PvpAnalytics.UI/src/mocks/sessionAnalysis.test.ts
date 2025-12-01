import { describe, it, expect } from 'vitest'
import { mockSessionAnalysis } from './sessionAnalysis'

describe('mockSessionAnalysis', () => {
  it('generates sessions with matchCount and ratingChange in expected ranges', () => {
    const sessions = mockSessionAnalysis.sessions
    expect(sessions.length).toBeGreaterThan(0)

    for (const session of sessions) {
      expect(session.matchCount).toBeGreaterThanOrEqual(3)
      expect(session.matchCount).toBeLessThanOrEqual(7)
      expect(session.ratingChange).toBeGreaterThanOrEqual(-30)
      expect(session.ratingChange).toBeLessThanOrEqual(30)
      expect(session.averageMatchDuration).toBeGreaterThanOrEqual(280)
      expect(session.averageMatchDuration).toBeLessThanOrEqual(320)
    }
  })
})


