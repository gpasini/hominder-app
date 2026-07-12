import { describe, expect, it } from 'vitest'
import type { TaskView } from '../tasks/taskStatus'
import { buildWeek, isoDate, startOfWeek, taskToCalendarItem, weekDates } from './calendarItems'

function task(overrides: Partial<TaskView>): TaskView {
  return {
    id: 'task-1',
    title: 'Tailler l\'olivier',
    notes: null,
    status: 'Due',
    openDate: '2026-07-06',
    dueDate: '2026-07-08',
    daysOverdue: 0,
    assigneeId: null,
    assigneeName: null,
    requiresNextDueOverride: false,
    policy: {
      kind: 'Interval',
      intervalAmount: 1,
      intervalUnit: 'Years',
      startReference: null,
      startMonth: null,
      endMonth: null,
      dueDate: null,
    },
    ...overrides,
  }
}

describe('isoDate', () => {
  it('formats local date components as YYYY-MM-DD', () => {
    expect(isoDate(new Date(2026, 6, 8))).toBe('2026-07-08')
    expect(isoDate(new Date(2026, 0, 3))).toBe('2026-01-03')
  })
})

describe('startOfWeek', () => {
  it('returns the Monday of the week for a mid-week date', () => {
    expect(isoDate(startOfWeek(new Date(2026, 6, 8)))).toBe('2026-07-06')
  })

  it('returns the same Monday when given a Monday', () => {
    expect(isoDate(startOfWeek(new Date(2026, 6, 6)))).toBe('2026-07-06')
  })

  it('treats Sunday as the last day of the week', () => {
    expect(isoDate(startOfWeek(new Date(2026, 6, 12)))).toBe('2026-07-06')
  })
})

describe('weekDates', () => {
  it('returns seven ISO dates from Monday to Sunday', () => {
    expect(weekDates(new Date(2026, 6, 8))).toEqual([
      '2026-07-06',
      '2026-07-07',
      '2026-07-08',
      '2026-07-09',
      '2026-07-10',
      '2026-07-11',
      '2026-07-12',
    ])
  })
})

describe('taskToCalendarItem', () => {
  it('maps a task onto its due date with its status', () => {
    expect(taskToCalendarItem(task({ id: 'x', title: 'CT', dueDate: '2026-07-09', status: 'Overdue' }))).toEqual({
      taskId: 'x',
      title: 'CT',
      date: '2026-07-09',
      status: 'Overdue',
    })
  })
})

describe('buildWeek', () => {
  it('produces seven days with correct labels and day numbers', () => {
    const week = buildWeek(new Date(2026, 6, 8), [])
    expect(week).toHaveLength(7)
    expect(week[0]).toMatchObject({ date: '2026-07-06', weekdayLabel: 'lun', dayOfMonth: 6, items: [] })
    expect(week[6]).toMatchObject({ date: '2026-07-12', weekdayLabel: 'dim', dayOfMonth: 12 })
  })

  it('places each task on the day matching its due date', () => {
    const week = buildWeek(new Date(2026, 6, 8), [
      task({ id: 'a', dueDate: '2026-07-06' }),
      task({ id: 'b', dueDate: '2026-07-08' }),
      task({ id: 'c', dueDate: '2026-07-08' }),
    ])
    expect(week[0].items.map((item) => item.taskId)).toEqual(['a'])
    expect(week[2].items.map((item) => item.taskId)).toEqual(['b', 'c'])
  })

  it('ignores tasks whose due date falls outside the visible week', () => {
    const week = buildWeek(new Date(2026, 6, 8), [task({ id: 'z', dueDate: '2026-07-20' })])
    expect(week.every((day) => day.items.length === 0)).toBe(true)
  })
})
