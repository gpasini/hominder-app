import { groupByStatus, statusLabel, statusModifier, type TaskView } from './taskStatus'
import { TaskCard } from './TaskCard'

type TaskListProps = {
  tasks: TaskView[]
  onMarkDone: (task: TaskView) => void
  onEdit: (task: TaskView) => void
  onDelete: (task: TaskView) => void
}

export function TaskList({ tasks, onMarkDone, onEdit, onDelete }: TaskListProps) {
  const groups = groupByStatus(tasks)

  if (groups.length === 0) {
    return <p className="empty">Aucune tâche pour l'instant.</p>
  }

  return (
    <div className="stack">
      {groups.map(([status, groupTasks]) => (
        <section className="group" key={status}>
          <h2 className="group__title">
            <span className={`group__dot group__dot--${statusModifier(status)}`} />
            {statusLabel(status)}
            <span className="group__count">{groupTasks.length}</span>
          </h2>
          <div className="cards">
            {groupTasks.map((task) => (
              <TaskCard
                key={task.id}
                task={task}
                onMarkDone={onMarkDone}
                onEdit={onEdit}
                onDelete={onDelete}
              />
            ))}
          </div>
        </section>
      ))}
    </div>
  )
}
