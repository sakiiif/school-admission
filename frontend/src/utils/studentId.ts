const STUDENT_ID_PATTERN = /^\d{17}$/

// The backend requires exactly 17 digits
export function isValidStudentId(value: string): boolean {
  return STUDENT_ID_PATTERN.test(value)
}