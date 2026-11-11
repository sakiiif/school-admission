import { act, renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { useStudentId } from './useStudentId'

const KEY = 'admission.studentId'
const VALID = '20152691234567890'

beforeEach(() => {
  window.localStorage.clear()
})

describe('useStudentId', () => {
  it('starts empty when nothing is stored', () => {
    const { result } = renderHook(() => useStudentId())
    expect(result.current.draft).toBe('')
    expect(result.current.studentId).toBe('')
  })

  it('exposes a student id only once all 17 digits are typed', () => {
    const { result } = renderHook(() => useStudentId())

    act(() => result.current.setDraft('2015269'))
    expect(result.current.studentId).toBe('')

    act(() => result.current.setDraft(VALID))
    expect(result.current.studentId).toBe(VALID)
  })

  it('drops letters and ignores digits beyond 17', () => {
    const { result } = renderHook(() => useStudentId())
    act(() => result.current.setDraft('2015abc2691234567890123'))
    expect(result.current.draft).toBe(VALID)
  })

  it('remembers a valid id in the browser', () => {
    const { result } = renderHook(() => useStudentId())
    act(() => result.current.setDraft(VALID))
    expect(window.localStorage.getItem(KEY)).toBe(VALID)
  })

  it('restores the stored id after a reload', () => {
    window.localStorage.setItem(KEY, VALID)
    const { result } = renderHook(() => useStudentId())
    expect(result.current.studentId).toBe(VALID)
    expect(result.current.draft).toBe(VALID)
  })

  it('ignores a corrupt stored value', () => {
    window.localStorage.setItem(KEY, 'not-an-id')
    const { result } = renderHook(() => useStudentId())
    expect(result.current.studentId).toBe('')
  })

  it('forgets the id when the box is cleared', () => {
    const { result } = renderHook(() => useStudentId())
    act(() => result.current.setDraft(VALID))
    act(() => result.current.setDraft(''))
    expect(window.localStorage.getItem(KEY)).toBeNull()
  })
})

