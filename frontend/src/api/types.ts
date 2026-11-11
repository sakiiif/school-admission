export type HoldStatus = 'HELD' | 'CONFIRMED' | 'EXPIRED' | 'CANCELLED'

export interface ClassInfo {
  id: string
  name: string
  totalSeats: number
  heldSeats: number
  confirmedSeats: number
  availableSeats: number
}

export interface Hold {
  holdId: string
  classId: string
  studentId: string
  status: HoldStatus
  expiresAt: string 
}

export interface ConfirmResult {
  holdId: string
  status: HoldStatus
}

export interface MyHold {
  holdId: string
  classId: string
  className: string
  status: HoldStatus
  expiresAt: string
}