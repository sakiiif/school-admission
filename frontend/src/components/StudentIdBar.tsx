interface Props {
  value: string
  onChange: (value: string) => void
  isValid: boolean
}

export function StudentIdBar({ value, onChange, isValid }: Props) {
  let help: string
  if (isValid) help = 'Student ID accepted. You can apply for seats.'
  else if (value.length === 0) help = 'Enter your 17-digit registration number to apply for a seat.'
  else help = `${value.length} of 17 digits entered. Digits only.`

  return (
    <section className="student-bar" aria-label="Student identity">
      <label htmlFor="student-id">Student registration number</label>
      <div className="student-bar__row">
        <input
          id="student-id"
          type="text"
          inputMode="numeric"
          autoComplete="off"
          maxLength={17}
          placeholder="17-digit registration number"
          value={value}
          onChange={(event) => onChange(event.target.value)}
          aria-describedby="student-id-help"
          aria-invalid={value.length > 0 && !isValid}
        />
        {value && (
          <button type="button" className="btn btn--secondary" onClick={() => onChange('')}>
            Clear
          </button>
        )}
      </div>
      <p id="student-id-help" className={isValid ? 'hint hint--ok' : 'hint'}>
        {help}
      </p>
    </section>
  )
}

