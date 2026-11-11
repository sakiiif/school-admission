import { ApiError } from './errors'
import type { ClassInfo, ConfirmResult, Hold, MyHold } from './types'

const BASE_URL = (import.meta.env.VITE_API_URL || 'http://localhost:8080').replace(/\/+$/, '')
const TIMEOUT_MS = 10_000

interface RequestOptions {
  method?: 'GET' | 'POST' | 'DELETE'
  studentId?: string
  body?: unknown
}

function fallbackMessage(status: number): string {
  if (status === 400) return 'Some of the submitted information is not valid.'
  if (status === 403) return 'You are not allowed to do this.'
  if (status === 404) return 'We could not find what you were looking for.'
  if (status === 409) return 'This action is no longer possible.'
  if (status >= 500) return 'The server had a problem. Please try again in a moment.'
  return 'Something went wrong. Please try again.'
}

async function toApiError(response: Response): Promise<ApiError> {
  let code: string | undefined
  let message: string | undefined

  try {
    const data: unknown = await response.json()
    if (data && typeof data === 'object') {
      const record = data as Record<string, unknown>
      if (typeof record.error === 'string') code = record.error
      if (typeof record.message === 'string') message = record.message
    }
  } catch {
    // The body was not JSON (for example an HTML error page). Use the fallback text.
  }

  return new ApiError(
    response.status,
    code ?? `HTTP_${response.status}`,
    message ?? fallbackMessage(response.status),
  )
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (options.body !== undefined) headers['Content-Type'] = 'application/json'
  if (options.studentId) headers['X-Student-Id'] = options.studentId

  const controller = new AbortController()
  const timer = setTimeout(() => controller.abort(), TIMEOUT_MS)

  let response: Response
  try {
    response = await fetch(`${BASE_URL}${path}`, {
      method: options.method ?? 'GET',
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
      signal: controller.signal,
    })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw new ApiError(0, 'TIMEOUT', 'The server took too long to respond. Please try again.')
    }
    throw new ApiError(
      0,
      'NETWORK_ERROR',
      'Cannot reach the server. Please check your connection and try again.',
    )
  } finally {
    clearTimeout(timer)
  }

  if (!response.ok) throw await toApiError(response)
  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

const enc = encodeURIComponent

export const api = {
  createClass: (name: string, totalSeats: number) =>
    request<ClassInfo>('/classes', { method: 'POST', body: { name, totalSeats } }),

  listClasses: () => request<ClassInfo[]>('/classes'),

  getClass: (classId: string) => request<ClassInfo>(`/classes/${enc(classId)}`),

  applyForSeat: (classId: string, studentId: string) =>
    request<Hold>(`/classes/${enc(classId)}/holds`, { method: 'POST', studentId }),

  getHold: (holdId: string) => request<Hold>(`/holds/${enc(holdId)}`),

  confirmHold: (holdId: string, studentId: string) =>
    request<ConfirmResult>(`/holds/${enc(holdId)}/confirm`, { method: 'POST', studentId }),

  cancelHold: (holdId: string, studentId: string) =>
    request<void>(`/holds/${enc(holdId)}`, { method: 'DELETE', studentId }),

  listMyHolds: (studentId: string) => request<MyHold[]>('/me/holds', { studentId }),
}