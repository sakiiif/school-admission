import { describe, expect, it } from 'vitest'
import { isValidStudentId } from './studentId'

describe('isValidStudentId', () => {
  it('accepts exactly 17 digits', () => {
    expect(isValidStudentId('20152691234567890')).toBe(true)
  })

  it.each(['', '123', '2015269123456789', '201526912345678901', '2015269123456789a', ' 20152691234567890'])(
    'rejects %j',
    (value) => {
      expect(isValidStudentId(value)).toBe(false)
    },
  )
})

