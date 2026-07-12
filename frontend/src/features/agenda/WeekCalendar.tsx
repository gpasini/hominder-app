import { statusModifier, type TaskView } from '../tasks/taskStatus'
import { buildWeek } from './calendarItems'

type WeekCalendarProps = {
  tasks: TaskView[]
  reference: Date
  onSelectTask: (task: TaskView) => void
}

export function WeekCalendar({ tasks, reference, onSelectTask }: WeekCalendarProps) {
  const week = buildWeek(reference, tasks)
  const tasksById = new Map(tasks.map((task) => [task.id, task]))

  return (
    <div className="week">
      {week.map((day) => (
        <div className="week__day" key={day.date}>
          <div className="week__day-head">
            <span className="week__weekday">{day.weekdayLabel}</span>
            <span className="week__daynum">{day.dayOfMonth}</span>
          </div>
          <div className="week__items">
            {day.items.map((item) => {
              const task = tasksById.get(item.taskId)
              if (!task) {
                return null
              }
              return (
                <button
                  type="button"
                  key={item.taskId}
                  className={`chip chip--${statusModifier(item.status)}`}
                  onClick={() => onSelectTask(task)}
                >
                  {item.title}
                </button>
              )
            })}
          </div>
        </div>
      ))}
    </div>
  )
}
