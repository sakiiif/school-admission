import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useNotice } from './useNotice'

beforeEach(() => {
  vi.useFakeTimers()
})

afterEach(() => {
  vi.useRealTimers()
})

describe('useNotice', () => {
  it('clears a success message after 6 seconds', () => {
    const { result } = renderHook(() => useNotice())

    act(() => result.current.showSuccess('Done'))
    expect(result.current.notice).toEqual({ kind: 'success', text: 'Done' })

    act(() => {
      vi.advanceTimersByTime(5_900)
    })
    expect(result.current.notice).not.toBeNull()

    act(() => {
      vi.advanceTimersByTime(200)
    })
    expect(result.current.notice).toBeNull()
  })

  it('keeps an error visible longer, for 10 seconds', () => {
    const { result } = renderHook(() => useNotice())

    act(() => result.current.showError('No seats are available in this class.'))

    act(() => {
      vi.advanceTimersByTime(6_100)
    })
    expect(result.current.notice?.kind).toBe('error')

    act(() => {
      vi.advanceTimersByTime(4_000)
    })
    expect(result.current.notice).toBeNull()
  })

  it('replaces the previous message with the newest one', () => {
    const { result } = renderHook(() => useNotice())

    act(() => result.current.showError('Something failed.'))
    act(() => result.current.showSuccess('Fixed.'))

    expect(result.current.notice).toEqual({ kind: 'success', text: 'Fixed.' })
  })

  it('can be dismissed by hand', () => {
    const { result } = renderHook(() => useNotice())

    act(() => result.current.showSuccess('Done'))
    act(() => result.current.dismiss())

    expect(result.current.notice).toBeNull()
  })
})

