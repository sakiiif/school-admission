import { describe, expect, it } from 'vitest'
import { formatRemaining, getDisplayStatus, getRemainingMs, parseTimestamp } from './holds'

describe('parseTimestamp', () => {
  it('handles the 7 fractional digits that .NET can send', () => {
    expect(parseTimestamp('2026-11-01T04:10:00.1234567Z')).toBe(Date.parse('2026-11-01T04:10:00.123Z'))
  })

  it('handles timestamps without fractional seconds', () => {
    expect(parseTimestamp('2026-11-01T04:10:00Z')).toBe(Date.parse('2026-11-01T04:10:00.000Z'))
  })
})

describe('getRemainingMs', () => {
  it('is the time left until expiry, and negative afterwards', () => {
    const expiresAt = '2026-11-01T04:10:00Z'
    const expiry = Date.parse(expiresAt)
    expect(getRemainingMs(expiresAt, expiry - 90_000)).toBe(90_000)
    expect(getRemainingMs(expiresAt, expiry + 1_000)).toBe(-1_000)
  })
})

describe('getDisplayStatus', () => {
  const expiresAt = '2026-11-01T04:10:00Z'
  const expiry = Date.parse(expiresAt)

  it('keeps HELD while time is left', () => {
    expect(getDisplayStatus('HELD', expiresAt, expiry - 1)).toBe('HELD')
  })

  it('shows HELD as EXPIRED at the expiry moment and after it', () => {
    expect(getDisplayStatus('HELD', expiresAt, expiry)).toBe('EXPIRED')
    expect(getDisplayStatus('HELD', expiresAt, expiry + 60_000)).toBe('EXPIRED')
  })

  it.each(['CONFIRMED', 'CANCELLED', 'EXPIRED'] as const)('never changes %s', (status) => {
    expect(getDisplayStatus(status, expiresAt, expiry + 60_000)).toBe(status)
  })
})

describe('formatRemaining', () => {
  it.each([
    [0, '00:00'],
    [-5_000, '00:00'],
    [999, '00:01'],
    [1_000, '00:01'],
    [1_001, '00:02'],
    [59_000, '00:59'],
    [61_000, '01:01'],
    [600_000, '10:00'],
    [3_723_000, '1:02:03'],
  ])('formats %i ms as %s', (ms, expected) => {
    expect(formatRemaining(ms)).toBe(expected)
  })
})

