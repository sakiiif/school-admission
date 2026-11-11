import { useSyncExternalStore } from 'react'

let currentNow = Date.now()
let timer: ReturnType<typeof setInterval> | undefined
const listeners = new Set<() => void>()

function subscribe(listener: () => void) {
  listeners.add(listener)

  if (listeners.size === 1) {
    currentNow = Date.now()
    timer = setInterval(() => {
      currentNow = Date.now()
      listeners.forEach((notify) => notify())
    }, 1000)
  }

  return () => {
    listeners.delete(listener)
    if (listeners.size === 0 && timer !== undefined) {
      clearInterval(timer)
      timer = undefined
    }
  }
}

function getSnapshot() {
  return currentNow
}

// The current time in milliseconds, refreshed every second
export function useNow(): number {
  return useSyncExternalStore(subscribe, getSnapshot)
}

