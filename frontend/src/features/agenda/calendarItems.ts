import type { TaskStatus, TaskView } from '../tasks/taskStatus'

export type CalendarItem = {
  taskId: string
  title: string
  date: string
  status: TaskStatus
}

export type CalendarDay = {
  date: string
  weekdayLabel: string
  dayOfMonth: number
  items: CalendarItem[]
}

const weekdayLabels = ['lun', 'mar', 'mer', 'jeu', 'ven', 'sam', 'dim']

export function isoDate(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function startOfWeek(reference: Date): Date {
  const monday = new Date(reference.getFullYear(), reference.getMonth(), reference.getDate())
  const offset = (monday.getDay() + 6) % 7
  monday.setDate(monday.getDate() - offset)
  return monday
}

export function weekDates(reference: Date): string[] {
  const monday = startOfWeek(reference)
  return Array.from({ length: 7 }, (_, index) => {
    const day = new Date(monday.getFullYear(), monday.getMonth(), monday.getDate() + index)
    return isoDate(day)
  })
}

export function taskToCalendarItem(task: TaskView): CalendarItem {
  return {
    taskId: task.id,
    title: task.title,
    date: task.dueDate,
    status: task.status,
  }
}

export function buildWeek(reference: Date, tasks: TaskView[]): CalendarDay[] {
  const items = tasks.map(taskToCalendarItem)
  return weekDates(reference).map((date, index) => {
    const [, , day] = date.split('-')
    return {
      date,
      weekdayLabel: weekdayLabels[index],
      dayOfMonth: Number(day),
      items: items.filter((item) => item.date === date),
    }
  })
}
