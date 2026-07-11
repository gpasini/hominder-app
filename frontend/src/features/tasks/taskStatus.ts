export type TaskStatus = 'Upcoming' | 'Due' | 'Overdue' | 'Done'

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
}

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

const statusOrder: Record<TaskStatus, number> = {
  Overdue: 0,
  Due: 1,
  Upcoming: 2,
  Done: 3,
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

export function groupByStatus(tasks: TaskView[]): [TaskStatus, TaskView[]][] {
  const groups = new Map<TaskStatus, TaskView[]>()
  for (const task of tasks) {
    const list = groups.get(task.status) ?? []
    list.push(task)
    groups.set(task.status, list)
  }
  return [...groups.entries()].sort((left, right) => statusOrder[left[0]] - statusOrder[right[0]])
}
