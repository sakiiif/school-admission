import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { api } from '../api/client'
import { ApiError } from '../api/errors'
import { renderWithClient } from '../test/renderWithClient'
import { CreateClassForm } from './CreateClassForm'

const NAME_ERROR = 'Enter a class name.'
const SEATS_ERROR = 'Enter a whole number of seats from 1 to 100000.'

function renderForm(showSuccess = vi.fn(), showError = vi.fn()) {
  renderWithClient(<CreateClassForm showSuccess={showSuccess} showError={showError} />)
  return { showSuccess, showError }
}

describe('CreateClassForm', () => {
  it('shows inline errors and sends nothing when the form is empty', async () => {
    const create = vi.spyOn(api, 'createClass')
    const user = userEvent.setup()
    renderForm()

    await user.click(screen.getByRole('button', { name: 'Create class' }))

    expect(screen.getByText(NAME_ERROR)).toBeInTheDocument()
    expect(screen.getByText(SEATS_ERROR)).toBeInTheDocument()
    expect(create).not.toHaveBeenCalled()
  })

  it('rejects a name made only of spaces', async () => {
    const create = vi.spyOn(api, 'createClass')
    const user = userEvent.setup()
    renderForm()

    await user.type(screen.getByLabelText('Class name'), '   ')
    await user.type(screen.getByLabelText('Number of seats'), '10')
    await user.click(screen.getByRole('button', { name: 'Create class' }))

    expect(screen.getByText(NAME_ERROR)).toBeInTheDocument()
    expect(create).not.toHaveBeenCalled()
  })

  it.each(['0', '-5', '2.5'])('rejects %s as a seat count', async (seats) => {
    const create = vi.spyOn(api, 'createClass')
    const user = userEvent.setup()
    renderForm()

    await user.type(screen.getByLabelText('Class name'), 'Class 7 - Morning Shift')
    await user.type(screen.getByLabelText('Number of seats'), seats)
    await user.click(screen.getByRole('button', { name: 'Create class' }))

    expect(screen.getByText(SEATS_ERROR)).toBeInTheDocument()
    expect(create).not.toHaveBeenCalled()
  })

  it('creates the class with a trimmed name, reports success and clears the form', async () => {
    const create = vi.spyOn(api, 'createClass').mockResolvedValue({
      id: 'c_7',
      name: 'Class 7 - Morning Shift',
      totalSeats: 2,
      heldSeats: 0,
      confirmedSeats: 0,
      availableSeats: 2,
    })
    const user = userEvent.setup()
    const { showSuccess } = renderForm()

    await user.type(screen.getByLabelText('Class name'), '  Class 7 - Morning Shift  ')
    await user.type(screen.getByLabelText('Number of seats'), '2')
    await user.click(screen.getByRole('button', { name: 'Create class' }))

    await waitFor(() => expect(create).toHaveBeenCalledWith('Class 7 - Morning Shift', 2))
    await waitFor(() => expect(showSuccess).toHaveBeenCalledWith(expect.stringContaining('Class 7 - Morning Shift')))
    expect(screen.getByLabelText('Class name')).toHaveValue('')
    expect(screen.getByLabelText('Number of seats')).toHaveValue(null)
  })

  it('shows the server message and keeps the input when creation fails', async () => {
    vi.spyOn(api, 'createClass').mockRejectedValue(
      new ApiError(400, 'VALIDATION_ERROR', 'Class name is required.'),
    )
    const user = userEvent.setup()
    const { showError } = renderForm()

    await user.type(screen.getByLabelText('Class name'), 'Class 7')
    await user.type(screen.getByLabelText('Number of seats'), '5')
    await user.click(screen.getByRole('button', { name: 'Create class' }))

    await waitFor(() => expect(showError).toHaveBeenCalledWith('Class name is required.'))
    expect(screen.getByLabelText('Class name')).toHaveValue('Class 7')
  })
})


