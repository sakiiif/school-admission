import { useEffect, useState } from 'react'
import { isValidStudentId } from '../utils/studentId'

const STORAGE_KEY = 'admission.studentId'
const ID_LENGTH = 17

function readStoredId(): string {
  try {
    const value = window.localStorage.getItem(STORAGE_KEY)
    return value && isValidStudentId(value) ? value : ''
  } catch {
    return '' // storage can be blocked (private mode, strict settings)
  }
}

function writeStoredId(value: string) {
  try {
    if (value) window.localStorage.setItem(STORAGE_KEY, value)
    else window.localStorage.removeItem(STORAGE_KEY)
  } catch {
    // Not being able to remember the ID must never break the page.
  }
}


 // 'draft' is what is typed in the box. 'studentId' is the identity actually used
 // for API calls: the draft once it is a valid 17-digit number, otherwise
 
export function useStudentId() {
  const [draft, setDraftState] = useState(readStoredId)
  const studentId = isValidStudentId(draft) ? draft : ''

  useEffect(() => {
    writeStoredId(studentId)
  }, [studentId])

  const setDraft = (value: string) => setDraftState(value.replace(/\D/g, '').slice(0, ID_LENGTH))

  return { draft, setDraft, studentId }
}