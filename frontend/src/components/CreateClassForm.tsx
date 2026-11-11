import { useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { toUserMessage } from '../api/errors'

const MAX_NAME_LENGTH = 200
const MAX_SEATS = 100_000

interface Props {
  showSuccess: (text: string) => void
  showError: (text: string) => void
}

export function CreateClassForm({ showSuccess, showError }: Props) {
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [seats, setSeats] = useState('')
  const [submitted, setSubmitted] = useState(false)

  const create = useMutation({
    mutationFn: (input: { name: string; totalSeats: number }) =>
      api.createClass(input.name, input.totalSeats),
    onSuccess: (created) => {
      showSuccess(`Class "${created.name}" was created with ${created.totalSeats} seats.`)
      setName('')
      setSeats('')
      setSubmitted(false)
      void queryClient.invalidateQueries({ queryKey: ['classes'] })
    },
    onError: (error) => showError(toUserMessage(error)),
  })

  const trimmedName = name.trim()
  const nameError =
    trimmedName.length === 0
      ? 'Enter a class name.'
      : trimmedName.length > MAX_NAME_LENGTH
        ? `The class name can have at most ${MAX_NAME_LENGTH} characters.`
        : null

  const seatsText = seats.trim()
  const seatCount = Number(seatsText)
  const seatsError =
    !/^\d+$/.test(seatsText) || seatCount < 1 || seatCount > MAX_SEATS
      ? `Enter a whole number of seats from 1 to ${MAX_SEATS}.`
      : null

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitted(true)
    if (nameError || seatsError) return
    create.mutate({ name: trimmedName, totalSeats: seatCount })
  }

  return (
    <section aria-labelledby="create-class-heading">
      <h2 id="create-class-heading">School admin: create a class</h2>
      <p className="hint">
        Each shift is its own class, for example "Class 6 - Morning Shift" and "Class 6 - Day Shift".
      </p>

      <form className="admin-form" onSubmit={handleSubmit} noValidate>
        <div className="field">
          <label htmlFor="class-name">Class name</label>
          <input
            id="class-name"
            type="text"
            value={name}
            maxLength={MAX_NAME_LENGTH + 50}
            onChange={(event) => setName(event.target.value)}
            aria-invalid={submitted && nameError !== null}
            aria-describedby={submitted && nameError ? 'class-name-error' : undefined}
            placeholder="Class 6 - Morning Shift"
          />
          {submitted && nameError && (
            <p id="class-name-error" className="field-error">
              {nameError}
            </p>
          )}
        </div>

        <div className="field">
          <label htmlFor="class-seats">Number of seats</label>
          <input
            id="class-seats"
            type="number"
            min={1}
            step={1}
            inputMode="numeric"
            value={seats}
            onChange={(event) => setSeats(event.target.value)}
            aria-invalid={submitted && seatsError !== null}
            aria-describedby={submitted && seatsError ? 'class-seats-error' : undefined}
            placeholder="120"
          />
          {submitted && seatsError && (
            <p id="class-seats-error" className="field-error">
              {seatsError}
            </p>
          )}
        </div>

        <div>
          <button type="submit" className="btn" disabled={create.isPending}>
            {create.isPending ? 'Creating…' : 'Create class'}
          </button>
        </div>
      </form>
    </section>
  )
}

