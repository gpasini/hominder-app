import { groupByStatus, statusLabel, type TaskView } from './taskStatus'
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
    return <p>Aucune tâche pour l'instant.</p>
  }

  return (
    <div>
      {groups.map(([status, groupTasks]) => (
        <section key={status}>
          <h2>{statusLabel(status)}</h2>
          {groupTasks.map((task) => (
            <TaskCard
              key={task.id}
              task={task}
              onMarkDone={onMarkDone}
              onEdit={onEdit}
              onDelete={onDelete}
            />
          ))}
        </section>
      ))}
    </div>
  )
}
