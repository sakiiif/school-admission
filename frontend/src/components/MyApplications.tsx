import { useEffect } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { toUserMessage } from '../api/errors'
import type { HoldStatus, MyHold } from '../api/types'
import { useNow } from '../hooks/useNow'
import { formatRemaining, getDisplayStatus, getRemainingMs, parseTimestamp } from '../utils/holds'

const STATUS_LABEL: Record<HoldStatus, string> = {
  HELD: 'Held',
  CONFIRMED: 'Confirmed',
  EXPIRED: 'Expired',
  CANCELLED: 'Cancelled',
}

interface Props {
  studentId: string
  showSuccess: (text: string) => void
  showError: (text: string) => void
}

export function MyApplications({ studentId, showSuccess, showError }: Props) {
  const queryClient = useQueryClient()
  const now = useNow() // ticks every second so countdowns move without any refetch

  const holds = useQuery({
    queryKey: ['my-holds', studentId],
    queryFn: () => api.listMyHolds(studentId),
    enabled: studentId !== '',
    refetchInterval: 3000,
  })

  const refreshAll = () => {
    void queryClient.invalidateQueries({ queryKey: ['my-holds'] })
    void queryClient.invalidateQueries({ queryKey: ['classes'] })
  }

  const pay = useMutation({
    mutationFn: (hold: MyHold) => api.confirmHold(hold.holdId, studentId),
    onSuccess: (_result, hold) =>
      showSuccess(`Admission fee paid. Your admission to ${hold.className} is confirmed.`),
    onError: (error) => showError(toUserMessage(error)),
    onSettled: refreshAll,
  })

  const cancel = useMutation({
    mutationFn: (hold: MyHold) => api.cancelHold(hold.holdId, studentId),
    onSuccess: (_result, hold) =>
      showSuccess(`Your application for ${hold.className} was cancelled and the seat was released.`),
    onError: (error) => showError(toUserMessage(error)),
    onSettled: refreshAll,
  })

  // When a countdown reaches zero, ask the server for the real status and
  // fresh seat counts, so the released seat shows up in the class list quickly.
  const hasLocallyExpired = (holds.data ?? []).some(
    (hold) => hold.status === 'HELD' && parseTimestamp(hold.expiresAt) <= now,
  )
  useEffect(() => {
    if (!hasLocallyExpired) return
    void queryClient.invalidateQueries({ queryKey: ['my-holds'] })
    void queryClient.invalidateQueries({ queryKey: ['classes'] })
  }, [hasLocallyExpired, queryClient])

  function handleCancel(hold: MyHold) {
    const sure = window.confirm(
      `Cancel your application for ${hold.className}? Your seat will be released and may be taken by someone else.`,
    )
    if (sure) cancel.mutate(hold)
  }

  if (!studentId) {
    return (
      <section aria-labelledby="applications-heading">
        <h2 id="applications-heading">My Applications</h2>
        <p className="hint">Enter a valid student registration number above to see your applications.</p>
      </section>
    )
  }

  if (holds.isPending) {
    return (
      <section aria-labelledby="applications-heading">
        <h2 id="applications-heading">My Applications</h2>
        <p>Loading your applications…</p>
      </section>
    )
  }

  if (!holds.data) {
    return (
      <section aria-labelledby="applications-heading">
        <h2 id="applications-heading">My Applications</h2>
        <p role="alert" className="error">
          {toUserMessage(holds.error)}
        </p>
        <button type="button" className="btn btn--secondary" onClick={() => void holds.refetch()}>
          Try again
        </button>
      </section>
    )
  }

  return (
    <section aria-labelledby="applications-heading">
      <h2 id="applications-heading">My Applications</h2>

      {holds.isError && (
        <p role="alert" className="error">
          {toUserMessage(holds.error)} Showing the last known applications.
        </p>
      )}

      {holds.data.length === 0 ? (
        <p>You have not applied to any class yet.</p>
      ) : (
        <div className="table-wrap">
          <table className="data">
            <thead>
              <tr>
                <th scope="col">Class</th>
                <th scope="col">Status</th>
                <th scope="col">Time left</th>
                <th scope="col">
                  <span className="visually-hidden">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {holds.data.map((hold) => {
                const status = getDisplayStatus(hold.status, hold.expiresAt, now)
                const remaining = getRemainingMs(hold.expiresAt, now)
                const isHeld = status === 'HELD'
                const isPaying = pay.isPending && pay.variables?.holdId === hold.holdId
                const isCancelling = cancel.isPending && cancel.variables?.holdId === hold.holdId
                const isBusy = isPaying || isCancelling

                let disabledReason: string | undefined
                if (status === 'EXPIRED') disabledReason = 'This application has expired'
                else if (status === 'CONFIRMED') disabledReason = 'Admission is already confirmed'
                else if (status === 'CANCELLED') disabledReason = 'This application was cancelled'

                return (
                  <tr key={hold.holdId}>
                    <th scope="row">{hold.className}</th>
                    <td>
                      <span className={`badge badge--${status.toLowerCase()}`}>{STATUS_LABEL[status]}</span>
                    </td>
                    <td className={isHeld && remaining <= 60_000 ? 'urgent' : undefined}>
                      {isHeld ? formatRemaining(remaining) : '—'}
                    </td>
                    <td>
                      <div className="row-actions">
                        <button
                          type="button"
                          className="btn"
                          disabled={!isHeld || isBusy}
                          title={disabledReason}
                          onClick={() => pay.mutate(hold)}
                        >
                          {isPaying ? 'Processing…' : 'Pay Admission Fee'}
                        </button>
                        <button
                          type="button"
                          className="btn btn--secondary"
                          disabled={!isHeld || isBusy}
                          title={disabledReason}
                          onClick={() => handleCancel(hold)}
                        >
                          {isCancelling ? 'Cancelling…' : 'Cancel'}
                        </button>
                      </div>
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

