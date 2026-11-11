// A failed API call, already carrying a message that is safe to show to the user
export class ApiError extends Error {
  readonly status: number // 0 means the server could not be reached
  readonly code: string // NO_SEATS_AVAILABLE, NETWORK_ERROR

  constructor(status: number, code: string, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }
}

// Turns anything that was thrown into text a guardian can understand
export function toUserMessage(error: unknown): string {
  if (error instanceof ApiError) return error.message
  return 'Something went wrong. Please try again.'
}