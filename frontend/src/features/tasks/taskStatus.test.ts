import { expect, test } from 'vitest'
import { groupByStatus, statusLabel, type TaskView } from './taskStatus'

const view = (id: string, status: TaskView['status']): TaskView => ({
  id,
  title: id,
  notes: null,
  status,
  openDate: '2026-03-01',
  dueDate: '2026-05-31',
  daysOverdue: 0,
  assigneeId: null,
  assigneeName: null,
  requiresNextDueOverride: false,
})

test('statusLabel maps to French labels', () => {
  expect(statusLabel('Overdue')).toBe('En retard')
  expect(statusLabel('Due')).toBe('À faire')
  expect(statusLabel('Upcoming')).toBe('À venir')
  expect(statusLabel('Done')).toBe('Fait')
})

test('groupByStatus orders overdue first and done last', () => {
  const groups = groupByStatus([view('a', 'Upcoming'), view('b', 'Overdue'), view('c', 'Done')])

  expect(groups.map(([status]) => status)).toEqual(['Overdue', 'Upcoming', 'Done'])
})
