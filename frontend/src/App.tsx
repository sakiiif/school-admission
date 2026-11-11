import { useQuery } from '@tanstack/react-query'
import { api } from './api/client'
import { toUserMessage } from './api/errors'

export default function App() {
  const { data, error, isPending } = useQuery({
    queryKey: ['classes'],
    queryFn: () => api.listClasses(),
    refetchInterval: 3000,
  })

  return (
    <main className="container">
      <h1>School Admission</h1>
      {isPending && <p>Loading classes…</p>}
      {error && (
        <p role="alert" className="error">
          {toUserMessage(error)}
        </p>
      )}
      {data && <p>Connected to the server. {data.length} class(es) found.</p>}
    </main>
  )
}