export type TaskStatus = 'Upcoming' | 'Due' | 'Overdue' | 'Done'

export type RecurrenceKind = 'Interval' | 'MonthWindow' | 'FixedDate' | 'OneOff'

export type RecurrenceUnit = 'Days' | 'Weeks' | 'Months' | 'Years'

export type RecurrencePolicyInput = {
  kind: RecurrenceKind
  intervalAmount: number | null
  intervalUnit: RecurrenceUnit | null
  startReference: string | null
  startMonth: number | null
  endMonth: number | null
  dueDate: string | null
}

export type TaskView = {
  id: string
  title: string
  notes: string | null
  status: TaskStatus
  openDate: string
  dueDate: string
  daysOverdue: number
  assigneeId: string | null
  assigneeName: string | null
  requiresNextDueOverride: boolean
  policy: RecurrencePolicyInput
}

export function statusModifier(status: TaskStatus): string {
  return status.toLowerCase()
}

export function statusLabel(status: TaskStatus): string {
  switch (status) {
    case 'Overdue':
      return 'En retard'
    case 'Due':
      return 'À faire'
    case 'Upcoming':
      return 'À venir'
    case 'Done':
      return 'Fait'
  }
}

