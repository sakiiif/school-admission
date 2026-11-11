import { act, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { api } from '../api/client'
import { ApiError } from '../api/errors'
import { makeClass } from '../test/factories'
import { renderWithClient } from '../test/renderWithClient'
import { ClassList } from './ClassList'

const STUDENT = '20152691234567890'
const OPEN = makeClass()
const FULL = makeClass({
  id: 'c_full',
  name: 'Class 6 - Day Shift',
  totalSeats: 1,
  heldSeats: 0,
  confirmedSeats: 1,
  availableSeats: 0,
})

function renderList(studentId: string, showSuccess = vi.fn(), showError = vi.fn()) {
  return {
    showSuccess,
    showError,
    ...renderWithClient(<ClassList studentId={studentId} showSuccess={showSuccess} showError={showError} />),
  }
}

describe('ClassList', () => {
  it('shows total, held, confirmed and available seats for each class', async () => {
    vi.spyOn(api, 'listClasses').mockResolvedValue([OPEN])
    renderList(STUDENT)

    const row = await screen.findByRole('row', { name: /Class 6 - Morning Shift/ })
    expect(within(row).getByText('120')).toBeInTheDocument()
    expect(within(row).getByText('15')).toBeInTheDocument()
    expect(within(row).getByText('80')).toBeInTheDocument()
    expect(within(row).getByText('25')).toBeInTheDocument()
  })

  it('disables every Apply button until a valid student id is entered', async () => {
    vi.spyOn(api, 'listClasses').mockResolvedValue([OPEN, FULL])
    renderList('')

    await screen.findByText('Class 6 - Morning Shift')

    for (const button of screen.getAllByRole('button', { name: 'Apply' })) {
      expect(button).toBeDisabled()
    }
    expect(screen.getByText('Enter a valid student registration number above to apply.')).toBeInTheDocument()
  })

  it('enables Apply when seats are left and disables it for a full class', async () => {
    vi.spyOn(api, 'listClasses').mockResolvedValue([OPEN, FULL])
    renderList(STUDENT)

    const openRow = await screen.findByRole('row', { name: /Class 6 - Morning Shift/ })
    const fullRow = screen.getByRole('row', { name: /Class 6 - Day Shift/ })

    expect(within(openRow).getByRole('button', { name: 'Apply' })).toBeEnabled()
    expect(within(fullRow).getByRole('button', { name: 'Apply' })).toBeDisabled()
    expect(within(fullRow).getByText('0 (full)')).toBeInTheDocument()
  })

  it('applies for a seat and reports success', async () => {
    vi.spyOn(api, 'listClasses').mockResolvedValue([OPEN])
    const apply = vi.spyOn(api, 'applyForSeat').mockResolvedValue({
      holdId: 'h_9',
      classId: OPEN.id,
      studentId: STUDENT,
      status: 'HELD',
      expiresAt: '2026-11-01T04:10:00Z',
    })
    const user = userEvent.setup()
    const { showSuccess } = renderList(STUDENT)

    await user.click(await screen.findByRole('button', { name: 'Apply' }))

    await waitFor(() => expect(apply).toHaveBeenCalledWith(OPEN.id, STUDENT))
    await waitFor(() =>
      expect(showSuccess).toHaveBeenCalledWith(expect.stringContaining('Class 6 - Morning Shift')),
    )
  })

  it('shows the server message when the class filled up in the meantime', async () => {
    vi.spyOn(api, 'listClasses').mockResolvedValue([OPEN])
    vi.spyOn(api, 'applyForSeat').mockRejectedValue(
      new ApiError(409, 'NO_SEATS_AVAILABLE', 'No seats are available in this class.'),
    )
    const user = userEvent.setup()
    const { showError } = renderList(STUDENT)

    await user.click(await screen.findByRole('button', { name: 'Apply' }))

    await waitFor(() => expect(showError).toHaveBeenCalledWith('No seats are available in this class.'))
  })

  it('keeps the last known counts and warns when a refresh fails', async () => {
    const list = vi.spyOn(api, 'listClasses')
    list.mockResolvedValueOnce([OPEN])
    const { client } = renderList(STUDENT)
    await screen.findByText('Class 6 - Morning Shift')

    list.mockRejectedValue(
      new ApiError(0, 'NETWORK_ERROR', 'Cannot reach the server. Please check your connection and try again.'),
    )
    await act(async () => {
      await client.invalidateQueries({ queryKey: ['classes'] })
    })

    expect(await screen.findByRole('alert')).toHaveTextContent('Showing the last known seat counts.')
    expect(screen.getByText('Class 6 - Morning Shift')).toBeInTheDocument()
  })

  it('shows a friendly error with a retry button when classes cannot be loaded at all', async () => {
    const list = vi.spyOn(api, 'listClasses')
    list.mockRejectedValueOnce(
      new ApiError(0, 'NETWORK_ERROR', 'Cannot reach the server. Please check your connection and try again.'),
    )
    list.mockResolvedValue([OPEN])
    const user = userEvent.setup()
    renderList(STUDENT)

    expect(await screen.findByRole('alert')).toHaveTextContent('Cannot reach the server.')

    await user.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('Class 6 - Morning Shift')).toBeInTheDocument()
  })
})

