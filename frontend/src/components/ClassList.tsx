import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { toUserMessage } from '../api/errors'

interface Props {
  studentId: string
  showSuccess: (text: string) => void
  showError: (text: string) => void
}

export function ClassList({ studentId, showSuccess, showError }: Props) {
  const queryClient = useQueryClient()

  const classes = useQuery({
    queryKey: ['classes'],
    queryFn: () => api.listClasses(),
    refetchInterval: 3000, // seat counts refresh without a page reload
  })

  const apply = useMutation({
    mutationFn: (classId: string) => api.applyForSeat(classId, studentId),
    onSuccess: (hold, classId) => {
      const name = classes.data?.find((c) => c.id === classId)?.name ?? 'the class'
      const until = new Date(hold.expiresAt).toLocaleTimeString()
      showSuccess(`A seat is held for you in ${name}. Pay the admission fee before ${until}.`)
    },
    onError: (error) => showError(toUserMessage(error)),
    onSettled: () => {
      // Even a failure (for example "no seats left") means our numbers are stale.
      void queryClient.invalidateQueries({ queryKey: ['classes'] })
      void queryClient.invalidateQueries({ queryKey: ['my-holds'] })
    },
  })

  if (classes.isPending) return <p>Loading classes…</p>

  if (!classes.data) {
    return (
      <div>
        <p role="alert" className="error">
          {toUserMessage(classes.error)}
        </p>
        <button type="button" className="btn btn--secondary" onClick={() => void classes.refetch()}>
          Try again
        </button>
      </div>
    )
  }

  return (
    <section aria-labelledby="classes-heading">
      <h2 id="classes-heading">Classes</h2>

      {classes.isError && (
        <p role="alert" className="error">
          {toUserMessage(classes.error)} Showing the last known seat counts.
        </p>
      )}

      {!studentId && <p className="hint">Enter a valid student registration number above to apply.</p>}

      {classes.data.length === 0 ? (
        <p>No classes have been created yet.</p>
      ) : (
        <div className="table-wrap">
          <table className="data">
            <thead>
              <tr>
                <th scope="col">Class</th>
                <th scope="col" className="num">Total</th>
                <th scope="col" className="num">Held</th>
                <th scope="col" className="num">Confirmed</th>
                <th scope="col" className="num">Available</th>
                <th scope="col">
                  <span className="visually-hidden">Action</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {classes.data.map((c) => {
                const isApplying = apply.isPending && apply.variables === c.id
                const isFull = c.availableSeats === 0

                let blockedReason: string | null = null
                if (!studentId) blockedReason = 'Enter a valid student registration number first'
                else if (isFull) blockedReason = 'No seats are left in this class'
                else if (isApplying) blockedReason = 'Applying…'

                return (
                  <tr key={c.id}>
                    <th scope="row">{c.name}</th>
                    <td className="num">{c.totalSeats}</td>
                    <td className="num">{c.heldSeats}</td>
                    <td className="num">{c.confirmedSeats}</td>
                    <td className={isFull ? 'num full' : 'num'}>
                      {c.availableSeats}
                      {isFull && ' (full)'}
                    </td>
                    <td>
                      <button
                        type="button"
                        className="btn"
                        disabled={blockedReason !== null}
                        title={blockedReason ?? undefined}
                        onClick={() => apply.mutate(c.id)}
                      >
                        {isApplying ? 'Applying…' : 'Apply'}
                      </button>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

