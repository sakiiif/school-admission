import type { ClassInfo, MyHold } from '../api/types'

export function makeClass(overrides: Partial<ClassInfo> = {}): ClassInfo {
  return {
    id: 'c_open',
    name: 'Class 6 - Morning Shift',
    totalSeats: 120,
    heldSeats: 15,
    confirmedSeats: 80,
    availableSeats: 25,
    ...overrides,
  }
}

export function makeMyHold(overrides: Partial<MyHold> = {}): MyHold {
  return {
    holdId: 'h_1',
    classId: 'c_open',
    className: 'Class 6 - Morning Shift',
    status: 'HELD',
    expiresAt: '2026-11-01T04:10:00Z',
    ...overrides,
  }
}

