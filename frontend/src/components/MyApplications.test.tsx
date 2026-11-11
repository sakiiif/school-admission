import { act, cleanup, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { api } from '../api/client'
import { ApiError } from '../api/errors'
import { makeMyHold } from '../test/factories'
import { createTestClient, renderWithClient } from '../test/renderWithClient'
import { MyApplications } from './MyApplications'

const STUDENT = '20152691234567890'
const START = new Date('2026-11-01T04:00:00.000Z')

const inSeconds = (seconds: number) => new Date(START.getTime() + seconds * 1000).toISOString()

function renderApplications(showSuccess = vi.fn(), showError = vi.fn(), client = createTestClient()) {
  return {
    showSuccess,
    showError,
    ...renderWithClient(
      <MyApplications studentId={STUDENT} showSuccess={showSuccess} showError={showError} />,
      client,
    ),
  }
}

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval', 'Date'] })
  vi.setSystemTime(START)
})

afterEach(() => {
  cleanup()
  vi.useRealTimers()
})

describe('MyApplications', () => {
  it('counts down and turns Expired by itself, disabling Pay and Cancel', async () => {
    // A server that is slow to tidy up: it keeps reporting HELD.
    // The page must still flip to Expired on its own when the time runs out.
    vi.spyOn(api, 'listMyHolds').mockResolvedValue([makeMyHold({ expiresAt: inSeconds(10) })])
    const client = createTestClient()
    const invalidate = vi.spyOn(client, 'invalidateQueries')
    renderApplications(vi.fn(), vi.fn(), client)

    expect(await screen.findByText('00:10')).toBeInTheDocument()
    expect(screen.getByText('Held')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Pay Admission Fee' })).toBeEnabled()

    act(() => {
      vi.advanceTimersByTime(4_000)
    })
    expect(screen.getByText('00:06')).toBeInTheDocument()
    expect(invalidate).not.toHaveBeenCalledWith({ queryKey: ['classes'] })

    act(() => {
      vi.advanceTimersByTime(6_000)
    })
    expect(screen.getByText('Expired')).toBeInTheDocument()
    expect(screen.queryByText('Held')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Pay Admission Fee' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()

    // The released seat is requested from the server straight away.
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['classes'] })
  })

  it('shows the last minute in red', async () => {
    vi.spyOn(api, 'listMyHolds').mockResolvedValue([makeMyHold({ expiresAt: inSeconds(90) })])
    renderApplications()

    const time = await screen.findByText('01:30')
    expect(time).not.toHaveClass('urgent')

    act(() => {
      vi.advanceTimersByTime(35_000)
    })
    expect(screen.getByText('00:55')).toHaveClass('urgent')
  })

  it('pays the admission fee for a held application', async () => {
    const hold = makeMyHold({ expiresAt: inSeconds(600) })
    vi.spyOn(api, 'listMyHolds').mockResolvedValue([hold])
    const confirm = vi
      .spyOn(api, 'confirmHold')
      .mockResolvedValue({ holdId: hold.holdId, status: 'CONFIRMED' })
    const user = userEvent.setup()
    const { showSuccess } = renderApplications()

    await user.click(await screen.findByRole('button', { name: 'Pay Admission Fee' }))

    await waitFor(() => expect(confirm).toHaveBeenCalledWith(hold.holdId, STUDENT))
    await waitFor(() =>
      expect(showSuccess).toHaveBeenCalledWith(expect.stringContaining('Class 6 - Morning Shift')),
    )
  })

  it('explains a failed payment in plain language', async () => {
    vi.spyOn(api, 'listMyHolds').mockResolvedValue([makeMyHold({ expiresAt: inSeconds(600) })])
    vi.spyOn(api, 'confirmHold').mockRejectedValue(
      new ApiError(409, 'HOLD_EXPIRED', 'This application has expired. Please apply again.'),
    )
    const user = userEvent.setup()
    const { showError } = renderApplications()

    await user.click(await screen.findByRole('button', { name: 'Pay Admission Fee' }))

    await waitFor(() => expect(showError).toHaveBeenCalledWith('This application has expired. Please apply again.'))
  })

  it('asks before cancelling, then releases the seat', async () => {
    const hold = makeMyHold({ expiresAt: inSeconds(600) })
    vi.spyOn(api, 'listMyHolds').mockResolvedValue([hold])
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    const cancel = vi.spyOn(api, 'cancelHold').mockResolvedValue(undefined)
    const user = userEvent.setup()
    const { showSuccess } = renderApplications()

    await user.click(await screen.findByRole('button', { name: 'Cancel' }))

    await waitFor(() => expect(cancel).toHaveBeenCalledWith(hold.holdId, STUDENT))
    await waitFor(() => expect(showSuccess).toHaveBeenCalledWith(expect.stringContaining('cancelled')))
  })

  it('does nothing when the cancel confirmation is dismissed', async () => {
    vi.spyOn(api, 'listMyHolds').mockResolvedValue([makeMyHold({ expiresAt: inSeconds(600) })])
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    const cancel = vi.spyOn(api, 'cancelHold').mockResolvedValue(undefined)
    const user = userEvent.setup()
    renderApplications()

    await user.click(await screen.findByRole('button', { name: 'Cancel' }))

    expect(cancel).not.toHaveBeenCalled()
  })

  it('shows confirmed and cancelled applications without any actions', async () => {
    vi.spyOn(api, 'listMyHolds').mockResolvedValue([
      makeMyHold({ holdId: 'h_2', status: 'CONFIRMED' }),
      makeMyHold({ holdId: 'h_3', status: 'CANCELLED' }),
    ])
    renderApplications()

    expect(await screen.findByText('Confirmed')).toBeInTheDocument()
    expect(screen.getByText('Cancelled')).toBeInTheDocument()
    expect(screen.getAllByText('—')).toHaveLength(2)
    for (const button of screen.getAllByRole('button', { name: 'Pay Admission Fee' })) {
      expect(button).toBeDisabled()
    }
  })

  it('says so when the student has no applications yet', async () => {
    vi.spyOn(api, 'listMyHolds').mockResolvedValue([])
    renderApplications()

    expect(await screen.findByText('You have not applied to any class yet.')).toBeInTheDocument()
  })

  it('does not call the server until a student id is entered', () => {
    const list = vi.spyOn(api, 'listMyHolds')
    renderWithClient(<MyApplications studentId="" showSuccess={vi.fn()} showError={vi.fn()} />)

    expect(screen.getByText(/Enter a valid student registration number/)).toBeInTheDocument()
    expect(list).not.toHaveBeenCalled()
  })
})

