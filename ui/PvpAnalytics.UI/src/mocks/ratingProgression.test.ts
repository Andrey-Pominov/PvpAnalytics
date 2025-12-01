import { describe, it, expect } from 'vitest'
import { mockRatingProgression } from './ratingProgression'

describe('mockRatingProgression', () => {
  it('generates rating changes within expected bounds', () => {
    const points = mockRatingProgression.dataPoints
    expect(points.length).toBeGreaterThan(0)

    for (const point of points) {
      expect(point.ratingChange).toBeGreaterThanOrEqual(-20)
      expect(point.ratingChange).toBeLessThanOrEqual(20)
      expect(point.ratingAfter).toBeGreaterThanOrEqual(1500)
      expect(point.ratingAfter).toBeLessThanOrEqual(2800)
    }
  })
})


