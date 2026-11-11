import { afterEach, describe, expect, it, vi } from 'vitest'
import { api } from './client'
import { ApiError } from './errors'

const STUDENT = '20152691234567890'

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function mockFetch(respond: () => Response) {
  const fn = vi.fn<typeof fetch>(() => Promise.resolve(respond()))
  vi.stubGlobal('fetch', fn)
  return fn
}

async function failureOf(promise: Promise<unknown>): Promise<ApiError> {
  try {
    await promise
  } catch (error) {
    expect(error).toBeInstanceOf(ApiError)
    return error as ApiError
  }
  throw new Error('Expected the request to fail, but it succeeded.')
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('api client', () => {
  it('sends the student id header and parses the JSON response', async () => {
    const fetchMock = mockFetch(() => jsonResponse([]))

    const result = await api.listMyHolds(STUDENT)

    expect(result).toEqual([])
    const [url, init] = fetchMock.mock.calls[0]
    expect(String(url).endsWith('/me/holds')).toBe(true)
    expect(init?.headers).toMatchObject({ 'X-Student-Id': STUDENT })
  })

  it('sends a JSON body when creating a class', async () => {
    const fetchMock = mockFetch(() =>
      jsonResponse({ id: 'c_1', name: 'Class 6', totalSeats: 10, heldSeats: 0, confirmedSeats: 0, availableSeats: 10 }, 201),
    )

    await api.createClass('Class 6', 10)

    const [, init] = fetchMock.mock.calls[0]
    expect(init?.method).toBe('POST')
    expect(init?.body).toBe(JSON.stringify({ name: 'Class 6', totalSeats: 10 }))
    expect(init?.headers).toMatchObject({ 'Content-Type': 'application/json' })
  })

  it('uses the server message for a business error', async () => {
    mockFetch(() =>
      jsonResponse({ error: 'NO_SEATS_AVAILABLE', message: 'No seats are available in this class.' }, 409),
    )

    const error = await failureOf(api.applyForSeat('c_1', STUDENT))

    expect(error.status).toBe(409)
    expect(error.code).toBe('NO_SEATS_AVAILABLE')
    expect(error.message).toBe('No seats are available in this class.')
  })

  it('shows a friendly message when the server cannot be reached', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    const error = await failureOf(api.listClasses())

    expect(error.status).toBe(0)
    expect(error.code).toBe('NETWORK_ERROR')
    expect(error.message).toContain('Cannot reach the server')
  })

  it('does not leak a non-JSON error page to the user', async () => {
    mockFetch(() => new Response('<html>boom</html>', { status: 500 }))

    const error = await failureOf(api.listClasses())

    expect(error.code).toBe('HTTP_500')
    expect(error.message).toBe('The server had a problem. Please try again in a moment.')
  })

  it('returns undefined for 204 No Content', async () => {
    mockFetch(() => new Response(null, { status: 204 }))

    await expect(api.cancelHold('h_1', STUDENT)).resolves.toBeUndefined()
  })
})

