import { ClassList } from './components/ClassList'
import { CreateClassForm } from './components/CreateClassForm'
import { MyApplications } from './components/MyApplications'
import { NoticeBanner } from './components/NoticeBanner'
import { StudentIdBar } from './components/StudentIdBar'
import { useNotice } from './hooks/useNotice'
import { useStudentId } from './hooks/useStudentId'

export default function App() {
  const { draft, setDraft, studentId } = useStudentId()
  const { notice, showSuccess, showError, dismiss } = useNotice()

  return (
    <main className="container">
      <h1>School Admission</h1>
      <StudentIdBar value={draft} onChange={setDraft} isValid={studentId !== ''} />
      <NoticeBanner notice={notice} onDismiss={dismiss} />
      <ClassList studentId={studentId} showSuccess={showSuccess} showError={showError} />
      <MyApplications studentId={studentId} showSuccess={showSuccess} showError={showError} />
      <CreateClassForm showSuccess={showSuccess} showError={showError} />
    </main>
  )
}

