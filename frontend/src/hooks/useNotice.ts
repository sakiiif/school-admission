import { useCallback, useEffect, useState } from 'react'

export interface Notice {
  kind: 'success' | 'error'
  text: string
}

// One message at a time: a new one replaces the old one, and it clears itself
export function useNotice() {
  const [notice, setNotice] = useState<Notice | null>(null)

  useEffect(() => {
    if (!notice) return
    const timer = setTimeout(() => setNotice(null), notice.kind === 'success' ? 6000 : 10000)
    return () => clearTimeout(timer)
  }, [notice])

  const showSuccess = useCallback((text: string) => setNotice({ kind: 'success', text }), [])
  const showError = useCallback((text: string) => setNotice({ kind: 'error', text }), [])
  const dismiss = useCallback(() => setNotice(null), [])

  return { notice, showSuccess, showError, dismiss }
}


