import type { HoldStatus } from '../api/types'

 // The backend can send up to 7 fractional-second digits. JavaScript only
 // guarantees 3, so extra digits are trimmed before parsing.
export function parseTimestamp(iso: string): number {
  return Date.parse(iso.replace(/(\.\d{3})\d+/, '$1'))
}

// Milliseconds until the hold expires. Zero or negative means it has expired
export function getRemainingMs(expiresAt: string, now: number): number {
  return parseTimestamp(expiresAt) - now
}

// A HELD application whose time has run out is shown as EXPIRED immediately.
export function getDisplayStatus(status: HoldStatus, expiresAt: string, now: number): HoldStatus {
  return status === 'HELD' && getRemainingMs(expiresAt, now) <= 0 ? 'EXPIRED' : status
}

// 9:05 left shows as "09:05", an hour or more as "1:02:03"
export function formatRemaining(ms: number): string {
  const totalSeconds = Math.max(0, Math.ceil(ms / 1000))
  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60

  const mm = String(minutes).padStart(2, '0')
  const ss = String(seconds).padStart(2, '0')
  return hours > 0 ? `${hours}:${mm}:${ss}` : `${mm}:${ss}`
}

