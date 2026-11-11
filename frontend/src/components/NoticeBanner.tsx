import type { Notice } from '../hooks/useNotice'

interface Props {
  notice: Notice | null
  onDismiss: () => void
}

export function NoticeBanner({ notice, onDismiss }: Props) {
  if (!notice) return null

  return (
    <div
      className={`notice notice--${notice.kind}`}
      role={notice.kind === 'error' ? 'alert' : 'status'}
    >
      <span>{notice.text}</span>
      <button type="button" className="notice__close" onClick={onDismiss} aria-label="Dismiss message">
        ×
      </button>
    </div>
  )
}



