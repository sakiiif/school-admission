import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { StudentIdBar } from './StudentIdBar'

describe('StudentIdBar', () => {
  it('asks for the registration number when empty', () => {
    render(<StudentIdBar value="" onChange={vi.fn()} isValid={false} />)

    expect(screen.getByText(/Enter your 17-digit registration number/)).toBeInTheDocument()
    expect(screen.getByLabelText('Student registration number')).toHaveAttribute('aria-invalid', 'false')
    expect(screen.queryByRole('button', { name: 'Clear' })).not.toBeInTheDocument()
  })

  it('shows progress and marks a partial id as invalid', () => {
    render(<StudentIdBar value="201526" onChange={vi.fn()} isValid={false} />)

    expect(screen.getByText('6 of 17 digits entered. Digits only.')).toBeInTheDocument()
    expect(screen.getByLabelText('Student registration number')).toHaveAttribute('aria-invalid', 'true')
  })

  it('confirms a complete id', () => {
    render(<StudentIdBar value="20152691234567890" onChange={vi.fn()} isValid />)

    expect(screen.getByText('Student ID accepted. You can apply for seats.')).toBeInTheDocument()
    expect(screen.getByLabelText('Student registration number')).toHaveAttribute('aria-invalid', 'false')
  })

  it('reports what the user types', async () => {
    const onChange = vi.fn()
    const user = userEvent.setup()
    render(<StudentIdBar value="" onChange={onChange} isValid={false} />)

    await user.type(screen.getByLabelText('Student registration number'), '7')

    expect(onChange).toHaveBeenCalledWith('7')
  })

  it('clears the id with the Clear button', async () => {
    const onChange = vi.fn()
    const user = userEvent.setup()
    render(<StudentIdBar value="201526" onChange={onChange} isValid={false} />)

    await user.click(screen.getByRole('button', { name: 'Clear' }))

    expect(onChange).toHaveBeenCalledWith('')
  })
})

